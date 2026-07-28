using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddMySpaceDonationPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminNote",
                table: "PaymentAttempts",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "PaymentAttempts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckoutUrlExpiresAt",
                table: "PaymentAttempts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedAt",
                table: "PaymentAttempts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConfirmedByUserId",
                table: "PaymentAttempts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DonorTransferDeclarationComment",
                table: "PaymentAttempts",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DonorTransferDeclaredAt",
                table: "PaymentAttempts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiredAt",
                table: "PaymentAttempts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "PaymentAttempts",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InternalReference",
                table: "PaymentAttempts",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "PaymentAttempts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundedAt",
                table: "PaymentAttempts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "PaymentAttempts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ConfirmedPaymentProvider",
                table: "Donations",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PostPaymentProcessedAt",
                table: "Donations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostPaymentProcessingError",
                table: "Donations",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HelloAssoConnectionError",
                table: "BeneficiaryOrganizations",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HelloAssoConnectionLastCheckedAt",
                table: "BeneficiaryOrganizations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HelloAssoConnectionStatus",
                table: "BeneficiaryOrganizations",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HelloAssoCredentialKey",
                table: "BeneficiaryOrganizations",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HelloAssoEnvironment",
                table: "BeneficiaryOrganizations",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBankTransferEnabled",
                table: "BeneficiaryOrganizations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsHelloAssoPaymentEnabled",
                table: "BeneficiaryOrganizations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPayPalEnabled",
                table: "BeneficiaryOrganizations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PayPalCredentialKey",
                table: "BeneficiaryOrganizations",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayPalEnvironment",
                table: "BeneficiaryOrganizations",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayPalMerchantAlias",
                table: "BeneficiaryOrganizations",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayPalMerchantId",
                table: "BeneficiaryOrganizations",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrganizationBankAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BeneficiaryOrganizationId = table.Column<int>(type: "int", nullable: false),
                    AccountHolder = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    EncryptedIban = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IbanLastFour = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    EncryptedBic = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BicLastFour = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationBankAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationBankAccounts_BeneficiaryOrganizations_BeneficiaryOrganizationId",
                        column: x => x.BeneficiaryOrganizationId,
                        principalTable: "BeneficiaryOrganizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(@"
UPDATE PaymentAttempts
SET InternalReference = CONCAT('PAY-MIG-', RIGHT(CONCAT('0000000000', Id), 10))
WHERE InternalReference = '';

UPDATE PaymentAttempts
SET UpdatedAt = CreatedAt
WHERE UpdatedAt = '0001-01-01T00:00:00.0000000';

UPDATE BeneficiaryOrganizations
SET IsHelloAssoPaymentEnabled = 1
WHERE IsDonationEnabled = 1
  AND HelloAssoOrganizationSlug IS NOT NULL
  AND LTRIM(RTRIM(HelloAssoOrganizationSlug)) <> '';
");

            migrationBuilder.CreateIndex(
                name: "UX_PaymentAttempts_IdempotencyKey",
                table: "PaymentAttempts",
                column: "IdempotencyKey",
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_PaymentAttempts_InternalReference",
                table: "PaymentAttempts",
                column: "InternalReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationBankAccounts_Organization_Active",
                table: "OrganizationBankAccounts",
                columns: new[] { "BeneficiaryOrganizationId", "IsActive", "ValidFrom" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrganizationBankAccounts");

            migrationBuilder.DropIndex(
                name: "UX_PaymentAttempts_IdempotencyKey",
                table: "PaymentAttempts");

            migrationBuilder.DropIndex(
                name: "UX_PaymentAttempts_InternalReference",
                table: "PaymentAttempts");

            migrationBuilder.DropColumn(
                name: "AdminNote",
                table: "PaymentAttempts");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "PaymentAttempts");

            migrationBuilder.DropColumn(
                name: "CheckoutUrlExpiresAt",
                table: "PaymentAttempts");

            migrationBuilder.DropColumn(
                name: "ConfirmedAt",
                table: "PaymentAttempts");

            migrationBuilder.DropColumn(
                name: "ConfirmedByUserId",
                table: "PaymentAttempts");

            migrationBuilder.DropColumn(
                name: "DonorTransferDeclarationComment",
                table: "PaymentAttempts");

            migrationBuilder.DropColumn(
                name: "DonorTransferDeclaredAt",
                table: "PaymentAttempts");

            migrationBuilder.DropColumn(
                name: "ExpiredAt",
                table: "PaymentAttempts");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "PaymentAttempts");

            migrationBuilder.DropColumn(
                name: "InternalReference",
                table: "PaymentAttempts");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "PaymentAttempts");

            migrationBuilder.DropColumn(
                name: "RefundedAt",
                table: "PaymentAttempts");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "PaymentAttempts");

            migrationBuilder.DropColumn(
                name: "ConfirmedPaymentProvider",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "PostPaymentProcessedAt",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "PostPaymentProcessingError",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "HelloAssoConnectionError",
                table: "BeneficiaryOrganizations");

            migrationBuilder.DropColumn(
                name: "HelloAssoConnectionLastCheckedAt",
                table: "BeneficiaryOrganizations");

            migrationBuilder.DropColumn(
                name: "HelloAssoConnectionStatus",
                table: "BeneficiaryOrganizations");

            migrationBuilder.DropColumn(
                name: "HelloAssoCredentialKey",
                table: "BeneficiaryOrganizations");

            migrationBuilder.DropColumn(
                name: "HelloAssoEnvironment",
                table: "BeneficiaryOrganizations");

            migrationBuilder.DropColumn(
                name: "IsBankTransferEnabled",
                table: "BeneficiaryOrganizations");

            migrationBuilder.DropColumn(
                name: "IsHelloAssoPaymentEnabled",
                table: "BeneficiaryOrganizations");

            migrationBuilder.DropColumn(
                name: "IsPayPalEnabled",
                table: "BeneficiaryOrganizations");

            migrationBuilder.DropColumn(
                name: "PayPalCredentialKey",
                table: "BeneficiaryOrganizations");

            migrationBuilder.DropColumn(
                name: "PayPalEnvironment",
                table: "BeneficiaryOrganizations");

            migrationBuilder.DropColumn(
                name: "PayPalMerchantAlias",
                table: "BeneficiaryOrganizations");

            migrationBuilder.DropColumn(
                name: "PayPalMerchantId",
                table: "BeneficiaryOrganizations");
        }
    }
}
