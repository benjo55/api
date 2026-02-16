using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Data.Seed
{
    public static class ContractOptionTypeSeeder
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ContractOptionType>().HasData(
                // Gestion financière
                new ContractOptionType { Id = 1, Code = "STOP_LOSS_GAIN", Category = "Gestion financière", Label = "Arbitrage conditionnel (stop-loss/gain)", Objective = "Sécuriser gains ou limiter pertes", Mechanism = "Transfert auto vers support sécurisé quand un seuil est atteint", DefaultCost = "Gratuit ou frais d’arbitrage" },
                new ContractOptionType { Id = 2, Code = "SECURISATION_PV", Category = "Gestion financière", Label = "Sécurisation des plus-values", Objective = "Mettre à l’abri les gains", Mechanism = "Les plus-values sont transférées régulièrement vers fonds sécurisé", DefaultCost = "Souvent gratuit" },
                new ContractOptionType { Id = 3, Code = "DYNAMISATION_INTERETS", Category = "Gestion financière", Label = "Dynamisation des intérêts", Objective = "Booster la performance", Mechanism = "Les intérêts du fonds € sont investis en UC", DefaultCost = "Gratuit" },
                new ContractOptionType { Id = 4, Code = "REEQUILIBRAGE_AUTO", Category = "Gestion financière", Label = "Rééquilibrage automatique", Objective = "Maintenir allocation cible", Mechanism = "Arbitrages périodiques pour revenir à la répartition choisie", DefaultCost = "Gratuit ou frais d’arbitrage" },
                new ContractOptionType { Id = 5, Code = "INVEST_PROGRESSIF", Category = "Gestion financière", Label = "Investissement progressif", Objective = "Lisser le risque d’entrée", Mechanism = "Transfert programmé du fonds € vers UC", DefaultCost = "Gratuit" },
                new ContractOptionType { Id = 6, Code = "DESINVEST_PROGRESSIF", Category = "Gestion financière", Label = "Désinvestissement progressif", Objective = "Sécuriser progressivement le capital", Mechanism = "Transfert programmé d’UC vers fonds €", DefaultCost = "Gratuit" },

                // Garanties complémentaires
                new ContractOptionType { Id = 10, Code = "GARANTIE_PLANCHER_SIMPLE", Category = "Garanties complémentaires", Label = "Garantie plancher simple", Objective = "Protéger le capital transmis", Mechanism = "Au décès, versement min = primes nettes versées", DefaultCost = "Prime d’assurance prélevée" },
                new ContractOptionType { Id = 11, Code = "GARANTIE_PLANCHER_INDEXEE", Category = "Garanties complémentaires", Label = "Garantie plancher indexée", Objective = "Protection + rendement minimum", Mechanism = "Plancher = primes + taux garanti annuel", DefaultCost = "Plus coûteux que simple" },
                new ContractOptionType { Id = 12, Code = "GARANTIE_PLANCHER_CLIQUET", Category = "Garanties complémentaires", Label = "Garantie plancher cliquet", Objective = "Sécuriser le plus haut atteint", Mechanism = "Plancher = valeur max atteinte par le contrat", DefaultCost = "Prime plus élevée" },
                new ContractOptionType { Id = 13, Code = "GARANTIE_DECES_MAJ", Category = "Garanties complémentaires", Label = "Garantie décès majorée", Objective = "Augmenter capital transmis", Mechanism = "Valeur contrat + % supplémentaire", DefaultCost = "Prime d’assurance" },
                new ContractOptionType { Id = 14, Code = "GARANTIE_RENTE_PLANCHER", Category = "Garanties complémentaires", Label = "Garantie rente plancher", Objective = "Sécuriser un revenu minimal", Mechanism = "Garantie d’une rente viagère minimale à la sortie", DefaultCost = "Prime ou frais intégrés" },
                new ContractOptionType { Id = 15, Code = "GARANTIE_PLANCHER_PROG", Category = "Garanties complémentaires", Label = "Garantie plancher progressive", Objective = "Réduire le coût avec le temps", Mechanism = "Couverture décroissante au fil des ans", DefaultCost = "Moins coûteuse" },

                // Autres options
                new ContractOptionType { Id = 20, Code = "AVANCE", Category = "Autres options", Label = "Avances", Objective = "Liquidité sans rachat", Mechanism = "Prêt garanti par contrat avec taux d’intérêt", DefaultCost = "Intérêts sur avance" },
                new ContractOptionType { Id = 21, Code = "OPTION_RENTE", Category = "Autres options", Label = "Options de rente", Objective = "Adapter la sortie", Mechanism = "Différents modes de rente viagère ou temporaire", DefaultCost = "Impacte le montant de la rente" },
                new ContractOptionType { Id = 22, Code = "GESTION_SOUS_MANDAT", Category = "Autres options", Label = "Gestion sous mandat", Objective = "Déléguer totalement la gestion", Mechanism = "L’assureur gère selon un profil", DefaultCost = "0,2 à 0,8 %/an" },
                new ContractOptionType { Id = 23, Code = "CLAUSE_DEMEMBREE", Category = "Autres options", Label = "Clause bénéficiaire démembrée", Objective = "Optimiser la fiscalité successorale", Mechanism = "Usufruitier = conjoint / NP = enfants", DefaultCost = "Aucun coût" }
            );
        }
    }
}
