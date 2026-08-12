using System.Globalization;
using System.Net;
using System.Text;
using api.Interfaces;
using api.Interfaces.Documents;
using api.Services.Documents.Models;

namespace api.Services.Documents.Renderers
{
    public sealed class BoostSimulationHtmlPdfRenderer : IDocumentRenderer
    {
        private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

        private static readonly string[] Principles =
        [
            "Le surplus de PB s'applique sur la collecte nette (collecte brute - rachats - arbitrages sortants) sur le fonds euro et s'obtient en sommant les montants de surplus de PB correspondant aux investissements comme aux désinvestissements.",
            "La part d'épargne déjà détenue sur le fonds euro avant investissement lors d'une campagne de collecte pendant la période d'éligibilité n'est pas éligible au boost de PB.",
            "Une campagne de collecte correspond à une période d'exercice pendant laquelle tout investissement effectué sur le fonds euro est éligible au surplus de PB.",
            "La durée d'une campagne de collecte doit être d'un minimum de 6 mois. Deux campagnes sur l'année d'exercice correspondent donc au S1 et au S2.",
            "Un investissement sur le fonds euro est éligible au surplus de PB si sa date d'effet est comprise entre les dates de début et de fin de la campagne.",
            "Un désinvestissement sur le fonds euro effectué pendant une campagne génère un surplus de PB négatif si un versement éligible a été effectué pendant la campagne.",
            "Le calcul du surplus de PB prend en compte la date d'effet, le nombre de jours restant jusqu'au 31 décembre et le taux annuel applicable.",
            "Le montant total du surplus de PB reste soumis aux prélèvements sociaux."
        ];

        private readonly IPdfGenerationService _pdfGenerationService;

        public BoostSimulationHtmlPdfRenderer(IPdfGenerationService pdfGenerationService)
        {
            _pdfGenerationService = pdfGenerationService;
        }

        public async Task<RenderedDocument> RenderAsync(
            object model,
            DocumentDefinition definition,
            DocumentGenerationContext context,
            CancellationToken cancellationToken = default)
        {
            var document = (BoostSimulationDocumentModel)model;
            var html = RenderHtml(document);
            var pdf = await _pdfGenerationService.GeneratePdfAsync(
                html,
                definition.EffectiveRenderOptions.PageSize,
                cancellationToken);

            return new RenderedDocument(
                new MemoryStream(pdf),
                "application/pdf",
                $"{document.FileName}.pdf",
                new Dictionary<string, string>
                {
                    ["collecteId"] = document.Collecte.Id.ToString(CultureInfo.InvariantCulture),
                    ["operationsCount"] = document.Operations.Count.ToString(CultureInfo.InvariantCulture),
                    ["totalBoost"] = document.Operations.Sum(operation => operation.MontantBoost).ToString(CultureInfo.InvariantCulture)
                });
        }

