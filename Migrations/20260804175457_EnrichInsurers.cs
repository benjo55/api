using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class EnrichInsurers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Acronym",
                table: "Insurers",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActivityEndDate",
                table: "Insurers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApeNafCode",
                table: "Insurers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AssetsUnderManagement",
                table: "Insurers",
                type: "decimal(20,2)",
                precision: 20,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AuthorizationDate",
                table: "Insurers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ComplaintsProcedureUrl",
                table: "Insurers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CustomerCount",
                table: "Insurers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerSegmentsJson",
                table: "Insurers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataQualityNotes",
                table: "Insurers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataSourceType",
                table: "Insurers",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DistributionChannelsJson",
                table: "Insurers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EiopaRegisterId",
                table: "Insurers",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmployeeCount",
                table: "Insurers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExerciseRegime",
                table: "Insurers",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FinancialDataYear",
                table: "Insurers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FormerNamesJson",
                table: "Insurers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeographicCoverage",
                table: "Insurers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GroupName",
                table: "Insurers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GroupWebsiteUrl",
                table: "Insurers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeadquartersSiret",
                table: "Insurers",
                type: "nvarchar(14)",
                maxLength: 14,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "History",
                table: "Insurers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HomeCountryCode",
                table: "Insurers",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IncorporationCountryCode",
                table: "Insurers",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IncorporationDate",
                table: "Insurers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InsurerType",
                table: "Insurers",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InternalCode",
                table: "Insurers",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsGroupHead",
                table: "Insurers",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLifeInsurer",
                table: "Insurers",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsNonLifeInsurer",
                table: "Insurers",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsReinsurer",
                table: "Insurers",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSubjectToSolvencyII",
                table: "Insurers",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KeyFacts",
                table: "Insurers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastVerifiedAt",
                table: "Insurers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LegalForm",
                table: "Insurers",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LegalName",
                table: "Insurers",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Lei",
                table: "Insurers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LongDescription",
                table: "Insurers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MainActivitiesJson",
                table: "Insurers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Mission",
                table: "Insurers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfficialRegistryUrl",
                table: "Insurers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OwnershipPercentage",
                table: "Insurers",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParentLegalEntityName",
                table: "Insurers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParentLei",
                table: "Insurers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrivacyPolicyUrl",
                table: "Insurers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductSpecialtiesJson",
                table: "Insurers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Rating",
                table: "Insurers",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RatingAgency",
                table: "Insurers",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RatingDate",
                table: "Insurers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RatingOutlook",
                table: "Insurers",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RatingSourceUrl",
                table: "Insurers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RcsCity",
                table: "Insurers",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RcsNumber",
                table: "Insurers",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegulatoryNotes",
                table: "Insurers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegulatoryStatus",
                table: "Insurers",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RetrievedAt",
                table: "Insurers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShortDescription",
                table: "Insurers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Siren",
                table: "Insurers",
                type: "nvarchar(9)",
                maxLength: 9,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceName",
                table: "Insurers",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceReference",
                table: "Insurers",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "Insurers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupervisoryAuthorityCountryCode",
                table: "Insurers",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupervisoryAuthorityName",
                table: "Insurers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupervisoryRegisterId",
                table: "Insurers",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupervisoryRegisterName",
                table: "Insurers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SuspensionDate",
                table: "Insurers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TradeName",
                table: "Insurers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UltimateParentLei",
                table: "Insurers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UltimateParentName",
                table: "Insurers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidFrom",
                table: "Insurers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidTo",
                table: "Insurers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VatNumber",
                table: "Insurers",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerificationStatus",
                table: "Insurers",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerifiedBy",
                table: "Insurers",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WithdrawalDate",
                table: "Insurers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InsurerAuthorizations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InsurerId = table.Column<int>(type: "int", nullable: false),
                    AuthorityName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AuthorityCountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    RegisterName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RegisterReference = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    AuthorizationType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    InsuranceBranchCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    InsuranceBranchLabel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BusinessCategory = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    HostCountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    ExerciseRegime = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SourceUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastVerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsurerAuthorizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InsurerAuthorizations_Insurers_InsurerId",
                        column: x => x.InsurerId,
                        principalTable: "Insurers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InsurerContactPoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InsurerId = table.Column<int>(type: "int", nullable: false),
                    ContactType = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Label = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    DepartmentName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    AddressLine1 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    AddressLine2 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    City = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Region = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OpeningHours = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SourceUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastVerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsurerContactPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InsurerContactPoints_Insurers_InsurerId",
                        column: x => x.InsurerId,
                        principalTable: "Insurers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InsurerSolvencyMetrics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InsurerId = table.Column<int>(type: "int", nullable: false),
                    ReportingYear = table.Column<int>(type: "int", nullable: false),
                    ReportingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SfcrPublicationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SfcrDocumentUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EligibleOwnFunds = table.Column<decimal>(type: "decimal(20,2)", precision: 20, scale: 2, nullable: true),
                    SolvencyCapitalRequirement = table.Column<decimal>(type: "decimal(20,2)", precision: 20, scale: 2, nullable: true),
                    ScrCoverageRatio = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    MinimumCapitalRequirement = table.Column<decimal>(type: "decimal(20,2)", precision: 20, scale: 2, nullable: true),
                    McrCoverageRatio = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    IsGroupReport = table.Column<bool>(type: "bit", nullable: false),
                    SourceUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastVerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsurerSolvencyMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InsurerSolvencyMetrics_Insurers_InsurerId",
                        column: x => x.InsurerId,
                        principalTable: "Insurers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Insurers_InternalCode",
                table: "Insurers",
                column: "InternalCode");

            migrationBuilder.CreateIndex(
                name: "IX_Insurers_RegulatoryStatus",
                table: "Insurers",
                column: "RegulatoryStatus");

            migrationBuilder.CreateIndex(
                name: "UX_Insurers_Lei",
                table: "Insurers",
                column: "Lei",
                unique: true,
                filter: "[Lei] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_Insurers_Siren",
                table: "Insurers",
                column: "Siren",
                unique: true,
                filter: "[Siren] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InsurerAuthorizations_Insurer_Country_Branch",
                table: "InsurerAuthorizations",
                columns: new[] { "InsurerId", "HostCountryCode", "InsuranceBranchCode" });

            migrationBuilder.CreateIndex(
                name: "IX_InsurerContactPoints_Insurer_Type_Primary",
                table: "InsurerContactPoints",
                columns: new[] { "InsurerId", "ContactType", "IsPrimary" });

            migrationBuilder.CreateIndex(
                name: "IX_InsurerSolvencyMetrics_Insurer_Year_Group",
                table: "InsurerSolvencyMetrics",
                columns: new[] { "InsurerId", "ReportingYear", "IsGroupReport" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InsurerAuthorizations");

            migrationBuilder.DropTable(
                name: "InsurerContactPoints");

            migrationBuilder.DropTable(
                name: "InsurerSolvencyMetrics");

            migrationBuilder.DropIndex(
                name: "IX_Insurers_InternalCode",
                table: "Insurers");

            migrationBuilder.DropIndex(
                name: "IX_Insurers_RegulatoryStatus",
                table: "Insurers");

            migrationBuilder.DropIndex(
                name: "UX_Insurers_Lei",
                table: "Insurers");

            migrationBuilder.DropIndex(
                name: "UX_Insurers_Siren",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "Acronym",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "ActivityEndDate",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "ApeNafCode",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "AssetsUnderManagement",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "AuthorizationDate",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "ComplaintsProcedureUrl",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "CustomerCount",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "CustomerSegmentsJson",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "DataQualityNotes",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "DataSourceType",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "DistributionChannelsJson",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "EiopaRegisterId",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "EmployeeCount",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "ExerciseRegime",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "FinancialDataYear",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "FormerNamesJson",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "GeographicCoverage",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "GroupName",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "GroupWebsiteUrl",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "HeadquartersSiret",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "History",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "HomeCountryCode",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "IncorporationCountryCode",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "IncorporationDate",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "InsurerType",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "InternalCode",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "IsGroupHead",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "IsLifeInsurer",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "IsNonLifeInsurer",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "IsReinsurer",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "IsSubjectToSolvencyII",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "KeyFacts",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "LastVerifiedAt",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "LegalForm",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "LegalName",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "Lei",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "LongDescription",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "MainActivitiesJson",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "Mission",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "OfficialRegistryUrl",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "OwnershipPercentage",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "ParentLegalEntityName",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "ParentLei",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "PrivacyPolicyUrl",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "ProductSpecialtiesJson",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "RatingAgency",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "RatingDate",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "RatingOutlook",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "RatingSourceUrl",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "RcsCity",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "RcsNumber",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "RegulatoryNotes",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "RegulatoryStatus",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "RetrievedAt",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "ShortDescription",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "Siren",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "SourceName",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "SourceReference",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "SourceUrl",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "SupervisoryAuthorityCountryCode",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "SupervisoryAuthorityName",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "SupervisoryRegisterId",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "SupervisoryRegisterName",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "SuspensionDate",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "TradeName",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "UltimateParentLei",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "UltimateParentName",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "ValidFrom",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "ValidTo",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "VatNumber",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "VerificationStatus",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "VerifiedBy",
                table: "Insurers");

            migrationBuilder.DropColumn(
                name: "WithdrawalDate",
                table: "Insurers");
        }
    }
}
