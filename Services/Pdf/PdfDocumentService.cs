using api.Dtos.Pdf;
using api.Interfaces;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using QRCoder;

namespace api.Services.Pdf
{
    public sealed class PdfDocumentService : IPdfDocumentService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Dictionary<PdfDocumentType, IPdfTemplate> _templateByDocumentType;

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
                try
                {
                    var client = _httpClientFactory.CreateClient("pdf-assets");
                    return await client.GetByteArrayAsync(uri, cancellationToken);
                }
                catch
                {
                    return null;
                }
            }

            return null;
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
