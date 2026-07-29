using System.Globalization;
using System.Text.RegularExpressions;
using System.Text.Json;
using api.Exceptions;
using api.Interfaces;
using api.Models;
using api.Models.Enum;
using PdfSharp.Drawing;
using PdfSharp.Pdf.IO;

namespace api.Services.TaxReceipts
{
    public sealed class TaxReceiptPdfGenerator2041Rd : ITaxReceiptPdfGenerator
    {
        public string CerfaCode => "2041-RD";
        public string CerfaVersion => "11580*05";

        private readonly IWebHostEnvironment _environment;
        private readonly IAmountToWordsService _amountToWordsService;

        public TaxReceiptPdfGenerator2041Rd(IWebHostEnvironment environment, IAmountToWordsService amountToWordsService)
        {
            _environment = environment;
            _amountToWordsService = amountToWordsService;
        }

        public async Task<byte[]> GenerateAsync(TaxReceipt receipt, CancellationToken cancellationToken = default)
        {
            var mapping = await LoadMappingAsync(cancellationToken);
            var templatePath = Path.Combine(
                _environment.ContentRootPath,
                "Templates",
                "TaxReceipts",
                CerfaCode,
                CerfaVersion.Replace("*", "-"),
                mapping.TemplateFile);

            if (!File.Exists(templatePath))
            {
                throw new BusinessException("CerfaTemplateNotFound");
            }

            using var document = PdfReader.Open(templatePath, PdfDocumentOpenMode.Modify);
            var values = BuildValues(receipt);
            foreach (var field in mapping.TextFields)
            {
                if (!values.TryGetValue(field.Key, out var value) || string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                var entry = field.Value;
                var page = document.Pages[entry.Page - 1];
                using var graphics = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
                var text = entry.Uppercase ? value.ToUpperInvariant() : value;
                DrawWrappedText(graphics, text, entry);
            }

            foreach (var checkbox in CheckedBoxes(receipt))
            {
                if (!mapping.Checkboxes.TryGetValue(checkbox, out var entry))
                {
                    continue;
                }

                var page = document.Pages[entry.Page - 1];
                using var graphics = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
                DrawCheck(graphics, entry);
            }

            using var stream = new MemoryStream();
            document.Save(stream, closeStream: false);
            return stream.ToArray();
        }

        private async Task<CerfaTemplateMapping> LoadMappingAsync(CancellationToken cancellationToken)
        {
            var mappingPath = Path.Combine(
                _environment.ContentRootPath,
                "Templates",
                "TaxReceipts",
                CerfaCode,
                CerfaVersion.Replace("*", "-"),
                "mapping.json");

            if (!File.Exists(mappingPath))
            {
                throw new BusinessException("CerfaMappingNotFound");
            }

            await using var stream = File.OpenRead(mappingPath);
            return await JsonSerializer.DeserializeAsync<CerfaTemplateMapping>(
                stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken)
                ?? throw new BusinessException("CerfaMappingNotFound");
        }

        private Dictionary<string, string> BuildValues(TaxReceipt receipt)
        {
            var donation = receipt.Donation;
            var donorSnapshot = donation.DonorSnapshot;
            var donor = donation.Donor;
            var organization = receipt.BeneficiaryOrganization;
            var culture = CultureInfo.GetCultureInfo("fr-FR");
            var organizationStreet = ResolveStreetParts(organization.StreetNumber, organization.StreetName);
            var donorStreet = donorSnapshot is not null
                ? ResolveStreetParts(null, donorSnapshot.AddressLine1)
                : ResolveStreetParts(donor.StreetNumber, donor.StreetName);
            var donorFirstName = donorSnapshot?.FirstName ?? donor.FirstName;
            var donorLastName = donorSnapshot?.LastName ?? donor.LastName;
            var donorPostalCode = donorSnapshot?.PostalCode ?? donor.PostalCode;
            var donorCity = donorSnapshot?.City ?? donor.City;
            var donorCountry = donorSnapshot?.Country ?? donor.CountryCode;

            return new Dictionary<string, string>
            {
                ["receiptNumber"] = receipt.ReceiptNumber,
                ["organization.name"] = organization.Name,
                ["organization.identifier"] = organization.Identifier,
                ["organization.streetNumber"] = organizationStreet.Number,
                ["organization.streetName"] = organizationStreet.Name,
                ["organization.postalCode"] = organization.PostalCode,
                ["organization.city"] = organization.City,
                ["organization.country"] = organization.CountryCode,
                ["organization.purpose"] = organization.Purpose,
                ["organization.otherCategoryDescription"] = organization.OtherCategoryDescription ?? string.Empty,
                ["organization.recognitionDecreeDate"] = FormatDate(organization.RecognitionDecreeDate),
                ["organization.recognitionOfficialJournalDate"] = FormatDate(organization.RecognitionOfficialJournalDate),
                ["organization.approvalDate"] = FormatDate(organization.ApprovalDate),
                ["donor.lastName"] = donorLastName,
                ["donor.firstName"] = donorFirstName,
                ["donor.streetNumber"] = donorStreet.Number,
                ["donor.streetName"] = donorStreet.Name,
                ["donor.postalCode"] = donorPostalCode,
                ["donor.city"] = donorCity,
                ["donor.country"] = donorCountry,
                ["donation.amount"] = donation.Amount.ToString("N2", culture),
                ["donation.amountInWords"] = _amountToWordsService.ToFrenchEuros(donation.Amount),
                ["donation.date"] = donation.DonationDate.ToString("dd/MM/yyyy", culture),
                ["donation.date.day"] = donation.DonationDate.ToString("dd", culture),
                ["donation.date.month"] = donation.DonationDate.ToString("MM", culture),
                ["donation.date.year"] = donation.DonationDate.ToString("yyyy", culture),
                ["donation.purpose"] = donation.Purpose ?? donation.OtherFormDescription ?? string.Empty,
                ["donation.otherFormDescription"] = donation.OtherFormDescription ?? string.Empty,
                ["donation.otherNatureDescription"] = donation.OtherNatureDescription ?? string.Empty,
                ["receipt.generatedAt"] = (receipt.GeneratedAt ?? DateTime.UtcNow).ToString("dd/MM/yyyy", culture),
                ["receipt.generatedAt.day"] = (receipt.GeneratedAt ?? DateTime.UtcNow).ToString("dd", culture),
                ["receipt.generatedAt.month"] = (receipt.GeneratedAt ?? DateTime.UtcNow).ToString("MM", culture),
                ["receipt.generatedAt.year"] = (receipt.GeneratedAt ?? DateTime.UtcNow).ToString("yyyy", culture)
            };
        }

        private static string NormalizeStreetName(string streetName)
        {
            const string ruePrefix = "rue ";
            return streetName.StartsWith(ruePrefix, StringComparison.OrdinalIgnoreCase)
                ? streetName[ruePrefix.Length..].TrimStart()
                : streetName;
        }

        private static (string Number, string Name) ResolveStreetParts(string? streetNumber, string streetName)
        {
            if (!string.IsNullOrWhiteSpace(streetNumber))
            {
                return (streetNumber.Trim(), NormalizeStreetName(streetName.Trim()));
            }

            var trimmedStreetName = streetName.Trim();
            var match = Regex.Match(
                trimmedStreetName,
                @"^\s*(?<number>\d+\s*(?:bis|ter|quater)?)\s+(?<name>.+)$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            return match.Success
                ? (match.Groups["number"].Value.Trim(), NormalizeStreetName(match.Groups["name"].Value.Trim()))
                : (string.Empty, NormalizeStreetName(trimmedStreetName));
        }

        private static IReadOnlyList<string> CheckedBoxes(TaxReceipt receipt)
        {
            var donation = receipt.Donation;
            var organization = receipt.BeneficiaryOrganization;
            var result = new List<string>
            {
                $"organization.category.{organization.OrganizationCategory}",
                $"organization.subCategory.{organization.OrganizationSubCategory}",
                $"donation.form.{donation.DonationForm}",
                $"donation.nature.{donation.DonationNature}"
            };

            if (donation.TaxRegime is DonationTaxRegime.Article200 or DonationTaxRegime.Article200And978)
            {
                result.Add("donation.taxRegime.Article200");
            }

            if (donation.TaxRegime is DonationTaxRegime.Article978 or DonationTaxRegime.Article200And978)
            {
                result.Add("donation.taxRegime.Article978");
            }

            if (donation.PaymentMethod is not null)
            {
                result.Add($"donation.paymentMethod.{donation.PaymentMethod}");
            }

            return result;
        }

        private static void DrawWrappedText(XGraphics graphics, string text, CerfaTextFieldMapping field)
        {
            var font = new XFont("Arial", field.FontSize, XFontStyleEx.Bold);
            var brush = new XSolidBrush(DataColor);
            var backgroundBrush = XBrushes.White;
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var lines = new List<string>();
            var current = string.Empty;

            foreach (var word in words)
            {
                var candidate = string.IsNullOrEmpty(current) ? word : $"{current} {word}";
                if (graphics.MeasureString(candidate, font).Width <= field.Width || string.IsNullOrEmpty(current))
                {
                    current = candidate;
                }
                else
                {
                    lines.Add(current);
                    current = word;
                }
            }

            if (!string.IsNullOrWhiteSpace(current))
            {
                lines.Add(current);
            }

            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var size = graphics.MeasureString(line, font);
                var x = field.Align?.Equals("center", StringComparison.OrdinalIgnoreCase) == true
                    ? field.X + Math.Max(0, (field.Width - size.Width) / 2)
                    : field.Align?.Equals("right", StringComparison.OrdinalIgnoreCase) == true
                        ? field.X + Math.Max(0, field.Width - size.Width)
                        : field.X;
                var y = field.Y + i * (field.FontSize + 2);
                graphics.DrawRectangle(
                    backgroundBrush,
                    x - 0.5,
                    y - field.FontSize + 1,
                    Math.Min(size.Width + 1, field.Width + 1),
                    field.FontSize + 3);
                graphics.DrawString(line, font, brush, new XPoint(x, y));
            }
        }

        private static void DrawCheck(XGraphics graphics, CerfaCheckboxFieldMapping field)
        {
            var size = Math.Min(field.Size, 6.5);
            var offset = (field.Size - size) / 2;
            var x = field.X + offset;
            var y = field.Y + offset - 2;
            var pen = new XPen(DataColor, 1.4);
            graphics.DrawLine(pen, x, y, x + size, y + size);
            graphics.DrawLine(pen, x + size, y, x, y + size);
        }

        private static readonly XColor DataColor = XColor.FromArgb(196, 0, 0);

        private static string FormatDate(DateTime? date) => date?.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("fr-FR")) ?? string.Empty;
    }
}
