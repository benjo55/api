using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddCartographyDomainDocumentsAndExchangePatternCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TypicalUses",
                schema: "integration",
                table: "ExchangePatterns",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CartographyDomainDocuments",
                schema: "cmdb",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployerEntity = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartographyDomainDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CartographyDomainDocumentSections",
                schema: "cmdb",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CartographyDomainDocumentId = table.Column<int>(type: "int", nullable: false),
                    SectionKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    HeadingLevel = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    ContentHtml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlainText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EditorJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartographyDomainDocumentSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CartographyDomainDocumentSections_CartographyDomainDocuments_CartographyDomainDocumentId",
                        column: x => x.CartographyDomainDocumentId,
                        principalSchema: "cmdb",
                        principalTable: "CartographyDomainDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                schema: "integration",
                table: "ExchangePatterns",
                keyColumn: "Id",
                keyValue: 1,
                column: "TypicalUses",
                value: null);

            migrationBuilder.UpdateData(
                schema: "integration",
                table: "ExchangePatterns",
                keyColumn: "Id",
                keyValue: 2,
                column: "TypicalUses",
                value: null);

            migrationBuilder.UpdateData(
                schema: "integration",
                table: "ExchangePatterns",
                keyColumn: "Id",
                keyValue: 3,
                column: "TypicalUses",
                value: null);

            migrationBuilder.UpdateData(
                schema: "integration",
                table: "ExchangePatterns",
                keyColumn: "Id",
                keyValue: 4,
                column: "TypicalUses",
                value: null);

            migrationBuilder.UpdateData(
                schema: "integration",
                table: "ExchangePatterns",
                keyColumn: "Id",
                keyValue: 5,
                column: "TypicalUses",
                value: null);

            migrationBuilder.UpdateData(
                schema: "integration",
                table: "ExchangePatterns",
                keyColumn: "Id",
                keyValue: 6,
                column: "TypicalUses",
                value: null);

            migrationBuilder.UpdateData(
                schema: "integration",
                table: "ExchangePatterns",
                keyColumn: "Id",
                keyValue: 7,
                column: "TypicalUses",
                value: null);

            migrationBuilder.Sql("""
DECLARE @Patterns TABLE
(
    Code nvarchar(80) NOT NULL,
    Name nvarchar(150) NOT NULL,
    Family nvarchar(80) NOT NULL,
    InteractionMode nvarchar(30) NOT NULL,
    TriggerMode nvarchar(30) NOT NULL,
    DefaultTechnologyId int NULL,
    Description nvarchar(2000) NULL,
    TypicalUses nvarchar(2000) NULL
);

INSERT INTO @Patterns (Code, Name, Family, InteractionMode, TriggerMode, DefaultTechnologyId, Description, TypicalUses)
VALUES
(N'API_SYNC', N'API REST synchrone', N'API', N'Synchronous', N'OnDemand', 1, N'Appel HTTP synchrone avec réponse immédiate. Généralement JSON.', N'Consultation de données, opérations temps réel, Digital, applications mobiles'),
(N'API_ASYNC_CALLBACK', N'API REST asynchrone', N'API', N'Asynchronous', N'OnDemand', 1, N'L''appel retourne immédiatement un accusé de réception, le traitement est différé.', N'Traitements longs, génération de documents, workflows'),
(N'API_SOAP', N'API SOAP', N'API', N'Synchronous', N'OnDemand', NULL, N'Web Service SOAP/XML utilisant un contrat WSDL.', N'SI historiques, partenaires, banques'),
(N'API_GRAPHQL', N'API GraphQL', N'API', N'Synchronous', N'OnDemand', 1, N'API permettant au client de sélectionner les données souhaitées.', N'Portails, applications mobiles'),
(N'API_GRPC', N'API gRPC', N'API', N'Synchronous', N'OnDemand', NULL, N'Communication RPC binaire très performante.', N'Microservices, traitements intensifs'),
(N'API_ODATA', N'API OData', N'API', N'Synchronous', N'OnDemand', 1, N'API exposant des données interrogeables.', N'Reporting, référentiels'),
(N'API_GATEWAY', N'API Gateway', N'API', N'Synchronous', N'OnDemand', 1, N'Passage obligatoire via APIM (Kong, APISIX, Azure APIM...).', N'Exposition sécurisée d''API'),
(N'MESSAGING_QUEUE', N'File d''attente (Queue)', N'Messaging', N'Asynchronous', N'EventDriven', 6, N'Producteur et consommateur découplés via une file.', N'RabbitMQ, IBM MQ, Azure Service Bus'),
(N'MESSAGING_PUBSUB', N'Publish / Subscribe', N'Messaging', N'Asynchronous', N'EventDriven', 6, N'Publication d''un message à plusieurs consommateurs.', N'Notifications métier'),
(N'KAFKA_EVENT', N'Event Streaming', N'Messaging', N'Asynchronous', N'Continuous', 2, N'Diffusion continue d''événements persistants.', N'Kafka, Pulsar'),
(N'EVENT_SOURCING', N'Event Sourcing', N'Messaging', N'Asynchronous', N'EventDriven', 2, N'Les événements constituent la source de vérité métier.', N'SI modernes, audit complet'),
(N'COMMAND_BUS', N'Command Bus', N'Messaging', N'Asynchronous', N'EventDriven', 6, N'Envoi de commandes métier à un service.', N'CQRS'),
(N'NOTIFICATION', N'Notification', N'Messaging', N'Asynchronous', N'EventDriven', NULL, N'Diffusion d''une notification sans attente de réponse.', N'Alertes, mails, SMS'),
(N'WEBHOOK', N'Webhook', N'Messaging', N'Asynchronous', N'EventDriven', 1, N'Notification HTTP déclenchée automatiquement.', N'SaaS, intégrations externes'),
(N'FILE_EXPORT', N'Export de fichier', N'Batch', N'Asynchronous', N'Scheduled', NULL, N'Génération d''un fichier déposé dans un répertoire.', N'Comptabilité, partenaires'),
(N'FILE_IMPORT', N'Import de fichier', N'Batch', N'Asynchronous', N'Scheduled', NULL, N'Lecture d''un fichier provenant d''un partenaire.', N'Alimentation référentiels'),
(N'SFTP_BATCH', N'Dépôt SFTP', N'Batch', N'Asynchronous', N'Scheduled', 3, N'Échange sécurisé via SFTP.', N'Banque, ACPR, partenaires'),
(N'FTP_FTPS', N'FTP/FTPS', N'Batch', N'Asynchronous', N'Scheduled', NULL, N'Échange historique de fichiers.', N'Legacy'),
(N'BATCH_SCHEDULED', N'Batch planifié', N'Batch', N'Asynchronous', N'Scheduled', NULL, N'Traitement lancé à heure fixe.', N'Valorisation, traitements de nuit'),
(N'BATCH_EVENT', N'Batch événementiel', N'Batch', N'Asynchronous', N'EventDriven', NULL, N'Déclenchement suite à un événement.', N'Recalculs'),
(N'ETL_BATCH', N'Batch ETL', N'Batch', N'Asynchronous', N'Scheduled', 4, N'Extraction / Transformation / Chargement.', N'Alimentation Data Warehouse'),
(N'DATABASE_REPLICATION', N'Réplication de base', N'Data', N'Asynchronous', N'Continuous', 7, N'Synchronisation automatique de bases de données.', N'PRA, haute disponibilité'),
(N'CDC_STREAM', N'CDC (Change Data Capture)', N'Data', N'Asynchronous', N'Continuous', 2, N'Diffusion des modifications d''une base.', N'Kafka Connect, Debezium'),
(N'DATABASE_SYNC', N'Accès direct SQL', N'Data', N'Synchronous', N'OnDemand', 7, N'Une application lit directement une base distante.', N'Legacy'),
(N'MATERIALIZED_VIEW', N'Vue matérialisée', N'Data', N'Synchronous', N'OnDemand', 7, N'Exposition de données consolidées.', N'Reporting'),
(N'DATA_VIRTUALIZATION', N'Data Virtualization', N'Data', N'Synchronous', N'OnDemand', NULL, N'Accès logique sans duplication des données.', N'SI décisionnels'),
(N'DATA_LAKE_INGESTION', N'Data Lake ingestion', N'Data', N'Asynchronous', N'Scheduled', NULL, N'Envoi de données vers un Data Lake.', N'IA, Data Platform'),
(N'ESB', N'ESB', N'Middleware', N'Asynchronous', N'EventDriven', 5, N'Bus de services assurant transformation et routage.', N'Mule, Talend ESB'),
(N'EAI', N'EAI', N'Middleware', N'Asynchronous', N'EventDriven', NULL, N'Intégration applicative historique.', N'Tibco, WebMethods'),
(N'ORCHESTRATION', N'Orchestration', N'Middleware', N'Asynchronous', N'OnDemand', NULL, N'Un moteur orchestre plusieurs services.', N'Apache NiFi, Camunda'),
(N'CHOREOGRAPHY', N'Chorégraphie', N'Middleware', N'Asynchronous', N'EventDriven', NULL, N'Les applications collaborent sans orchestrateur central.', N'Microservices'),
(N'MEDIATION', N'Médiation', N'Middleware', N'Asynchronous', N'EventDriven', 5, N'Transformation de protocoles et formats.', N'ESB'),
(N'INTELLIGENT_ROUTING', N'Routage intelligent', N'Middleware', N'Asynchronous', N'EventDriven', 5, N'Choix dynamique du destinataire.', N'ESB'),
(N'SPLITTER_AGGREGATOR', N'Splitter / Aggregator', N'Middleware', N'Asynchronous', N'EventDriven', NULL, N'Découpage puis regroupement de messages.', N'Enterprise Integration Patterns'),
(N'CONTENT_BASED_ROUTER', N'Content Based Router', N'Middleware', N'Asynchronous', N'EventDriven', NULL, N'Routage selon le contenu du message.', N'NiFi, Camel'),
(N'MESSAGE_TRANSLATOR', N'Message Translator', N'Middleware', N'Asynchronous', N'EventDriven', NULL, N'Conversion XML ↔ JSON ↔ CSV...', N'Intégration'),
(N'CANONICAL_DATA_MODEL', N'Canonical Data Model', N'Middleware', N'Asynchronous', N'EventDriven', NULL, N'Transformation vers un modèle de données commun.', N'Urbanisation SI'),
(N'SOCKET_TCP', N'Socket TCP', N'Temps réel', N'Synchronous', N'Continuous', NULL, N'Communication réseau directe.', N'Applications industrielles'),
(N'WEBSOCKET', N'WebSocket', N'Temps réel', N'Synchronous', N'Continuous', NULL, N'Canal bidirectionnel permanent.', N'Supervision, temps réel'),
(N'SERVER_SENT_EVENTS', N'Server Sent Events', N'Temps réel', N'Asynchronous', N'Continuous', NULL, N'Flux serveur → client.', N'Notifications'),
(N'VIDEO_STREAMING', N'Streaming vidéo', N'Temps réel', N'Asynchronous', N'Continuous', NULL, N'Diffusion multimédia.', N'Visioconférence'),
(N'MTLS', N'mTLS', N'Sécurité', N'Synchronous', N'OnDemand', NULL, N'Communication avec authentification mutuelle.', N'Flux sensibles'),
(N'OAUTH2', N'OAuth2', N'Sécurité', N'Synchronous', N'OnDemand', NULL, N'Autorisation d''accès API.', N'APIs'),
(N'OPENID_CONNECT', N'OpenID Connect', N'Sécurité', N'Synchronous', N'OnDemand', NULL, N'Authentification utilisateur.', N'Portails'),
(N'ELECTRONIC_SIGNATURE', N'Signature électronique', N'Sécurité', N'Asynchronous', N'OnDemand', NULL, N'Signature des messages.', N'Banque'),
(N'PGP_ENCRYPTION', N'Chiffrement PGP', N'Sécurité', N'Asynchronous', N'Scheduled', NULL, N'Chiffrement des fichiers.', N'Échanges partenaires'),
(N'LLM_CALL', N'Appel LLM', N'IA', N'Synchronous', N'OnDemand', 1, N'Appel d''un moteur d''IA générative.', N'Copilotes'),
(N'RAG', N'RAG', N'IA', N'Synchronous', N'OnDemand', 1, N'Recherche documentaire avant génération.', N'Chatbots'),
(N'EMBEDDING', N'Embedding', N'IA', N'Asynchronous', N'Scheduled', NULL, N'Indexation vectorielle.', N'Recherche sémantique'),
(N'AI_AGENT', N'Agent IA', N'IA', N'Asynchronous', N'EventDriven', NULL, N'Communication entre applications et agents.', N'Orchestration IA'),
(N'CENTRALIZED_LOGS', N'Logs centralisés', N'Supervision', N'Asynchronous', N'Continuous', NULL, N'Envoi des journaux techniques.', N'ELK, Loki'),
(N'PROMETHEUS_METRICS', N'Métriques Prometheus', N'Supervision', N'Asynchronous', N'Continuous', NULL, N'Publication de métriques.', N'Grafana'),
(N'OPENTELEMETRY_TRACES', N'Traces OpenTelemetry', N'Supervision', N'Asynchronous', N'Continuous', NULL, N'Traces distribuées.', N'Observabilité'),
(N'HEALTH_CHECK', N'Health Check', N'Supervision', N'Synchronous', N'OnDemand', NULL, N'Verification de disponibilité.', N'Monitoring'),
(N'HEARTBEAT', N'Heartbeat', N'Supervision', N'Asynchronous', N'Continuous', NULL, N'Signal périodique attestant qu''un composant est vivant.', N'Supervision');

MERGE [integration].[ExchangePatterns] AS target
USING @Patterns AS source
    ON target.[Code] = source.[Code]
WHEN MATCHED THEN
    UPDATE SET
        [Name] = source.[Name],
        [Family] = source.[Family],
        [InteractionMode] = source.[InteractionMode],
        [TriggerMode] = source.[TriggerMode],
        [DefaultTechnologyId] = source.[DefaultTechnologyId],
        [Description] = source.[Description],
        [TypicalUses] = source.[TypicalUses],
        [IsActive] = CAST(1 AS bit),
        [IsSystem] = CAST(1 AS bit),
        [UpdatedDate] = SYSUTCDATETIME()
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([Code], [Name], [Family], [InteractionMode], [TriggerMode], [DefaultTechnologyId], [Description], [TypicalUses], [IsActive], [IsSystem], [Locked], [CreatedDate])
    VALUES (source.[Code], source.[Name], source.[Family], source.[InteractionMode], source.[TriggerMode], source.[DefaultTechnologyId], source.[Description], source.[TypicalUses], CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), SYSUTCDATETIME());
""");

            migrationBuilder.CreateIndex(
                name: "IX_CartographyDomainDocuments_EmployerEntity",
                schema: "cmdb",
                table: "CartographyDomainDocuments",
                column: "EmployerEntity",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartographyDomainDocumentSections_CartographyDomainDocumentId_SectionKey",
                schema: "cmdb",
                table: "CartographyDomainDocumentSections",
                columns: new[] { "CartographyDomainDocumentId", "SectionKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CartographyDomainDocumentSections",
                schema: "cmdb");

            migrationBuilder.DropTable(
                name: "CartographyDomainDocuments",
                schema: "cmdb");

            migrationBuilder.DropColumn(
                name: "TypicalUses",
                schema: "integration",
                table: "ExchangePatterns");
        }
    }
}
