using api.Dtos.Pdf;
using api.Interfaces;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using QRCoder;
using System.Net.Http.Json;

namespace api.Services.Pdf
{
    public sealed class PdfDocumentService : IPdfDocumentService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Dictionary<PdfDocumentType, IPdfTemplate> _templateByDocumentType;
        private readonly Dictionary<string, byte[]> _resolvedImageCache = new(StringComparer.OrdinalIgnoreCase);

        public PdfDocumentService(
            IHttpClientFactory httpClientFactory,
            IEnumerable<IPdfTemplate> templates)
        {
            _httpClientFactory = httpClientFactory;
            _templateByDocumentType = templates
                .SelectMany(template => template.SupportedDocumentTypes.Select(type => new { type, template }))
                .ToDictionary(x => x.type, x => x.template);
        }

        public async Task<PdfGeneratedFileDto> GenerateAsync(GeneratePdfRequestDto request, CancellationToken cancellationToken = default)
        {
            if (!_templateByDocumentType.TryGetValue(request.DocumentType, out var template))
            {
                throw new InvalidOperationException($"No PDF template registered for document type '{request.DocumentType}'.");
            }

            var logoImage = await ResolveImageAsync(request.LogoBase64, request.LogoUrl, cancellationToken);
            var chartImage = await ResolveImageAsync(request.ChartBase64, request.ChartUrl, cancellationToken);
            var charts = await ResolveChartsAsync(request.Charts, cancellationToken);

            if (chartImage is not null && charts.Count == 0)
            {
                charts.Add(new PdfResolvedChartDto
                {
                    Title = "Graphique",
                    Content = chartImage
                });
            }

            var qrCodeImage = GenerateQrCode(request.QrCodeContent);

            var content = template.Render(request, logoImage, qrCodeImage, chartImage, charts);

            return new PdfGeneratedFileDto
            {
                FileName = SanitizeFileName(request.FileName),
                Content = content
            };
        }

        public Task<PdfGeneratedFileDto> MergeAsync(MergePdfRequestDto request, CancellationToken cancellationToken = default)
        {
            if (request.Documents.Count == 0)
            {
                throw new ArgumentException("At least one PDF must be provided for merge.", nameof(request));
            }

            using var output = new PdfDocument();

            foreach (var document in request.Documents)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var bytes = DecodeBase64(document.Base64Content);
                using var inputStream = new MemoryStream(bytes);
                using var input = PdfReader.Open(inputStream, PdfDocumentOpenMode.Import);

                for (var i = 0; i < input.PageCount; i++)
                {
                    output.AddPage(input.Pages[i]);
                }
            }

            using var mergedStream = new MemoryStream();
            output.Save(mergedStream, false);