        private static string RenderHtml(BoostSimulationDocumentModel document)
        {
            var totalOperations = document.Operations.Sum(operation => operation.MontantOperation);
            var totalBoost = document.Operations.Sum(operation => operation.MontantBoost);
            var clientName = $"{document.Collecte.PrenomClient} {document.Collecte.NomClient}".Trim();
            var rows = string.Join(Environment.NewLine, document.Operations.Select(RenderOperationRow));
            var principles = string.Join(Environment.NewLine, Principles.Select((principle, index) =>
                $"""
                <li><strong>Principe {index + 1}</strong> - {Encode(principle)}</li>
                """));

            return $$"""
            <!doctype html>
            <html lang="fr">
            <head>
              <meta charset="utf-8">
              <style>
                @page { size: A4; margin: 16mm 14mm; }
                * { box-sizing: border-box; }
                body { margin: 0; color: #0f172a; font-family: Arial, Helvetica, sans-serif; font-size: 11px; line-height: 1.45; }
                header { display: flex; align-items: center; justify-content: space-between; border-bottom: 2px solid #1d4ed8; padding-bottom: 10px; margin-bottom: 18px; }
                .brand { font-size: 18px; font-weight: 800; color: #1e3a8a; }
                .tagline { color: #64748b; font-size: 10px; }
                h1 { margin: 0 0 6px; font-size: 23px; color: #0f172a; }
                h2 { margin: 20px 0 8px; font-size: 14px; color: #1e3a8a; }
                .summary { display: grid; grid-template-columns: repeat(3, 1fr); gap: 8px; margin: 14px 0 18px; }
                .metric { border: 1px solid #cbd5e1; border-radius: 6px; padding: 10px; background: #f8fafc; }
                .metric-label { color: #64748b; font-size: 9px; text-transform: uppercase; }
                .metric-value { margin-top: 4px; font-size: 15px; font-weight: 700; }
                table { width: 100%; border-collapse: collapse; margin-top: 8px; }
                th { background: #eaf2ff; color: #172554; text-align: left; font-weight: 700; }
                th, td { border: 1px solid #cbd5e1; padding: 7px 8px; vertical-align: top; }
                td.money, th.money { text-align: right; white-space: nowrap; }
                tfoot td { background: #f1f5f9; font-weight: 700; }
                .notice { margin-top: 16px; padding: 10px 12px; border-left: 4px solid #1d4ed8; background: #eff6ff; color: #1e3a8a; }
                ol { padding-left: 18px; }
                li { margin-bottom: 6px; }
              </style>
            </head>
            <body>
              <header>
                <div>
                  <div class="brand">APICIL Épargne</div>
                  <div class="tagline">Simulation de surplus de participation aux bénéfices</div>
                </div>
                <div>{{Encode(DateTime.UtcNow.ToString("dd/MM/yyyy", Fr))}}</div>
              </header>
              <main>
                <h1>{{Encode(document.Collecte.DescriptionCollecte)}}</h1>
                <p>Votre simulation de surplus de PB pour <strong>{{Encode(string.IsNullOrWhiteSpace(clientName) ? "Client non renseigné" : clientName)}}</strong>.</p>
                <section class="summary">
                  <div class="metric"><div class="metric-label">Taux S1</div><div class="metric-value">{{FormatPercent(document.Collecte.TauxCollecte1)}}</div></div>
                  <div class="metric"><div class="metric-label">Taux S2</div><div class="metric-value">{{FormatPercent(document.Collecte.TauxCollecte2)}}</div></div>
                  <div class="metric"><div class="metric-label">Boost total</div><div class="metric-value">{{FormatMoney(totalBoost)}}</div></div>
                </section>
                <h2>Liste des opérations</h2>
                <table>
                  <thead>
                    <tr>
                      <th>Date</th>
                      <th>Description</th>
                      <th>Catégorie</th>
                      <th class="money">Montant</th>
                      <th class="money">Éligible S1</th>
                      <th class="money">Éligible S2</th>
                      <th class="money">Boost</th>
                    </tr>
                  </thead>
                  <tbody>
                    {{rows}}
                  </tbody>
                  <tfoot>
                    <tr>
                      <td colspan="3">Total</td>
                      <td class="money">{{FormatMoney(totalOperations)}}</td>
                      <td class="money"></td>
                      <td class="money"></td>
                      <td class="money">{{FormatMoney(totalBoost)}}</td>
                    </tr>
                  </tfoot>
                </table>
                <div class="notice">Ce document est une simulation. Les montants définitifs restent soumis aux contrôles et aux règles contractuelles applicables.</div>
                <h2>Conditions générales de l'opération Boost</h2>
                <ol>
                  {{principles}}
                </ol>
              </main>
            </body>
            </html>
            """;
        }

        private static string RenderOperationRow(BoostOperationModel operation) =>
            $"""
            <tr>
              <td>{Encode(operation.DateOperation.ToString("dd/MM/yyyy", Fr))}</td>
              <td>{Encode(operation.DescriptionOperation)}</td>
              <td>{Encode(operation.CategorieOperation)}</td>
              <td class="money">{FormatMoney(operation.MontantOperation)}</td>
              <td class="money">{FormatMoney(operation.EligibleS1)}</td>
              <td class="money">{FormatMoney(operation.EligibleS2)}</td>
              <td class="money">{FormatMoney(operation.MontantBoost)}</td>
            </tr>
            """;

        private static string FormatMoney(decimal amount) => string.Format(Fr, "{0:N2} EUR", amount);

        private static string FormatPercent(decimal value) => string.Format(Fr, "{0:0.##} %", value);

        private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
    }
}
