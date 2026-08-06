using api.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Playwright;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace api.Services.LegalDocuments
{
    public sealed class PdfGenerationService : IPdfGenerationService
    {
        private static readonly Regex PageMarkerRegex = new(
            @"\[\[PDF_TARGET_(\d+)\]\]",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly ILogger<PdfGenerationService> _logger;

        public PdfGenerationService()
            : this(NullLogger<PdfGenerationService>.Instance)
        {
        }

        public PdfGenerationService(ILogger<PdfGenerationService> logger)
        {
            _logger = logger;
        }

        public async Task<byte[]> GeneratePdfAsync(string html, string pageFormat, CancellationToken cancellationToken = default)
        {
            try
            {
                using var playwright = await Playwright.CreateAsync();
                await using var browser = await LaunchBrowserAsync(playwright);

                var page = await browser.NewPageAsync();
                await page.RouteAsync("**/*", route =>
                {
                    var url = route.Request.Url;
                    if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
                        url.StartsWith("about:", StringComparison.OrdinalIgnoreCase))
                    {
                        return route.ContinueAsync();
                    }

                    return route.AbortAsync();
                });

                await page.SetContentAsync(html, new PageSetContentOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle
                });

                var pdfOptions = new PagePdfOptions
                {
                    Format = pageFormat,
                    PrintBackground = true,
                    PreferCSSPageSize = true,
                    DisplayHeaderFooter = true,
                    HeaderTemplate = "<div></div>",
                    FooterTemplate = """
                        <div style="box-sizing:border-box;width:100%;padding:0 14mm;color:#64748b;font-family:Arial,Helvetica,sans-serif;font-size:8px;text-align:right;">
                          Page <span class="pageNumber"></span> / <span class="totalPages"></span>
                        </div>
                        """
                };

                var firstPass = await page.PdfAsync(pdfOptions);
                var pageNumbers = ResolveTargetPageNumbers(firstPass);
                if (pageNumbers.Count > 0)
                {
                    await page.EvaluateAsync(
                        """
                        pageNumbers => {
                          document.querySelectorAll("[data-toc-target]").forEach(element => {
                            const targetId = element.getAttribute("data-toc-target");
                            const pageNumber = pageNumbers[targetId];
                            if (pageNumber) {
                              element.textContent = String(pageNumber);
                            }
                          });
                        }
                        """,
                        pageNumbers);
                }

                return await page.PdfAsync(pdfOptions);
            }
            catch (Exception ex)
            {
                var message = BuildFailureMessage(ex);
                _logger.LogError(ex, "PDF generation failed. PageFormat={PageFormat}; Diagnostic={Diagnostic}", pageFormat, message);
                throw new InvalidOperationException(message, ex);
            }
        }

        private async Task<IBrowser> LaunchBrowserAsync(IPlaywright playwright)
        {
            Exception? firstFailure = null;
            foreach (var channel in GetBrowserChannels())
            {
                try
                {
                    return await playwright.Chromium.LaunchAsync(CreateLaunchOptions(channel));
                }
                catch (Exception ex) when (LooksLikePlaywrightRuntimeIssue(ex))
                {
                    firstFailure ??= ex;
                    _logger.LogWarning(
                        ex,
                        "Chromium launch failed for Channel={Channel}; trying next browser option.",
                        channel ?? "playwright");
                }
            }

            throw firstFailure ?? new InvalidOperationException("Aucun navigateur compatible Playwright n'est disponible.");
        }

        private static IEnumerable<string?> GetBrowserChannels()
        {
            yield return null;

            if (OperatingSystem.IsWindows())
            {
                yield return "msedge";
                yield return "chrome";
            }
        }

        private static BrowserTypeLaunchOptions CreateLaunchOptions(string? channel)
        {
            var options = new BrowserTypeLaunchOptions
            {
                Headless = true,
                Channel = channel,
            };

            if (!OperatingSystem.IsWindows())
            {
                options.Args = ["--no-sandbox", "--disable-dev-shm-usage"];
            }

            return options;
        }

        private static string BuildFailureMessage(Exception exception)
        {
            var root = exception;
            while (root.InnerException is not null)
            {
                root = root.InnerException;
            }

            var details = root.Message.Trim();
            if (LooksLikePlaywrightRuntimeIssue(root))
            {
                return "La génération PDF a échoué car Chromium/Playwright n'est pas disponible sur le serveur. "
                    + "Sur Windows Server, installez Microsoft Edge ou exécutez `playwright.ps1 install chromium` "
                    + "dans le dossier publié, puis vérifiez que le compte applicatif peut lire le navigateur installé. "
                    + $"Détail: {details}";
            }

            return $"La génération PDF a échoué. Détail: {details}";
        }

        private static bool LooksLikePlaywrightRuntimeIssue(Exception exception)
        {
            var message = exception.Message;
            return exception is PlaywrightException ||
                message.Contains("Executable doesn't exist", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("Host system is missing dependencies", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("playwright install", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("chromium", StringComparison.OrdinalIgnoreCase);
        }

        private static Dictionary<string, int> ResolveTargetPageNumbers(byte[] pdf)
        {
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            using var document = PdfDocument.Open(pdf);

            for (var pageNumber = 1; pageNumber <= document.NumberOfPages; pageNumber++)
            {
                var text = document.GetPage(pageNumber).Text;
                foreach (Match match in PageMarkerRegex.Matches(text))
                {
                    result.TryAdd(match.Groups[1].Value, pageNumber);
                }
            }

            return result;
        }
    }
}