            return Task.FromResult(new PdfGeneratedFileDto
            {
                FileName = SanitizeFileName(request.FileName),
                Content = mergedStream.ToArray()
            });
        }

        private static string SanitizeFileName(string fileName)
        {
            var trimmed = string.IsNullOrWhiteSpace(fileName) ? "document" : fileName.Trim();
            return trimmed.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
                ? trimmed[..^4]
                : trimmed;
        }

        private async Task<byte[]?> ResolveImageAsync(string? base64, string? url, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(base64))
            {
                return DecodeBase64(base64);
            }

            if (!string.IsNullOrWhiteSpace(url) && Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                var cacheKey = uri.AbsoluteUri;
                if (_resolvedImageCache.TryGetValue(cacheKey, out var cached))
                {
                    return cached;
                }

                try
                {
                    if (TryBuildQuickChartPostRequest(uri, out var postUri, out var payload))
                    {
                        var bytes = await DownloadWithRetryAsync(async () =>
                        {
                            var httpClient = _httpClientFactory.CreateClient("pdf-assets");
                            using var response = await httpClient.PostAsJsonAsync(postUri, payload, cancellationToken);
                            response.EnsureSuccessStatusCode();
                            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
                        }, cancellationToken);

                        if (bytes is not null && bytes.Length > 0)
                        {
                            _resolvedImageCache[cacheKey] = bytes;
                        }

                        return bytes;
                    }

                    var fallbackBytes = await DownloadWithRetryAsync(async () =>
                    {
                        var httpClientFallback = _httpClientFactory.CreateClient("pdf-assets");
                        return await httpClientFallback.GetByteArrayAsync(uri, cancellationToken);
                    }, cancellationToken);

                    if (fallbackBytes is not null && fallbackBytes.Length > 0)
                    {
                        _resolvedImageCache[cacheKey] = fallbackBytes;
                    }

                    return fallbackBytes;
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        private static async Task<byte[]?> DownloadWithRetryAsync(Func<Task<byte[]>> download, CancellationToken cancellationToken)
        {
            const int maxAttempts = 3;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var bytes = await download();
                    if (bytes.Length > 0)
                    {
                        return bytes;
                    }
                }
                catch when (attempt < maxAttempts)
                {
                    // Retry transient download failures.
                }
            }

            return null;
        }

        private static bool TryBuildQuickChartPostRequest(Uri uri, out Uri postUri, out object payload)
        {
            postUri = uri;
            payload = new { };

            if (!string.Equals(uri.Host, "quickchart.io", StringComparison.OrdinalIgnoreCase) ||
                !uri.AbsolutePath.StartsWith("/chart", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var query = ParseQueryString(uri.Query);
            if (!query.TryGetValue("c", out var chartConfig) || string.IsNullOrWhiteSpace(chartConfig))
            {
                return false;
            }

            var width = ReadIntQueryValue(query, "width", 500);
            var height = ReadIntQueryValue(query, "height", 300);
            var devicePixelRatio = ReadDoubleQueryValue(query, "devicePixelRatio", 2d);

            query.TryGetValue("backgroundColor", out var backgroundColor);
            query.TryGetValue("format", out var format);
            query.TryGetValue("version", out var version);

            postUri = new Uri($"{uri.Scheme}://{uri.Host}/chart");
            payload = new
            {
                width,
                height,
                devicePixelRatio,
                backgroundColor,
                format = string.IsNullOrWhiteSpace(format) ? "png" : format,
                version,
                chart = chartConfig
            };

            return true;
        }

        private static Dictionary<string, string> ParseQueryString(string query)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(query))
            {
                return result;
            }

            var trimmed = query.StartsWith("?") ? query[1..] : query;
            foreach (var pair in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = pair.Split('=', 2);
                var key = Uri.UnescapeDataString(parts[0]);
                var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
                result[key] = value;
            }

            return result;
        }

        private static int ReadIntQueryValue(IReadOnlyDictionary<string, string> query, string key, int defaultValue)
        {
            return query.TryGetValue(key, out var value) && int.TryParse(value, out var parsed)
                ? parsed
                : defaultValue;
        }

        private static double ReadDoubleQueryValue(IReadOnlyDictionary<string, string> query, string key, double defaultValue)
        {
            return query.TryGetValue(key, out var value) && double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : defaultValue;
        }

        private async Task<List<PdfResolvedChartDto>> ResolveChartsAsync(IEnumerable<PdfChartDto> charts, CancellationToken cancellationToken)
        {
            var resolved = new List<PdfResolvedChartDto>();

            foreach (var chart in charts)
            {
                var image = await ResolveImageAsync(chart.Base64, chart.Url, cancellationToken);
                if (image is null || image.Length == 0)
                {
                    continue;
                }

                resolved.Add(new PdfResolvedChartDto
                {
                    Title = chart.Title,
                    Content = image
                });
            }

            return resolved;
        }

        private static byte[]? GenerateQrCode(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(content.Trim(), QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(data);
            return qrCode.GetGraphic(10);
        }

        private static byte[] DecodeBase64(string base64OrDataUri)
        {
            var payload = base64OrDataUri.Trim();
            var commaIndex = payload.IndexOf(',');

            if (payload.Contains("base64", StringComparison.OrdinalIgnoreCase) && commaIndex >= 0)
            {
                payload = payload[(commaIndex + 1)..];
            }

            return Convert.FromBase64String(payload);
        }
    }
}
