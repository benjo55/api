using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddConfigurationItemApplicationProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationProfiles",
                schema: "cmdb",
                columns: table => new
                {
                    ConfigurationItemId = table.Column<int>(type: "int", nullable: false),
                    ApplicationNature = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    InternetExposed = table.Column<bool>(type: "bit", nullable: true),
                    LegalOwnerEntity = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    OtherStakeholders = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SourceCodeAvailable = table.Column<bool>(type: "bit", nullable: true),
                    HostingMode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    HostingProvider = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    CloudServiceModel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    HostingNetworkZone = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    AuthenticationMode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    IamSolution = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    StandalonePasswordRules = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MfaEnabled = table.Column<bool>(type: "bit", nullable: true),
                    InternalTechnicalAdminCount = table.Column<int>(type: "int", nullable: true),
                    ExternalTechnicalAdminCount = table.Column<int>(type: "int", nullable: true),
                    LastAccessRecertificationDate = table.Column<DateTime>(type: "date", nullable: true),
                    LastAccessRemediationPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    PreviousAccessRecertificationDate = table.Column<DateTime>(type: "date", nullable: true),
                    PreviousAccessRemediationPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    CodeScanEnabled = table.Column<bool>(type: "bit", nullable: true),
                    LastPentestDate = table.Column<DateTime>(type: "date", nullable: true),
                    PreviousPentestDate = table.Column<DateTime>(type: "date", nullable: true),
                    LastRedTeamDate = table.Column<DateTime>(type: "date", nullable: true),
                    LastBugBountyDate = table.Column<DateTime>(type: "date", nullable: true),
                    OpenRecommendationsLow = table.Column<int>(type: "int", nullable: true),
                    OpenRecommendationsMedium = table.Column<int>(type: "int", nullable: true),
                    OpenRecommendationsHigh = table.Column<int>(type: "int", nullable: true),
                    OverdueRecommendationsLow = table.Column<int>(type: "int", nullable: true),
                    OverdueRecommendationsMedium = table.Column<int>(type: "int", nullable: true),
                    OverdueRecommendationsHigh = table.Column<int>(type: "int", nullable: true),
                    SecurityComments = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RestorationTestedWithinYear = table.Column<bool>(type: "bit", nullable: true),
                    LastRestorationTestResult = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    FailoverTestPerformed = table.Column<bool>(type: "bit", nullable: true),
                    LastFailoverTestDate = table.Column<DateTime>(type: "date", nullable: true),
                    PreviousFailoverTestDate = table.Column<DateTime>(type: "date", nullable: true),
                    PendingTestActionsCount = table.Column<int>(type: "int", nullable: true),
                    ProcessesPersonalData = table.Column<bool>(type: "bit", nullable: true),
                    NonProductionPersonalData = table.Column<bool>(type: "bit", nullable: true),
                    NonProductionBusinessData = table.Column<bool>(type: "bit", nullable: true),
                    PersonalDataPseudonymization = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationProfiles", x => x.ConfigurationItemId);
                    table.ForeignKey(
                        name: "FK_ApplicationProfiles_ConfigurationItems_ConfigurationItemId",
                        column: x => x.ConfigurationItemId,
                        principalSchema: "cmdb",
                        principalTable: "ConfigurationItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationProfiles",
                schema: "cmdb");
        }
    }
}
