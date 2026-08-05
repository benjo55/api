using api.Data;
using api.Models;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace api.Services
{
    public sealed class FieldDescriptionSeedService
    {
        private readonly ApplicationDBContext _db;
        private readonly ILogger<FieldDescriptionSeedService> _logger;

        public FieldDescriptionSeedService(
            ApplicationDBContext db,
            ILogger<FieldDescriptionSeedService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            var existingDescriptions = await _db.FieldDescriptions
                .ToListAsync(cancellationToken);
            var existingByKey = existingDescriptions
                .GroupBy(description => Key(description.EntityName, description.FieldName))
                .ToDictionary(group => group.Key, group => group.First());

            var inserted = 0;
            var completed = 0;
            var now = DateTime.UtcNow;

            var definitions = FieldDescriptionCatalog.All
                .Concat(GetModelFieldDefinitions())
                .Concat(await GetDatabaseFieldDefinitionsAsync(cancellationToken));

            foreach (var definition in definitions)
            {
                var key = Key(definition.EntityName, definition.FieldName);
                if (existingByKey.TryGetValue(key, out var existing))
                {
                    if (string.IsNullOrWhiteSpace(existing.Description))
                    {
                        existing.Description = definition.Description;
                        existing.UpdatedDate = now;
                        completed++;
                    }

                    continue;
                }

                var fieldDescription = new FieldDescription
                {
                    EntityName = definition.EntityName,
                    FieldName = definition.FieldName,
                    Description = definition.Description,
                    Locked = true,
                    CreatedDate = now,
                    UpdatedDate = now
                };
                _db.FieldDescriptions.Add(fieldDescription);
                existingByKey[key] = fieldDescription;
                inserted++;
            }

            if (inserted > 0 || completed > 0)
            {
                await _db.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation(
                "Field description catalog synchronized: {Inserted} inserted, {Completed} completed.",
                inserted,
                completed);
        }

        private IEnumerable<FieldDescriptionDefinition> GetModelFieldDefinitions()
        {
            var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var entityType in _db.Model.GetEntityTypes())
            {
                var tableName = entityType.GetTableName();
                if (string.IsNullOrWhiteSpace(tableName))
                {
                    continue;
                }

                if (string.Equals(tableName, nameof(ApplicationDBContext.FieldDescriptions), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var tableIdentifier = StoreObjectIdentifier.Table(
                    tableName,
                    entityType.GetSchema());

                foreach (var property in entityType.GetProperties())
                {
                    var columnName = property.GetColumnName(tableIdentifier)
                        ?? property.GetColumnName()
                        ?? property.Name;
                    if (string.IsNullOrWhiteSpace(columnName))
                    {
                        continue;
                    }

                    var key = Key(tableName, columnName);
                    if (!seenKeys.Add(key))
                    {
                        continue;
                    }

                    yield return new FieldDescriptionDefinition(
                        tableName,
                        columnName,
                        BuildGeneratedDescription(
                            tableName,
                            columnName,
                            Nullable.GetUnderlyingType(property.ClrType)?.Name ?? property.ClrType.Name,
                            property.IsNullable));
                }
            }
        }

        private async Task<IReadOnlyList<FieldDescriptionDefinition>> GetDatabaseFieldDefinitionsAsync(
            CancellationToken cancellationToken)
        {
            if (!_db.Database.IsRelational())
            {
                return [];
            }

            var definitions = new List<FieldDescriptionDefinition>();
            var connection = _db.Database.GetDbConnection();
            var shouldCloseConnection = connection.State == ConnectionState.Closed;

            if (shouldCloseConnection)
            {
                await connection.OpenAsync(cancellationToken);
            }

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = """
                    SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, IS_NULLABLE
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME <> 'FieldDescriptions'
                    ORDER BY TABLE_NAME, ORDINAL_POSITION
                    """;

                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    var tableName = reader.GetString(0);
                    var columnName = reader.GetString(1);
                    var dataType = reader.GetString(2);
                    var isNullable = string.Equals(
                        reader.GetString(3),
                        "YES",
                        StringComparison.OrdinalIgnoreCase);

                    definitions.Add(new FieldDescriptionDefinition(
                        tableName,
                        columnName,
                        BuildGeneratedDescription(tableName, columnName, dataType, isNullable)));
                }
            }
            finally
            {
                if (shouldCloseConnection)
                {
                    await connection.CloseAsync();
                }
            }

            return definitions;
        }

        private static string BuildGeneratedDescription(
            string tableName,
            string columnName,
            string typeLabel,
            bool isNullable)
        {
            var fieldLabel = HumanizeIdentifier(columnName);
            var tableLabel = HumanizeTableName(tableName, plural: true);
            var requiredText = isNullable ? "facultatif" : "obligatoire";
            var typeName = HumanizeTypeLabel(typeLabel);
            var semanticDescription = InferSemanticDescription(tableLabel, fieldLabel, columnName);

            return
                $"{semanticDescription} Champ {requiredText}, type {typeName}.";
        }

        private static string InferSemanticDescription(
            string tableLabel,
            string fieldLabel,
            string columnName)
        {
            var normalized = columnName.ToLowerInvariant();
            var tableSingular = Singularize(tableLabel);
            var relatedEntity = GetRelatedEntityLabel(columnName);

            if (string.Equals(normalized, "id", StringComparison.OrdinalIgnoreCase))
            {
                return $"Identifiant technique unique de l'enregistrement {tableSingular}. Il sert aux liens internes et à la traçabilité.";
            }

            if (normalized.EndsWith("id", StringComparison.OrdinalIgnoreCase) && relatedEntity is not null)
            {
                return $"Référence vers {relatedEntity}. Elle permet de rattacher cet enregistrement {tableSingular} à la donnée associée.";
            }

            if (normalized.Contains("email"))
            {
                return $"Adresse e-mail associée à {tableSingular}. Elle sert aux contacts, notifications ou contrôles d'identité selon le contexte.";
            }

            if (normalized.Contains("phone") || normalized.Contains("mobile") || normalized.Contains("tel"))
            {
                return $"Numéro de téléphone associé à {tableSingular}. Il permet le contact direct ou la vérification lorsque le parcours le requiert.";
            }

            if (normalized.Contains("address"))
            {
                return $"Adresse postale ou adresse structurée associée à {tableSingular}. Elle peut être utilisée pour l'identification, les courriers ou les justificatifs.";
            }

            if (normalized.Contains("city"))
            {
                return $"Ville ou commune associée à {tableSingular}. Elle précise la localisation de la donnée.";
            }

            if (normalized.Contains("country"))
            {
                return $"Pays associé à {tableSingular}. Il précise le rattachement géographique ou réglementaire.";
            }

            if (normalized.Contains("postal") || normalized.Contains("zip"))
            {
                return $"Code postal associé à {tableSingular}. Il complète les informations d'adresse.";
            }

            if (normalized.Contains("birth"))
            {
                return $"Information de naissance associée à {tableSingular}. Elle contribue à l'identification, aux contrôles d'âge et aux règles métier.";
            }

            if (normalized.Contains("date") || normalized.EndsWith("at"))
            {
                return $"Date ou horodatage lié à {fieldLabel} pour {tableSingular}. Ce champ situe l'événement ou l'état dans le temps.";
            }

            if (normalized.Contains("amount") || normalized.Contains("premium") || normalized.Contains("balance") || normalized.Contains("value") || normalized.Contains("capital"))
            {
                return $"Montant ou valeur financière correspondant à {fieldLabel} pour {tableSingular}. Il alimente les calculs, contrôles et restitutions financières.";
            }

            if (normalized.Contains("rate") || normalized.Contains("percentage") || normalized.Contains("percent"))
            {
                return $"Taux ou pourcentage correspondant à {fieldLabel} pour {tableSingular}. Il est utilisé dans les calculs, répartitions ou contrôles de seuil.";
            }

            if (normalized.Contains("quantity") || normalized.Contains("shares") || normalized.Contains("units"))
            {
                return $"Quantité, nombre de parts ou unités correspondant à {fieldLabel} pour {tableSingular}. Ce champ sert aux valorisations et réconciliations.";
            }

            if (normalized.Contains("currency"))
            {
                return $"Devise de référence de {tableSingular}. Elle indique dans quelle monnaie les montants associés sont exprimés.";
            }

            if (normalized.Contains("status") || normalized.Contains("state"))
            {
                return $"Statut de {tableSingular}. Il indique l'état courant du cycle de vie ou du traitement.";
            }

            if (normalized.Contains("type") || normalized.Contains("kind") || normalized.Contains("category") || normalized.Contains("family") || normalized.Contains("nature"))
            {
                return $"Classification de {tableSingular} selon {fieldLabel}. Elle permet d'appliquer les bonnes règles métier et d'organiser les affichages.";
            }

            if (normalized.StartsWith("is") || normalized.StartsWith("has") || normalized.StartsWith("can") || normalized.Contains("enabled") || normalized.Contains("active") || normalized.Contains("locked"))
            {
                return $"Indicateur oui/non lié à {fieldLabel} pour {tableSingular}. Il active, bloque ou qualifie un comportement métier.";
            }

            if (normalized.Contains("code"))
            {
                return $"Code de référence de {tableSingular}. Il fournit un identifiant court, stable ou interopérable.";
            }

            if (normalized.Contains("number") || normalized.Contains("référence") || normalized.Contains("registration"))
            {
                return $"Numéro ou référence associée à {tableSingular}. Il facilite l'identification, la recherche et les rapprochements.";
            }

            if (normalized.Contains("label") || normalized.Contains("name") || normalized.Contains("title"))
            {
                return $"Libellé ou nom de {tableSingular}. Il sert à identifier clairement l'enregistrement dans les listes, écrans et documents.";
            }

            if (normalized.Contains("description") || normalized.Contains("comment") || normalized.Contains("note") || normalized.Contains("reason"))
            {
                return $"Texte descriptif ou commentaire associé à {tableSingular}. Il apporte le contexte métier que les champs structurés ne portent pas seuls.";
            }

            if (normalized.Contains("url") || normalized.Contains("uri") || normalized.Contains("link"))
            {
                return $"Lien ou adresse externe associée à {tableSingular}. Il permet d'accéder à une ressource, un document ou une page complémentaire.";
            }

            if (normalized.Contains("file") || normalized.Contains("document") || normalized.Contains("artifact"))
            {
                return $"Information documentaire associée à {tableSingular}. Elle sert au classement, à la génération ou au suivi des pièces.";
            }

            if (normalized.Contains("content") || normalized.Contains("html") || normalized.Contains("text"))
            {
                return $"Contenu textuel de {tableSingular}. Il porte la matière affichée, générée ou transmise par l'application.";
            }

            if (normalized.Contains("json") || normalized.Contains("payload") || normalized.Contains("data"))
            {
                return $"Données structurées associées à {tableSingular}. Elles stockent un détail métier non représenté par des colonnes simples.";
            }

            if (normalized.Contains("hash") || normalized.Contains("token") || normalized.Contains("secret"))
            {
                return $"Valeur technique de sécurité associée à {tableSingular}. Elle sert à vérifier, protéger ou tracer une opération sensible.";
            }

            if (normalized.Contains("version"))
            {
                return $"Version de {tableSingular}. Elle permet de suivre l'évolution de la donnée ou d'appliquer la bonne variante métier.";
            }

            if (normalized.Contains("order") || normalized.Contains("rank") || normalized.Contains("priority") || normalized.Contains("position"))
            {
                return $"Ordre, rang ou priorité de {tableSingular}. Ce champ pilote le tri, l'affichage ou l'arbitrage entre plusieurs éléments.";
            }

            return $"Champ {fieldLabel} de {tableSingular}. Il porte une information métier déduite de son nom et rattachée à cet enregistrement.";
        }

        private static string? GetRelatedEntityLabel(string columnName)
        {
            if (!columnName.EndsWith("Id", StringComparison.OrdinalIgnoreCase) || columnName.Length <= 2)
            {
                return null;
            }

            var relatedName = columnName[..^2];
            return string.IsNullOrWhiteSpace(relatedName)
                ? null
                : Singularize(HumanizeTableName(relatedName, plural: false));
        }

        private static string Singularize(string value)
        {
            var normalized = value.Trim();
            if (SingularLabels.Contains(normalized))
            {
                return normalized;
            }

            return value.EndsWith("ies", StringComparison.OrdinalIgnoreCase)
                ? value[..^3] + "y"
                : value.EndsWith("s", StringComparison.OrdinalIgnoreCase)
                    ? value[..^1]
                    : value;
        }

        private static string HumanizeTableName(string value, bool plural)
        {
            var normalized = value.Trim();
            if (TableLabels.TryGetValue(normalized, out var labels))
            {
                return plural ? labels.Plural : labels.Singular;
            }

            return HumanizeIdentifier(value);
        }

        private static string HumanizeTypeLabel(string value)
        {
            var normalized = value.Trim().ToLowerInvariant();
            return normalized switch
            {
                "string" or "nvarchar" or "varchar" or "text" or "ntext" => "texte",
                "int32" or "int" => "nombre entier",
                "int64" or "bigint" => "grand nombre entier",
                "decimal" or "numeric" or "money" or "smallmoney" => "nombre décimal",
                "double" or "float" or "real" => "nombre décimal",
                "boolean" or "bool" or "bit" => "booléen",
                "datetime" or "datetime2" or "date" or "datetimeoffset" => "date/heure",
                "guid" or "uniqueidentifier" => "identifiant unique",
                "byte[]" or "varbinary" or "binary" or "image" => "donnée binaire",
                _ => value
            };
        }

        private static string HumanizeIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            var chars = new List<char>(value.Length + 8);
            for (var index = 0; index < value.Length; index++)
            {
                var current = value[index];
                if (current == '_')
                {
                    chars.Add(' ');
                    continue;
                }

                if (index > 0
                    && char.IsUpper(current)
                    && !char.IsWhiteSpace(value[index - 1])
                    && !char.IsUpper(value[index - 1]))
                {
                    chars.Add(' ');
                }

                chars.Add(current);
            }

            var rawLabel = string.Join(
                " ",
                new string(chars.ToArray())
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries));
            var directKey = rawLabel.Replace(" ", "", StringComparison.Ordinal);
            if (FieldLabels.TryGetValue(directKey, out var directLabel))
            {
                return directLabel;
            }

            var translatedWords = rawLabel
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(word => WordLabels.TryGetValue(word, out var translated) ? translated : word.ToLowerInvariant());

            return string.Join(" ", translatedWords);
        }

        private static string Key(string entityName, string fieldName)
        {
            return $"{entityName.Trim().ToLowerInvariant()}::{fieldName.Trim().ToLowerInvariant()}";
        }

        private static readonly HashSet<string> SingularLabels = new(StringComparer.OrdinalIgnoreCase)
        {
            "personne",
            "contrat",
            "produit",
            "marque",
            "assureur",
            "support financier",
            "opération",
            "clause bénéficiaire",
            "rôle",
            "permission",
            "description de champ",
            "élément de configuration",
            "flux d'intégration",
            "donateur",
            "don",
            "reçu fiscal",
            "profil fiscal",
            "avance",
            "compartiment"
        };

        private static readonly Dictionary<string, (string Singular, string Plural)> TableLabels = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Persons"] = ("personne", "personnes"),
            ["Person"] = ("personne", "personnes"),
            ["Contracts"] = ("contrat", "contrats"),
            ["Contract"] = ("contrat", "contrats"),
            ["Products"] = ("produit", "produits"),
            ["Product"] = ("produit", "produits"),
            ["Brands"] = ("marque", "marques"),
            ["Brand"] = ("marque", "marques"),
            ["Insurers"] = ("assureur", "assureurs"),
            ["Insurer"] = ("assureur", "assureurs"),
            ["FinancialSupports"] = ("support financier", "supports financiers"),
            ["FinancialSupport"] = ("support financier", "supports financiers"),
            ["Operations"] = ("opération", "opérations"),
            ["Operation"] = ("opération", "opérations"),
            ["BeneficiaryClauses"] = ("clause bénéficiaire", "clauses bénéficiaires"),
            ["BeneficiaryClause"] = ("clause bénéficiaire", "clauses bénéficiaires"),
            ["Roles"] = ("rôle", "rôles"),
            ["Role"] = ("rôle", "rôles"),
            ["Permissions"] = ("permission", "permissions"),
            ["Permission"] = ("permission", "permissions"),
            ["FieldDescriptions"] = ("description de champ", "descriptions de champs"),
            ["FieldDescription"] = ("description de champ", "descriptions de champs"),
            ["ConfigurationItems"] = ("élément de configuration", "éléments de configuration"),
            ["ConfigurationItem"] = ("élément de configuration", "éléments de configuration"),
            ["IntegrationFlows"] = ("flux d'intégration", "flux d'intégration"),
            ["IntegrationFlow"] = ("flux d'intégration", "flux d'intégration"),
            ["Donors"] = ("donateur", "donateurs"),
            ["Donor"] = ("donateur", "donateurs"),
            ["Donations"] = ("don", "dons"),
            ["Donation"] = ("don", "dons"),
            ["TaxReceipts"] = ("reçu fiscal", "reçus fiscaux"),
            ["TaxReceipt"] = ("reçu fiscal", "reçus fiscaux"),
            ["TaxProfiles"] = ("profil fiscal", "profils fiscaux"),
            ["TaxProfile"] = ("profil fiscal", "profils fiscaux"),
            ["Advances"] = ("avance", "avances"),
            ["Advance"] = ("avance", "avances"),
            ["Compartments"] = ("compartiment", "compartiments"),
            ["Compartment"] = ("compartiment", "compartiments")
        };

        private static readonly Dictionary<string, string> FieldLabels = new(StringComparer.OrdinalIgnoreCase)
        {
            ["FirstName"] = "prénom",
            ["LastName"] = "nom",
            ["BirthDate"] = "date de naissance",
            ["BirthPlace"] = "lieu de naissance",
            ["BirthCity"] = "commune de naissance",
            ["BirthCountry"] = "pays de naissance",
            ["ContractNumber"] = "numéro de contrat",
            ["ContractLabel"] = "libellé du contrat",
            ["ProductCode"] = "code produit",
            ["ProductName"] = "nom du produit",
            ["BrandCode"] = "code marque",
            ["BrandName"] = "nom de la marque",
            ["OperationDate"] = "date d'opération",
            ["CreatedDate"] = "date de création",
            ["UpdatedDate"] = "date de mise à jour",
            ["CreatedAt"] = "date de création",
            ["UpdatedAt"] = "date de mise à jour",
            ["EffectiveDate"] = "date d'effet",
            ["SubscriptionDate"] = "date de souscription",
            ["CurrentValue"] = "valeur actuelle",
            ["RedemptionValue"] = "valeur de rachat",
            ["InitialPremium"] = "prime initiale",
            ["TotalPaidPremiums"] = "total des primes versées",
            ["RequestedAmount"] = "montant demandé",
            ["ApprovedAmount"] = "montant accordé",
            ["OutstandingCapital"] = "capital restant dû",
            ["InterestRate"] = "taux d'intérêt",
            ["ReceiptNumber"] = "numéro de reçu",
            ["DonationDate"] = "date du don",
            ["PaymentStatus"] = "statut du paiement",
            ["EntityName"] = "nom de l'entité",
            ["FieldName"] = "nom du champ",
            ["IsActive"] = "actif",
            ["IsSystem"] = "système",
            ["IsClosed"] = "clôturé",
            ["IsDefault"] = "par défaut",
            ["Locked"] = "verrouillé"
        };

        private static readonly Dictionary<string, string> WordLabels = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Id"] = "identifiant",
            ["Code"] = "code",
            ["Name"] = "nom",
            ["First"] = "premier",
            ["Last"] = "dernier",
            ["Label"] = "libellé",
            ["Title"] = "titre",
            ["Description"] = "description",
            ["Comment"] = "commentaire",
            ["Note"] = "note",
            ["Reason"] = "motif",
            ["Date"] = "date",
            ["At"] = "à",
            ["Created"] = "création",
            ["Updated"] = "mise à jour",
            ["Deleted"] = "suppression",
            ["Birth"] = "naissance",
            ["Country"] = "pays",
            ["City"] = "ville",
            ["Address"] = "adresse",
            ["Postal"] = "postal",
            ["Phone"] = "téléphone",
            ["Email"] = "e-mail",
            ["Number"] = "numéro",
            ["Reference"] = "référence",
            ["Registration"] = "immatriculation",
            ["Status"] = "statut",
            ["State"] = "état",
            ["Type"] = "type",
            ["Kind"] = "nature",
            ["Category"] = "catégorie",
            ["Family"] = "famille",
            ["Nature"] = "nature",
            ["Amount"] = "montant",
            ["Value"] = "valeur",
            ["Capital"] = "capital",
            ["Premium"] = "prime",
            ["Balance"] = "solde",
            ["Rate"] = "taux",
            ["Percentage"] = "pourcentage",
            ["Percent"] = "pourcentage",
            ["Quantity"] = "quantité",
            ["Shares"] = "parts",
            ["Units"] = "unités",
            ["Currency"] = "devise",
            ["Is"] = "est",
            ["Has"] = "possède",
            ["Can"] = "peut",
            ["Enabled"] = "activé",
            ["Active"] = "actif",
            ["Locked"] = "verrouillé",
            ["File"] = "fichier",
            ["Document"] = "document",
            ["Content"] = "contenu",
            ["Text"] = "texte",
            ["Data"] = "données",
            ["Payload"] = "charge utile",
            ["Hash"] = "empreinte",
            ["Token"] = "jeton",
            ["Secret"] = "secret",
            ["Version"] = "version",
            ["Order"] = "ordre",
            ["Rank"] = "rang",
            ["Priority"] = "priorité",
            ["Position"] = "position"
        };
    }

    internal static class FieldDescriptionCatalog
    {
        public static readonly IReadOnlyList<FieldDescriptionDefinition> All =
        [
            // Personnes
            new("Persons", "firstName", "Prénom officiel de la personne, utilisé dans les contrats, documents et recherches."),
            new("Persons", "lastName", "Nom de famille officiel de la personne."),
            new("Persons", "birthDate", "Date de naissance utilisée pour l'identification, la fiscalité et les contrôles d'éligibilité."),
            new("Persons", "birthPlace", "Lieu de naissance déclaré lorsque le détail INSEE n'est pas disponible."),
            new("Persons", "birthCity", "Commune de naissance normalisée avec les données géographiques lorsqu'elles sont disponibles."),
            new("Persons", "birthCountry", "Pays de naissance de la personne."),
            new("Persons", "email1", "Adresse e-mail principale pour les notifications et échanges courants."),
            new("Persons", "email2", "Adresse e-mail secondaire facultative."),
            new("Persons", "phone1", "Numéro de téléphone principal."),
            new("Persons", "phone2", "Numéro de téléphone secondaire facultatif."),
            new("Persons", "address", "Adresse postale principale de la personne."),
            new("Persons", "fiscalAddress", "Adresse retenue pour les besoins fiscaux lorsqu'elle diffère de l'adresse principale."),
            new("Persons", "sex", "Civilité ou sexe administratif utilisé pour certaines restitutions documentaires."),
            new("Persons", "status", "Statut de suivi de la fiche personne."),
            new("Persons", "locked", "Indique si la fiche est verrouillée pour limiter les modifications sensibles."),

            // Contrats
            new("Contracts", "contractNumber", "Numéro unique du contrat chez l'assureur ou dans le référentiel interne."),
            new("Contracts", "contractLabel", "Libellé court permettant d'identifier rapidement le contrat."),
            new("Contracts", "personId", "Souscripteur ou personne principale rattachée au contrat."),
            new("Contracts", "productId", "Produit ou enveloppe commerciale sur lequel le contrat est ouvert."),
            new("Contracts", "beneficiaryClauseId", "Clause bénéficiaire actuellement liée au contrat."),
            new("Contracts", "subscriptionDate", "Date de souscription ou de prise d'effet initiale du contrat."),
            new("Contracts", "effectiveDate", "Date à partir de laquelle les garanties ou effets contractuels s'appliquent."),
            new("Contracts", "contractStatus", "État de vie du contrat: actif, suspendu, clos ou autre statut métier."),
            new("Contracts", "contractType", "Nature opérationnelle du contrat."),
            new("Contracts", "contractFamily", "Famille fiscale ou assurantielle du contrat."),
            new("Contracts", "initialPremium", "Versement initial ayant permis l'ouverture du contrat."),
            new("Contracts", "totalPaidPremiums", "Cumul des primes versées depuis l'origine du contrat."),
            new("Contracts", "currentValue", "Valeur actuelle estimée du contrat."),
            new("Contracts", "redemptionValue", "Valeur de rachat estimée ou calculée."),
            new("Contracts", "currency", "Devise de référence du contrat."),
            new("Contracts", "managementFeesRate", "Taux de frais de gestion applicable au contrat lorsque défini au niveau contrat."),
            new("Contracts", "entryFeesRate", "Taux de frais d'entrée applicable aux versements."),
            new("Contracts", "exitFeesRate", "Taux de frais de sortie ou de rachat lorsqu'il existe."),
            new("Contracts", "scheduledPayment", "Montant de versement programmé rattaché au contrat."),
            new("Contracts", "compartments", "Poches ou compartiments de gestion associés au contrat."),
            new("Contracts", "supports", "Supports financiers détenus ou sélectionnés dans le contrat."),
            new("Contracts", "options", "Options contractuelles actives ou disponibles."),

            // Produits
            new("Products", "productCode", "Code interne ou commercial du produit."),
            new("Products", "productName", "Nom commercial du produit."),
            new("Products", "insurerId", "Assureur porteur du produit."),
            new("Products", "brandId", "Marque ou distributeur associé au produit."),
            new("Products", "contractFamily", "Famille de contrat ciblée par le produit."),
            new("Products", "productTypeId", "Type de produit issu du référentiel produit."),
            new("Products", "lifecycleStatus", "Statut du cycle de vie: brouillon, actif, archivé ou retiré."),
            new("Products", "commercialLaunchDate", "Date de lancement commercial du produit."),
            new("Products", "commercialEndDate", "Date de fin de commercialisation si le produit n'est plus ouvert à la vente."),
            new("Products", "minimumInitialPayment", "Montant minimal requis pour ouvrir un contrat sur ce produit."),
            new("Products", "minimumAdditionalPayment", "Montant minimal d'un versement libre ultérieur."),
            new("Products", "minimumScheduledPayment", "Montant minimal d'un versement programmé."),
            new("Products", "managementModes", "Modes de gestion proposés par le produit."),
            new("Products", "managementFeePolicy", "Politique de frais de gestion appliquée par défaut au produit."),
            new("Products", "operationFeePolicies", "Règles de frais applicables aux opérations du produit."),
            new("Products", "documentAssignments", "Documents contractuels ou précontractuels rattachés au produit."),

            // Marques
            new("Brands", "brandCode", "Code court et stable de la marque."),
            new("Brands", "brandName", "Nom public de la marque."),
            new("Brands", "slogan", "Phrase de positionnement ou promesse commerciale."),
            new("Brands", "description", "Présentation générale de la marque."),
            new("Brands", "logoUrl", "Adresse ou chemin du logo utilisé dans l'application."),
            new("Brands", "website", "Site web officiel de la marque."),
            new("Brands", "contactEmail", "Adresse e-mail de contact principale."),
            new("Brands", "country", "Pays de rattachement de la marque."),
            new("Brands", "city", "Ville principale ou siège de la marque."),
            new("Brands", "foundedYear", "Année de création de la marque."),
            new("Brands", "founder", "Fondateur ou origine de la marque."),
            new("Brands", "industry", "Secteur d'activité principal."),
            new("Brands", "mainColor", "Couleur dominante utilisée pour l'identité visuelle."),
            new("Brands", "parentGroup", "Groupe parent ou maison mère."),
            new("Brands", "notes", "Notes internes libres sur la marque."),
            new("Brands", "facebookUrl", "Lien vers la page Facebook officielle."),
            new("Brands", "instagramUrl", "Lien vers le compte Instagram officiel."),
            new("Brands", "linkedInUrl", "Lien vers la page LinkedIn officielle."),
            new("Brands", "isActive", "Indique si la marque est active dans les parcours et référentiels."),

            // Assureurs
            new("Insurers", "name", "Nom de l'assureur ou de la compagnie porteuse."),
            new("Insurers", "code", "Code interne ou référentiel de l'assureur."),
            new("Insurers", "registrationNumber", "Numéro d'immatriculation ou identifiant officiel."),
            new("Insurers", "country", "Pays d'établissement de l'assureur."),
            new("Insurers", "address", "Adresse du siège ou de contact de l'assureur."),
            new("Insurers", "email", "Adresse e-mail principale de contact."),
            new("Insurers", "phone", "Numéro de téléphone principal."),
            new("Insurers", "website", "Site web officiel de l'assureur."),
            new("Insurers", "isActive", "Indique si l'assureur est disponible pour les nouveaux rattachements."),

            // Supports financiers
            new("FinancialSupports", "isin", "Code ISIN du support financier, lorsqu'il existe."),
            new("FinancialSupports", "code", "Code interne ou technique du support."),
            new("FinancialSupports", "label", "Libellé lisible du support financier."),
            new("FinancialSupports", "supportType", "Type de support selon le référentiel financier."),
            new("FinancialSupports", "supportNature", "Nature assurance-vie du support, par exemple fonds euros ou unité de compte."),
            new("FinancialSupports", "currency", "Devise de cotation ou de référence du support."),
            new("FinancialSupports", "status", "Statut de disponibilité ou de suivi du support."),
            new("FinancialSupports", "marketingName", "Nom marketing affiché aux utilisateurs."),
            new("FinancialSupports", "legalName", "Dénomination juridique complète."),
            new("FinancialSupports", "AMFCode", "Code AMF lorsque le support est référencé par l'AMF."),
            new("FinancialSupports", "bloombergCode", "Code Bloomberg du support."),
            new("FinancialSupports", "morningstarCode", "Code Morningstar du support."),
            new("FinancialSupports", "CUSIP", "Identifiant CUSIP pour les marchés concernés."),
            new("FinancialSupports", "SEDOL", "Identifiant SEDOL pour les marchés concernés."),
            new("FinancialSupports", "assetManager", "Société de gestion responsable du support."),
            new("FinancialSupports", "depositaryBank", "Banque dépositaire du support."),
            new("FinancialSupports", "custodian", "Conservateur ou teneur de compte titres."),
            new("FinancialSupports", "inceptionDate", "Date de lancement du support."),
            new("FinancialSupports", "closureDate", "Date de clôture ou de fermeture du support."),
            new("FinancialSupports", "isClosed", "Indique si le support est fermé."),
            new("FinancialSupports", "assetClass", "Classe d'actifs principale du support."),
            new("FinancialSupports", "subAssetClass", "Sous-classe d'actifs du support."),
            new("FinancialSupports", "geographicFocus", "Zone géographique d'investissement dominante."),
            new("FinancialSupports", "sectorFocus", "Secteur économique cible ou dominant."),
            new("FinancialSupports", "capitalizationPolicy", "Politique de capitalisation ou de distribution des revenus."),
            new("FinancialSupports", "investmentStrategy", "Stratégie d'investissement du support."),
            new("FinancialSupports", "legalForm", "Forme juridique du support."),
            new("FinancialSupports", "managementStyle", "Style de gestion, par exemple active ou indicielle."),
            new("FinancialSupports", "ucitsCategory", "Catégorie UCITS/OPCVM lorsque applicable."),
            new("FinancialSupports", "minimumSubscription", "Montant minimal de souscription du support."),
            new("FinancialSupports", "minimumHolding", "Montant minimal de conservation sur le support."),
            new("FinancialSupports", "internalManagementFeeRate", "Taux de frais de gestion interne du support."),
            new("FinancialSupports", "contractManagementFeeOverrideEnabled", "Active une surcharge de frais de gestion spécifique pour les contrats."),
            new("FinancialSupports", "contractManagementFeeOverrideRate", "Taux de surcharge de frais de gestion au niveau contrat."),
            new("FinancialSupports", "contractManagementFeeOverrideFrequency", "Fréquence d'application de la surcharge de frais."),
            new("FinancialSupports", "contractManagementFeeOverrideProrataMethod", "Méthode de prorata utilisée pour calculer la surcharge."),
            new("FinancialSupports", "contractManagementFeeOverridePostingMode", "Mode de comptabilisation de la surcharge de frais."),
            new("FinancialSupports", "contractManagementFeeOverrideEffectiveDate", "Date de début d'application de la surcharge."),
            new("FinancialSupports", "contractManagementFeeOverrideEndDate", "Date de fin d'application de la surcharge."),
            new("FinancialSupports", "performanceFee", "Frais de performance éventuellement appliqués au support."),
            new("FinancialSupports", "turnoverRate", "Taux de rotation du portefeuille."),
            new("FinancialSupports", "aum", "Encours sous gestion du support."),
            new("FinancialSupports", "isCapitalGuaranteed", "Indique si le capital est garanti."),
            new("FinancialSupports", "isCurrencyHedged", "Indique si le risque de change est couvert."),
            new("FinancialSupports", "benchmark", "Indice de référence du support."),
            new("FinancialSupports", "hasESGLabel", "Indique si le support dispose d'un label ESG."),
            new("FinancialSupports", "esgLabel", "Nom du label ESG."),
            new("FinancialSupports", "sfdrClassification", "Classification SFDR du support."),
            new("FinancialSupports", "esgScore", "Score ESG disponible pour le support."),
            new("FinancialSupports", "esgScoreProvider", "Fournisseur du score ESG."),
            new("FinancialSupports", "mifidTargetMarket", "Marché cible MiFID du support."),
            new("FinancialSupports", "mifidRiskTolerance", "Tolérance au risque MiFID associée."),
            new("FinancialSupports", "mifidClientType", "Type de client MiFID visé."),
            new("FinancialSupports", "lastValuationAmount", "Dernière valeur liquidative ou valorisation connue."),
            new("FinancialSupports", "lastValuationDate", "Date de la dernière valorisation connue."),
            new("FinancialSupports", "weeklyVolatility", "Volatilité hebdomadaire estimée ou importée."),
            new("FinancialSupports", "maxDrawdown1Y", "Perte maximale observée sur un an."),
            new("FinancialSupports", "distributor", "Distributeur principal du support."),
            new("FinancialSupports", "isAvailableOnline", "Indique si le support est disponible en ligne."),
            new("FinancialSupports", "isAdvisedSale", "Indique si la vente conseillée est requise ou recommandée."),
            new("FinancialSupports", "isEligiblePEA", "Indique si le support est éligible au PEA."),
            new("FinancialSupports", "countryOfDistribution", "Pays de distribution du support."),
            new("FinancialSupports", "fundDomicile", "Pays ou juridiction de domiciliation du fonds."),
            new("FinancialSupports", "primaryListingMarket", "Marché de cotation principal."),
            new("FinancialSupports", "isFundOfFunds", "Indique si le support investit principalement dans d'autres fonds."),

            // Opérations
            new("Operations", "contractId", "Contrat sur lequel l'opération est appliquée."),
            new("Operations", "type", "Nature de l'opération : versement, retrait, arbitrage, avance ou frais."),
            new("Operations", "status", "Statut de traitement de l'opération."),
            new("Operations", "operationDate", "Date d'effet comptable et contractuelle de l'opération."),
            new("Operations", "amount", "Montant global de l'opération dans la devise indiquée."),
            new("Operations", "currency", "Devise utilisée pour saisir et valoriser l'opération."),
            new("Operations", "allocations", "Répartition de l'opération par poche et support financier."),
            new("Operations", "paymentMethod", "Moyen de paiement utilisé ou prévu pour l'opération."),
            new("Operations", "grossAmount", "Montant brut avant fiscalité ou retenues éventuelles."),
            new("Operations", "netAmount", "Montant net après frais, fiscalité ou ajustements."),
            new("Operations", "scheduleStatus", "Statut du programme lorsque l'opération est programmée."),

            // Clauses bénéficiaires
            new("BeneficiaryClauses", "clauseType", "Type de clause bénéficiaire, standard ou personnalisée."),
            new("BeneficiaryClauses", "title", "Titre court permettant d'identifier la clause."),
            new("BeneficiaryClauses", "description", "Commentaire ou explication interne de la clause."),
            new("BeneficiaryClauses", "clauseText", "Texte juridique de la clause bénéficiaire."),
            new("BeneficiaryClauses", "isActive", "Indique si la clause est active et applicable."),
            new("BeneficiaryClauses", "contractId", "Contrat auquel la clause est rattachée."),
            new("BeneficiaryClauses", "beneficiaries", "Liste des bénéficiaires et leurs modalités de répartition."),

            // Roles et permissions
            new("Roles", "roleCode", "Code stable utilisé par les règles d'autorisation."),
            new("Roles", "roleName", "Nom lisible du rôle."),
            new("Roles", "description", "Explication du périmètre fonctionnel du rôle."),
            new("Roles", "isSystem", "Indique si le rôle est géré par le système."),
            new("Roles", "privilegeRank", "Niveau de privilège relatif du rôle."),
            new("Permissions", "permissionCode", "Code technique de la permission."),
            new("Permissions", "permissionName", "Nom lisible de la permission."),
            new("Permissions", "description", "Explication de l'action ou du périmètre autorisé."),
            new("Permissions", "isSystem", "Indique si la permission appartient au catalogue système."),

            // Descriptions elles-mêmes
            new("FieldDescriptions", "entityName", "Nom de l'écran ou de la table auquel la description est rattachée."),
            new("FieldDescriptions", "fieldName", "Nom technique du champ documenté."),
            new("FieldDescriptions", "description", "Texte explicatif affiché dans les formulaires et fiches de lecture."),
            new("FieldDescriptions", "locked", "Indique si la description provient du catalogue système."),

            // CMDB / cartographie
            new("ConfigurationItems", "name", "Nom de l'élément de configuration."),
            new("ConfigurationItems", "code", "Code ou identifiant stable de l'élément."),
            new("ConfigurationItems", "type", "Type d'élément de configuration."),
            new("ConfigurationItems", "status", "Statut de vie de l'élément."),
            new("ConfigurationItems", "description", "Description fonctionnelle ou technique de l'élément."),
            new("ConfigurationItems", "responsibleEmployer", "Entité ou employeur responsable de l'élément."),
            new("ConfigurationItems", "applicationProfile", "Profil applicatif rattaché à l'élément."),
            new("ExchangePatterns", "code", "Code du pattern d'échange."),
            new("ExchangePatterns", "name", "Nom du pattern d'échange."),
            new("ExchangePatterns", "description", "Description du scénario ou modèle d'échange."),
            new("IntegrationFlows", "name", "Nom du flux d'intégration."),
            new("IntegrationFlows", "source", "Application ou système source du flux."),
            new("IntegrationFlows", "target", "Application ou système cible du flux."),
            new("IntegrationFlows", "technology", "Technologie ou protocole utilisé par le flux."),
            new("IntegrationFlows", "description", "Description fonctionnelle du flux."),

            // Dons et reçus fiscaux
            new("Donors", "firstName", "Prénom du donateur."),
            new("Donors", "lastName", "Nom du donateur."),
            new("Donors", "email", "Adresse e-mail de contact du donateur."),
            new("Donors", "birthDate", "Date de naissance du donateur lorsque requise."),
            new("Donors", "address", "Adresse postale utilisée pour le reçu fiscal."),
            new("Donations", "donorId", "Donateur associé au don."),
            new("Donations", "amount", "Montant du don."),
            new("Donations", "donationDate", "Date de réalisation du don."),
            new("Donations", "purpose", "Objet ou campagne du don."),
            new("Donations", "paymentStatus", "Statut du paiement du don."),
            new("BeneficiaryOrganizations", "name", "Nom de l'organisme bénéficiaire."),
            new("BeneficiaryOrganizations", "siren", "Identifiant SIREN de l'organisme."),
            new("BeneficiaryOrganizations", "address", "Adresse officielle de l'organisme."),
            new("BeneficiaryOrganizations", "isEligibleForTaxReceipt", "Indique si l'organisme peut émettre des reçus fiscaux."),
            new("TaxReceipts", "receiptNumber", "Numéro unique du reçu fiscal."),
            new("TaxReceipts", "donationId", "Don ayant donné lieu au reçu fiscal."),
            new("TaxReceipts", "issueDate", "Date d'émission du reçu fiscal."),
            new("TaxReceipts", "amount", "Montant retenu sur le reçu fiscal."),

            // Fiscalité et avances
            new("TaxProfiles", "label", "Libellé du profil fiscal."),
            new("TaxProfiles", "contractFamily", "Famille de contrat concernée par le profil fiscal."),
            new("TaxProfiles", "description", "Description des hypothèses fiscales du profil."),
            new("Advances", "advanceNumber", "Numéro de l'avance."),
            new("Advances", "contractId", "Contrat rattaché à l'avance."),
            new("Advances", "requestedAmount", "Montant demandé par le client."),
            new("Advances", "approvedAmount", "Montant accordé après instruction."),
            new("Advances", "outstandingCapital", "Capital restant dû au titre de l'avance."),
            new("Advances", "interestRate", "Taux d'intérêt applicable à l'avance."),
            new("Advances", "status", "Statut d'instruction ou de vie de l'avance."),

            // Compartiments
            new("Compartments", "label", "Libellé de la poche de gestion."),
            new("Compartments", "contractId", "Contrat auquel la poche appartient."),
            new("Compartments", "currentValue", "Valorisation actuelle de la poche."),
            new("Compartments", "isDefault", "Indique s'il s'agit de la poche principale du contrat.")
        ];
    }

    internal sealed record FieldDescriptionDefinition(
        string EntityName,
        string FieldName,
        string Description);
}

