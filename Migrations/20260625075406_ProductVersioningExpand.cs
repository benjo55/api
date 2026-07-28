using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class ProductVersioningExpand : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM dbo.Products WHERE LEN(ProductCode) > 50)
    THROW 51001, 'Migration ProductVersioningExpand impossible: au moins un ProductCode dépasse 50 caractères.', 1;
IF EXISTS (SELECT 1 FROM dbo.Products WHERE LEN(ProductName) > 200)
    THROW 51002, 'Migration ProductVersioningExpand impossible: au moins un ProductName dépasse 200 caractères.', 1;
IF EXISTS (
    SELECT 1
    FROM dbo.Products
    WHERE InsurerId IS NOT NULL
    GROUP BY InsurerId, ProductCode
    HAVING COUNT(*) > 1
)
    THROW 51003, 'Migration ProductVersioningExpand impossible: doublon (InsurerId, ProductCode).', 1;
");

            migrationBuilder.DropIndex(
                name: "IX_Products_InsurerId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_FeePolicies_Resolution",
                table: "FeePolicies");

            migrationBuilder.AddColumn<int>(
                name: "ProductVersionId",
                table: "ProductTaxOverrides",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProductName",
                table: "Products",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ProductCode",
                table: "Products",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "CommercialName",
                table: "Products",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Products",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOpenToNewBusiness",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOpenToNewPayments",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MarketingEndDate",
                table: "Products",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MarketingStartDate",
                table: "Products",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductEnvelopeId",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 4);

            migrationBuilder.AddColumn<int>(
                name: "ProductVersionId",
                table: "ProductOperationFeePolicies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductVersionId",
                table: "ProductManagementFeePolicies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductVersionId",
                table: "ProductFeatures",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductVersionId",
                table: "FeePolicies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductVersionId",
                table: "Contracts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LegalNatures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalNatures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    VersionCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VersionName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TaxProfileId = table.Column<int>(type: "int", nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    MinimumInitialPayment = table.Column<decimal>(type: "decimal(20,7)", precision: 20, scale: 7, nullable: true),
                    MinimumAdditionalPayment = table.Column<decimal>(type: "decimal(20,7)", precision: 20, scale: 7, nullable: true),
                    MinimumScheduledPayment = table.Column<decimal>(type: "decimal(20,7)", precision: 20, scale: 7, nullable: true),
                    MinimumPartialWithdrawal = table.Column<decimal>(type: "decimal(20,7)", precision: 20, scale: 7, nullable: true),
                    MinimumRemainingBalance = table.Column<decimal>(type: "decimal(20,7)", precision: 20, scale: 7, nullable: true),
                    MinimumSubscriptionAge = table.Column<int>(type: "int", nullable: true),
                    MaximumSubscriptionAge = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVersions_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductVersions_TaxProfiles_TaxProfileId",
                        column: x => x.TaxProfileId,
                        principalTable: "TaxProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductEnvelopes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ProductCategoryId = table.Column<int>(type: "int", nullable: false),
                    LegalNatureId = table.Column<int>(type: "int", nullable: false),
                    DefaultTaxProfileId = table.Column<int>(type: "int", nullable: true),
                    IsIndividual = table.Column<bool>(type: "bit", nullable: false),
                    IsCollective = table.Column<bool>(type: "bit", nullable: false),
                    AllowsMultipleHolders = table.Column<bool>(type: "bit", nullable: false),
                    RequiresInsuredPerson = table.Column<bool>(type: "bit", nullable: false),
                    SupportsBeneficiaryClause = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductEnvelopes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductEnvelopes_LegalNatures_LegalNatureId",
                        column: x => x.LegalNatureId,
                        principalTable: "LegalNatures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductEnvelopes_ProductCategories_ProductCategoryId",
                        column: x => x.ProductCategoryId,
                        principalTable: "ProductCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductEnvelopes_TaxProfiles_DefaultTaxProfileId",
                        column: x => x.DefaultTaxProfileId,
                        principalTable: "TaxProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductVersionId = table.Column<int>(type: "int", nullable: false),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    DocumentName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    StorageReference = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PublicationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductDocuments_ProductVersions_ProductVersionId",
                        column: x => x.ProductVersionId,
                        principalTable: "ProductVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductEligibilityRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductVersionId = table.Column<int>(type: "int", nullable: false),
                    RuleType = table.Column<int>(type: "int", nullable: false),
                    StringValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NumericValue = table.Column<decimal>(type: "decimal(20,7)", precision: 20, scale: 7, nullable: true),
                    BooleanValue = table.Column<bool>(type: "bit", nullable: true),
                    IsBlocking = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductEligibilityRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductEligibilityRules_ProductVersions_ProductVersionId",
                        column: x => x.ProductVersionId,
                        principalTable: "ProductVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductFeeRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductVersionId = table.Column<int>(type: "int", nullable: false),
                    FeeType = table.Column<int>(type: "int", nullable: false),
                    CalculationMethod = table.Column<int>(type: "int", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: true),
                    FixedAmount = table.Column<decimal>(type: "decimal(20,7)", precision: 20, scale: 7, nullable: true),
                    MinimumAmount = table.Column<decimal>(type: "decimal(20,7)", precision: 20, scale: 7, nullable: true),
                    MaximumAmount = table.Column<decimal>(type: "decimal(20,7)", precision: 20, scale: 7, nullable: true),
                    FreeOperationCount = table.Column<int>(type: "int", nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductFeeRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductFeeRules_ProductVersions_ProductVersionId",
                        column: x => x.ProductVersionId,
                        principalTable: "ProductVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductFinancialSupports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductVersionId = table.Column<int>(type: "int", nullable: false),
                    FinancialSupportId = table.Column<int>(type: "int", nullable: false),
                    IsAvailableForSubscription = table.Column<bool>(type: "bit", nullable: false),
                    IsAvailableForArbitration = table.Column<bool>(type: "bit", nullable: false),
                    IsDefaultSupport = table.Column<bool>(type: "bit", nullable: false),
                    MinimumAllocationPercentage = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    MaximumAllocationPercentage = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    AvailableFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AvailableTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductFinancialSupports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductFinancialSupports_FinancialSupports_FinancialSupportId",
                        column: x => x.FinancialSupportId,
                        principalTable: "FinancialSupports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductFinancialSupports_ProductVersions_ProductVersionId",
                        column: x => x.ProductVersionId,
                        principalTable: "ProductVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductGuarantees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductVersionId = table.Column<int>(type: "int", nullable: false),
                    GuaranteeType = table.Column<int>(type: "int", nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    IsOptional = table.Column<bool>(type: "bit", nullable: false),
                    MinimumCoverageAmount = table.Column<decimal>(type: "decimal(20,7)", precision: 20, scale: 7, nullable: true),
                    MaximumCoverageAmount = table.Column<decimal>(type: "decimal(20,7)", precision: 20, scale: 7, nullable: true),
                    MinimumRate = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: true),
                    MaximumRate = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: true),
                    CalculationRule = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EligibilityConditions = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductGuarantees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductGuarantees_ProductVersions_ProductVersionId",
                        column: x => x.ProductVersionId,
                        principalTable: "ProductVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductManagementModes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductVersionId = table.Column<int>(type: "int", nullable: false),
                    ManagementModeType = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AvailableFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AvailableTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductManagementModes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductManagementModes_ProductVersions_ProductVersionId",
                        column: x => x.ProductVersionId,
                        principalTable: "ProductVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductOperationRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductVersionId = table.Column<int>(type: "int", nullable: false),
                    OperationType = table.Column<int>(type: "int", nullable: false),
                    IsAllowed = table.Column<bool>(type: "bit", nullable: false),
                    MinimumAmount = table.Column<decimal>(type: "decimal(20,7)", precision: 20, scale: 7, nullable: true),
                    MaximumAmount = table.Column<decimal>(type: "decimal(20,7)", precision: 20, scale: 7, nullable: true),
                    MinimumRemainingAmount = table.Column<decimal>(type: "decimal(20,7)", precision: 20, scale: 7, nullable: true),
                    MaximumPercentage = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    MinimumHoldingPeriodInMonths = table.Column<int>(type: "int", nullable: true),
                    ProcessingDelayInBusinessDays = table.Column<int>(type: "int", nullable: true),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false),
                    RequiresSupportingDocument = table.Column<bool>(type: "bit", nullable: false),
                    Conditions = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductOperationRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductOperationRules_ProductVersions_ProductVersionId",
                        column: x => x.ProductVersionId,
                        principalTable: "ProductVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductPaymentRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductVersionId = table.Column<int>(type: "int", nullable: false),
                    PaymentType = table.Column<int>(type: "int", nullable: false),
                    Frequency = table.Column<int>(type: "int", nullable: true),
                    MinimumAmount = table.Column<decimal>(type: "decimal(20,7)", precision: 20, scale: 7, nullable: true),
                    MaximumAmount = table.Column<decimal>(type: "decimal(20,7)", precision: 20, scale: 7, nullable: true),
                    IsAllowed = table.Column<bool>(type: "bit", nullable: false),
                    RequiresManualApproval = table.Column<bool>(type: "bit", nullable: false),
                    ProcessingDelayInBusinessDays = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductPaymentRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductPaymentRules_ProductVersions_ProductVersionId",
                        column: x => x.ProductVersionId,
                        principalTable: "ProductVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "LegalNatures",
                columns: new[] { "Id", "Code", "CreatedDate", "Description", "IsActive", "Name", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, "INSURANCE_CONTRACT", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Contrat d'assurance", null },
                    { 2, "CAPITALIZATION_CONTRACT", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Contrat de capitalisation", null },
                    { 3, "RETIREMENT_PLAN", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Plan d'épargne retraite", null },
                    { 4, "COLLECTIVE_INSURANCE_CONTRACT", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Contrat collectif d'assurance", null },
                    { 5, "INVESTMENT_ACCOUNT", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Compte d'investissement", null }
                });

            migrationBuilder.InsertData(
                table: "ProductCategories",
                columns: new[] { "Id", "Code", "CreatedDate", "Description", "IsActive", "Name", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, "SAVINGS", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Épargne", null },
                    { 2, "RETIREMENT", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Retraite", null },
                    { 3, "CAPITALIZATION", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Capitalisation", null },
                    { 4, "PROTECTION", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Prévoyance / protection", null },
                    { 5, "INVESTMENT", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Investissement", null }
                });

            migrationBuilder.InsertData(
                table: "ProductEnvelopes",
                columns: new[] { "Id", "AllowsMultipleHolders", "Code", "CreatedDate", "DefaultTaxProfileId", "Description", "IsActive", "IsCollective", "IsIndividual", "LegalNatureId", "Name", "ProductCategoryId", "RequiresInsuredPerson", "SupportsBeneficiaryClause", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, false, "AV", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, true, false, true, 1, "Assurance-vie", 1, true, true, null },
                    { 2, false, "CAPI", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, null, true, false, true, 2, "Capitalisation", 3, false, false, null },
                    { 3, false, "PERIN", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, null, true, false, true, 3, "PER individuel", 2, false, true, null },
                    { 4, false, "PERCOL", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4, null, true, true, true, 3, "PER collectif", 2, false, true, null },
                    { 5, false, "PERO", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 5, null, true, true, true, 3, "PER obligatoire", 2, false, true, null },
                    { 6, false, "MADELIN", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, null, true, false, true, 1, "Contrat Madelin", 2, false, true, null },
                    { 7, false, "ART83", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 7, null, true, true, true, 4, "Article 83", 2, false, true, null },
                    { 8, false, "PEA", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 8, null, true, false, true, 5, "PEA", 5, false, false, null },
                    { 9, false, "PREV", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 9, null, true, true, true, 4, "Prévoyance collective", 4, true, true, null },
                    { 10, false, "DEP", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 10, null, true, false, true, 1, "Dépendance", 4, true, true, null },
                    { 11, false, "HCL", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 11, null, true, false, false, 1, "Homme-clé", 4, true, true, null },
                    { 12, false, "ART39", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 12, null, true, true, true, 4, "Article 39", 2, false, true, null }
                });

            migrationBuilder.Sql(@"
UPDATE p
SET ProductEnvelopeId = COALESCE(
    p.ProductEnvelopeId,
    CASE
        WHEN p.ProductTypeId BETWEEN 1 AND 11 THEN p.ProductTypeId
        WHEN p.ContractFamily = 0 THEN 1
        WHEN p.ContractFamily = 1 THEN 2
        WHEN p.ContractFamily = 2 THEN 3
        WHEN p.ContractFamily = 3 THEN 4
        WHEN p.ContractFamily = 4 THEN 5
        WHEN p.ContractFamily = 5 THEN 6
        WHEN p.ContractFamily = 6 THEN 7
        WHEN p.ContractFamily = 7 THEN 8
        WHEN p.ContractFamily = 8 THEN 9
        WHEN p.ContractFamily = 9 THEN 10
        WHEN p.ContractFamily = 10 THEN 11
        WHEN p.ContractFamily = 11 THEN 12
        ELSE NULL
    END
)
FROM dbo.Products p
WHERE p.ProductEnvelopeId IS NULL;

INSERT INTO dbo.ProductVersions
(
    ProductId,
    VersionCode,
    VersionName,
    EffectiveFrom,
    EffectiveTo,
    Status,
    TaxProfileId,
    CurrencyCode,
    CreatedDate
)
SELECT
    p.Id,
    'V1',
    'Version initiale',
    CAST(COALESCE(p.MarketingStartDate, p.CreatedDate, SYSUTCDATETIME()) AS date),
    p.MarketingEndDate,
    3,
    COALESCE(p.TaxProfileId, pe.DefaultTaxProfileId, pt.DefaultTaxProfileId),
    'EUR',
    SYSUTCDATETIME()
FROM dbo.Products p
LEFT JOIN dbo.ProductEnvelopes pe ON pe.Id = p.ProductEnvelopeId
LEFT JOIN dbo.ProductTypes pt ON pt.Id = p.ProductTypeId
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.ProductVersions v WHERE v.ProductId = p.Id AND v.VersionCode = 'V1'
);

UPDATE c
SET ProductVersionId = v.Id
FROM dbo.Contracts c
JOIN dbo.ProductVersions v ON v.ProductId = c.ProductId AND v.VersionCode = 'V1'
WHERE c.ProductVersionId IS NULL AND c.ProductId IS NOT NULL;

UPDATE m
SET ProductVersionId = v.Id
FROM dbo.ProductManagementFeePolicies m
JOIN dbo.ProductVersions v ON v.ProductId = m.ProductId AND v.VersionCode = 'V1'
WHERE m.ProductVersionId IS NULL;

UPDATE o
SET ProductVersionId = v.Id
FROM dbo.ProductOperationFeePolicies o
JOIN dbo.ProductVersions v ON v.ProductId = o.ProductId AND v.VersionCode = 'V1'
WHERE o.ProductVersionId IS NULL;

UPDATE f
SET ProductVersionId = v.Id
FROM dbo.FeePolicies f
JOIN dbo.ProductVersions v ON v.ProductId = f.ProductId AND v.VersionCode = 'V1'
WHERE f.ProductVersionId IS NULL AND f.ProductId IS NOT NULL;

UPDATE pf
SET ProductVersionId = v.Id
FROM dbo.ProductFeatures pf
JOIN dbo.ProductVersions v ON v.ProductId = pf.ProductId AND v.VersionCode = 'V1'
WHERE pf.ProductVersionId IS NULL;

UPDATE tax
SET ProductVersionId = v.Id
FROM dbo.ProductTaxOverrides tax
JOIN dbo.ProductVersions v ON v.ProductId = tax.ProductId AND v.VersionCode = 'V1'
WHERE tax.ProductVersionId IS NULL;
");

            migrationBuilder.CreateIndex(
                name: "IX_ProductTaxOverrides_ProductVersionId",
                table: "ProductTaxOverrides",
                column: "ProductVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_InsurerId_ProductCode",
                table: "Products",
                columns: new[] { "InsurerId", "ProductCode" },
                unique: true,
                filter: "[InsurerId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductEnvelopeId",
                table: "Products",
                column: "ProductEnvelopeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOperationFeePolicies_ProductVersionId",
                table: "ProductOperationFeePolicies",
                column: "ProductVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductManagementFeePolicies_ProductVersionId",
                table: "ProductManagementFeePolicies",
                column: "ProductVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductFeatures_ProductVersionId",
                table: "ProductFeatures",
                column: "ProductVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_FeePolicies_ProductVersionId",
                table: "FeePolicies",
                column: "ProductVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_FeePolicies_Resolution",
                table: "FeePolicies",
                columns: new[] { "Category", "FeeType", "Scope", "ProductId", "ProductVersionId", "ContractId", "CompartmentId", "FinancialSupportId", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_ProductVersionId",
                table: "Contracts",
                column: "ProductVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_LegalNatures_Code",
                table: "LegalNatures",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_Code",
                table: "ProductCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductDocuments_ProductVersionId_DocumentType_IsCurrent",
                table: "ProductDocuments",
                columns: new[] { "ProductVersionId", "DocumentType", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductEligibilityRules_ProductVersionId_RuleType",
                table: "ProductEligibilityRules",
                columns: new[] { "ProductVersionId", "RuleType" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductEnvelopes_Code",
                table: "ProductEnvelopes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductEnvelopes_DefaultTaxProfileId",
                table: "ProductEnvelopes",
                column: "DefaultTaxProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductEnvelopes_LegalNatureId",
                table: "ProductEnvelopes",
                column: "LegalNatureId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductEnvelopes_ProductCategoryId",
                table: "ProductEnvelopes",
                column: "ProductCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductFeeRules_ProductVersionId_FeeType_EffectiveFrom_EffectiveTo",
                table: "ProductFeeRules",
                columns: new[] { "ProductVersionId", "FeeType", "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductFinancialSupports_FinancialSupportId",
                table: "ProductFinancialSupports",
                column: "FinancialSupportId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductFinancialSupports_ProductVersionId_FinancialSupportId",
                table: "ProductFinancialSupports",
                columns: new[] { "ProductVersionId", "FinancialSupportId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductGuarantees_ProductVersionId_GuaranteeType",
                table: "ProductGuarantees",
                columns: new[] { "ProductVersionId", "GuaranteeType" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductManagementModes_ProductVersionId_ManagementModeType",
                table: "ProductManagementModes",
                columns: new[] { "ProductVersionId", "ManagementModeType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductOperationRules_ProductVersionId_OperationType",
                table: "ProductOperationRules",
                columns: new[] { "ProductVersionId", "OperationType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductPaymentRules_ProductVersionId_PaymentType_Frequency",
                table: "ProductPaymentRules",
                columns: new[] { "ProductVersionId", "PaymentType", "Frequency" },
                unique: true,
                filter: "[Frequency] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVersions_ProductId_Status_EffectiveFrom_EffectiveTo",
                table: "ProductVersions",
                columns: new[] { "ProductId", "Status", "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVersions_ProductId_VersionCode",
                table: "ProductVersions",
                columns: new[] { "ProductId", "VersionCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVersions_TaxProfileId",
                table: "ProductVersions",
                column: "TaxProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_ProductVersions_ProductVersionId",
                table: "Contracts",
                column: "ProductVersionId",
                principalTable: "ProductVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FeePolicies_ProductVersions_ProductVersionId",
                table: "FeePolicies",
                column: "ProductVersionId",
                principalTable: "ProductVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductFeatures_ProductVersions_ProductVersionId",
                table: "ProductFeatures",
                column: "ProductVersionId",
                principalTable: "ProductVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductManagementFeePolicies_ProductVersions_ProductVersionId",
                table: "ProductManagementFeePolicies",
                column: "ProductVersionId",
                principalTable: "ProductVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductOperationFeePolicies_ProductVersions_ProductVersionId",
                table: "ProductOperationFeePolicies",
                column: "ProductVersionId",
                principalTable: "ProductVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_ProductEnvelopes_ProductEnvelopeId",
                table: "Products",
                column: "ProductEnvelopeId",
                principalTable: "ProductEnvelopes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductTaxOverrides_ProductVersions_ProductVersionId",
                table: "ProductTaxOverrides",
                column: "ProductVersionId",
                principalTable: "ProductVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_ProductVersions_ProductVersionId",
                table: "Contracts");

            migrationBuilder.DropForeignKey(
                name: "FK_FeePolicies_ProductVersions_ProductVersionId",
                table: "FeePolicies");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductFeatures_ProductVersions_ProductVersionId",
                table: "ProductFeatures");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductManagementFeePolicies_ProductVersions_ProductVersionId",
                table: "ProductManagementFeePolicies");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductOperationFeePolicies_ProductVersions_ProductVersionId",
                table: "ProductOperationFeePolicies");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_ProductEnvelopes_ProductEnvelopeId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductTaxOverrides_ProductVersions_ProductVersionId",
                table: "ProductTaxOverrides");

            migrationBuilder.DropTable(
                name: "ProductDocuments");

            migrationBuilder.DropTable(
                name: "ProductEligibilityRules");

            migrationBuilder.DropTable(
                name: "ProductEnvelopes");

            migrationBuilder.DropTable(
                name: "ProductFeeRules");

            migrationBuilder.DropTable(
                name: "ProductFinancialSupports");

            migrationBuilder.DropTable(
                name: "ProductGuarantees");

            migrationBuilder.DropTable(
                name: "ProductManagementModes");

            migrationBuilder.DropTable(
                name: "ProductOperationRules");

            migrationBuilder.DropTable(
                name: "ProductPaymentRules");

            migrationBuilder.DropTable(
                name: "LegalNatures");

            migrationBuilder.DropTable(
                name: "ProductCategories");

            migrationBuilder.DropTable(
                name: "ProductVersions");

            migrationBuilder.DropIndex(
                name: "IX_ProductTaxOverrides_ProductVersionId",
                table: "ProductTaxOverrides");

            migrationBuilder.DropIndex(
                name: "IX_Products_InsurerId_ProductCode",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_ProductEnvelopeId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_ProductOperationFeePolicies_ProductVersionId",
                table: "ProductOperationFeePolicies");

            migrationBuilder.DropIndex(
                name: "IX_ProductManagementFeePolicies_ProductVersionId",
                table: "ProductManagementFeePolicies");

            migrationBuilder.DropIndex(
                name: "IX_ProductFeatures_ProductVersionId",
                table: "ProductFeatures");

            migrationBuilder.DropIndex(
                name: "IX_FeePolicies_ProductVersionId",
                table: "FeePolicies");

            migrationBuilder.DropIndex(
                name: "IX_FeePolicies_Resolution",
                table: "FeePolicies");

            migrationBuilder.DropIndex(
                name: "IX_Contracts_ProductVersionId",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "ProductVersionId",
                table: "ProductTaxOverrides");

            migrationBuilder.DropColumn(
                name: "CommercialName",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsOpenToNewBusiness",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsOpenToNewPayments",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "MarketingEndDate",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "MarketingStartDate",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ProductEnvelopeId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ProductVersionId",
                table: "ProductOperationFeePolicies");

            migrationBuilder.DropColumn(
                name: "ProductVersionId",
                table: "ProductManagementFeePolicies");

            migrationBuilder.DropColumn(
                name: "ProductVersionId",
                table: "ProductFeatures");

            migrationBuilder.DropColumn(
                name: "ProductVersionId",
                table: "FeePolicies");

            migrationBuilder.DropColumn(
                name: "ProductVersionId",
                table: "Contracts");

            migrationBuilder.AlterColumn<string>(
                name: "ProductName",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "ProductCode",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateIndex(
                name: "IX_Products_InsurerId",
                table: "Products",
                column: "InsurerId");

            migrationBuilder.CreateIndex(
                name: "IX_FeePolicies_Resolution",
                table: "FeePolicies",
                columns: new[] { "Category", "FeeType", "Scope", "ProductId", "ContractId", "CompartmentId", "FinancialSupportId", "Priority" });
        }
    }
}
