using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddHelloAssoDonationLot1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaxReceipts_BeneficiaryOrganizationId",
                table: "TaxReceipts");

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "Donations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentConfirmedAt",
                table: "Donations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "Donations",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Donations",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "BeneficiaryOrganizations",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "BeneficiaryOrganizations",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FiscalArticle",
                table: "BeneficiaryOrganizations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HelloAssoOrganizationSlug",
                table: "BeneficiaryOrganizations",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDonationEnabled",
                table: "BeneficiaryOrganizations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsEligibleForTaxReceipt",
                table: "BeneficiaryOrganizations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LegalName",
                table: "BeneficiaryOrganizations",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RnaNumber",
                table: "BeneficiaryOrganizations",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Siret",
                table: "BeneficiaryOrganizations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.Sql(@"
DECLARE @DefaultOrganizationId INT;
SELECT TOP(1) @DefaultOrganizationId = [Id]
FROM [BeneficiaryOrganizations]
ORDER BY CASE WHEN [IsActive] = 1 THEN 0 ELSE 1 END, [Id];

IF @DefaultOrganizationId IS NULL
BEGIN
    THROW 50001, 'Migration AddHelloAssoDonationLot1: aucune ligne dans BeneficiaryOrganizations pour initialiser Donations.OrganizationId.', 1;
END;

UPDATE [Donations]
SET [OrganizationId] = @DefaultOrganizationId
WHERE [OrganizationId] = 0;

UPDATE [Donations]
SET [PublicId] = REPLACE(CONVERT(nvarchar(36), NEWID()), '-', '')
WHERE [PublicId] IS NULL OR LTRIM(RTRIM([PublicId])) = '';

;WITH Duplicates AS
(
    SELECT [Id], [Reference], ROW_NUMBER() OVER (PARTITION BY [Reference] ORDER BY [Id]) AS [rn]
    FROM [Donations]
    WHERE [Reference] IS NOT NULL
)
UPDATE d
SET [Reference] = NULL
FROM Duplicates d
WHERE d.[rn] > 1;
");

            migrationBuilder.CreateTable(
                name: "PaymentAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DonationId = table.Column<int>(type: "int", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ProviderCheckoutIntentId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ProviderOrderId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ProviderPaymentId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ProviderPaymentState = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    PaymentStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RedirectUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AuthorizedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailureCode = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    FailureMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LastReconciledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentAttempts_Donations_DonationId",
                        column: x => x.DonationId,
                        principalTable: "Donations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentWebhookInbox",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Provider = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PayloadHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExternalObjectId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    RawPayload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessingStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentWebhookInbox", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_TaxReceipts_Organization_ReceiptNumber",
                table: "TaxReceipts",
                columns: new[] { "BeneficiaryOrganizationId", "ReceiptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Donations_OrganizationId",
                table: "Donations",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "UX_Donations_PublicId",
                table: "Donations",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Donations_Reference",
                table: "Donations",
                column: "Reference",
                unique: true,
                filter: "[Reference] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BeneficiaryOrganizations_HelloAssoSlug",
                table: "BeneficiaryOrganizations",
                column: "HelloAssoOrganizationSlug");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttempts_DonationId",
                table: "PaymentAttempts",
                column: "DonationId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttempts_ProviderPaymentId",
                table: "PaymentAttempts",
                column: "ProviderPaymentId");

            migrationBuilder.CreateIndex(
                name: "UX_PaymentAttempts_CheckoutIntentId",
                table: "PaymentAttempts",
                column: "ProviderCheckoutIntentId",
                unique: true,
                filter: "[ProviderCheckoutIntentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentWebhookInbox_Status_ReceivedAt",
                table: "PaymentWebhookInbox",
                columns: new[] { "ProcessingStatus", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_PaymentWebhookInbox_Provider_PayloadHash",
                table: "PaymentWebhookInbox",
                columns: new[] { "Provider", "PayloadHash" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Donations_BeneficiaryOrganizations_OrganizationId",
                table: "Donations",
                column: "OrganizationId",
                principalTable: "BeneficiaryOrganizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Donations_BeneficiaryOrganizations_OrganizationId",
                table: "Donations");

            migrationBuilder.DropTable(
                name: "PaymentAttempts");

            migrationBuilder.DropTable(
                name: "PaymentWebhookInbox");

            migrationBuilder.DropIndex(
                name: "UX_TaxReceipts_Organization_ReceiptNumber",
                table: "TaxReceipts");

            migrationBuilder.DropIndex(
                name: "IX_Donations_OrganizationId",
                table: "Donations");

            migrationBuilder.DropIndex(
                name: "UX_Donations_PublicId",
                table: "Donations");

            migrationBuilder.DropIndex(
                name: "UX_Donations_Reference",
                table: "Donations");

            migrationBuilder.DropIndex(
                name: "IX_BeneficiaryOrganizations_HelloAssoSlug",
                table: "BeneficiaryOrganizations");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "PaymentConfirmedAt",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "BeneficiaryOrganizations");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "BeneficiaryOrganizations");

            migrationBuilder.DropColumn(
                name: "FiscalArticle",
                table: "BeneficiaryOrganizations");

            migrationBuilder.DropColumn(
                name: "HelloAssoOrganizationSlug",
                table: "BeneficiaryOrganizations");

            migrationBuilder.DropColumn(
                name: "IsDonationEnabled",
                table: "BeneficiaryOrganizations");

            migrationBuilder.DropColumn(
                name: "IsEligibleForTaxReceipt",
                table: "BeneficiaryOrganizations");

            migrationBuilder.DropColumn(
                name: "LegalName",
                table: "BeneficiaryOrganizations");

            migrationBuilder.DropColumn(
                name: "RnaNumber",
                table: "BeneficiaryOrganizations");

            migrationBuilder.DropColumn(
                name: "Siret",
                table: "BeneficiaryOrganizations");

            migrationBuilder.CreateIndex(
                name: "IX_TaxReceipts_BeneficiaryOrganizationId",
                table: "TaxReceipts",
                column: "BeneficiaryOrganizationId");
        }
    }
}
