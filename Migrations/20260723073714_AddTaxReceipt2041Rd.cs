using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxReceipt2041Rd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "TaxReceiptNumberSequence");

            migrationBuilder.CreateTable(
                name: "BeneficiaryOrganizations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    IdentifierType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Identifier = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    StreetNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    StreetName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    AddressLine2 = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    City = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    OrganizationCategory = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    OrganizationSubCategory = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    OtherCategoryDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RecognitionDecreeDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RecognitionOfficialJournalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeneficiaryOrganizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Donors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DonorType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    AddressLine1 = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    AddressLine2 = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    StreetNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    StreetName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    City = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Donors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Donations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DonorId = table.Column<int>(type: "int", nullable: false),
                    DonationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    DonationForm = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OtherFormDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DonationNature = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OtherNatureDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TaxRegime = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Article200Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Article978Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Reference = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ExternalReference = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IsCancelled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Donations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Donations_Donors_DonorId",
                        column: x => x.DonorId,
                        principalTable: "Donors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaxReceipts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DonationId = table.Column<int>(type: "int", nullable: false),
                    BeneficiaryOrganizationId = table.Column<int>(type: "int", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CerfaCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CerfaVersion = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    GenerationRequestKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    GeneratedFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    DocumentArtifactId = table.Column<int>(type: "int", nullable: true),
                    PdfHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GeneratedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SentToEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    LastEmailStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReplacementReceiptId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxReceipts_BeneficiaryOrganizations_BeneficiaryOrganizationId",
                        column: x => x.BeneficiaryOrganizationId,
                        principalTable: "BeneficiaryOrganizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaxReceipts_DocumentArtifacts_DocumentArtifactId",
                        column: x => x.DocumentArtifactId,
                        principalTable: "DocumentArtifacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaxReceipts_Donations_DonationId",
                        column: x => x.DonationId,
                        principalTable: "Donations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaxReceipts_TaxReceipts_ReplacementReceiptId",
                        column: x => x.ReplacementReceiptId,
                        principalTable: "TaxReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaxReceiptEmailHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaxReceiptId = table.Column<int>(type: "int", nullable: false),
                    RecipientEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxReceiptEmailHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxReceiptEmailHistory_TaxReceipts_TaxReceiptId",
                        column: x => x.TaxReceiptId,
                        principalTable: "TaxReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BeneficiaryOrganizations_IdentifierType_Identifier",
                table: "BeneficiaryOrganizations",
                columns: new[] { "IdentifierType", "Identifier" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BeneficiaryOrganizations_IsActive",
                table: "BeneficiaryOrganizations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Donations_DonationDate",
                table: "Donations",
                column: "DonationDate");

            migrationBuilder.CreateIndex(
                name: "IX_Donations_Donor_Status",
                table: "Donations",
                columns: new[] { "DonorId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Donors_DuplicateLookup",
                table: "Donors",
                columns: new[] { "LastName", "FirstName", "PostalCode" });

            migrationBuilder.CreateIndex(
                name: "IX_Donors_Email",
                table: "Donors",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_TaxReceiptEmailHistory_Receipt_CreatedAt",
                table: "TaxReceiptEmailHistory",
                columns: new[] { "TaxReceiptId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxReceipts_BeneficiaryOrganizationId",
                table: "TaxReceipts",
                column: "BeneficiaryOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxReceipts_DocumentArtifactId",
                table: "TaxReceipts",
                column: "DocumentArtifactId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxReceipts_ReplacementReceiptId",
                table: "TaxReceipts",
                column: "ReplacementReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxReceipts_Status",
                table: "TaxReceipts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UX_TaxReceipts_ActiveDonation",
                table: "TaxReceipts",
                column: "DonationId",
                unique: true,
                filter: "[Status] IN ('Ready', 'Generated', 'Sent', 'EmailFailed')");

            migrationBuilder.CreateIndex(
                name: "UX_TaxReceipts_GenerationRequestKey",
                table: "TaxReceipts",
                column: "GenerationRequestKey",
                unique: true,
                filter: "[GenerationRequestKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_TaxReceipts_ReceiptNumber",
                table: "TaxReceipts",
                column: "ReceiptNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaxReceiptEmailHistory");

            migrationBuilder.DropTable(
                name: "TaxReceipts");

            migrationBuilder.DropTable(
                name: "BeneficiaryOrganizations");

            migrationBuilder.DropTable(
                name: "Donations");

            migrationBuilder.DropTable(
                name: "Donors");

            migrationBuilder.DropSequence(
                name: "TaxReceiptNumberSequence");
        }
    }
}
