using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class FinancialSupportTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinancialSupports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ISIN = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    SupportType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MarketingName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LegalName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AMFCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BloombergCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MorningstarCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CUSIP = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: true),
                    SEDOL = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: true),
                    AssetManager = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DepositaryBank = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Custodian = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InceptionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosureDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false),
                    AssetClass = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SubAssetClass = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    GeographicFocus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SectorFocus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CapitalizationPolicy = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    InvestmentStrategy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LegalForm = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ManagementStyle = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UCITSCategory = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MinimumSubscription = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: true),
                    MinimumHolding = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: true),
                    ManagementFee = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: true),
                    PerformanceFee = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: true),
                    TurnoverRate = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: true),
                    AUM = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: true),
                    IsCapitalGuaranteed = table.Column<bool>(type: "bit", nullable: true),
                    IsCurrencyHedged = table.Column<bool>(type: "bit", nullable: true),
                    Benchmark = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    HasESGLabel = table.Column<bool>(type: "bit", nullable: true),
                    ESGLabel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SFDRClassification = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ESGScore = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: true),
                    ESGScoreProvider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MifidTargetMarket = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MifidRiskTolerance = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MifidClientType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LastValuationAmount = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: true),
                    LastValuationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WeeklyVolatility = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: true),
                    MaxDrawdown1Y = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: true),
                    Distributor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsAvailableOnline = table.Column<bool>(type: "bit", nullable: true),
                    IsAdvisedSale = table.Column<bool>(type: "bit", nullable: true),
                    IsEligiblePEA = table.Column<bool>(type: "bit", nullable: true),
                    CountryOfDistribution = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FundDomicile = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PrimaryListingMarket = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsFundOfFunds = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialSupports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClientTypeCompliances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FinancialSupportId = table.Column<int>(type: "int", nullable: false),
                    ClientType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MifidCategory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsEligible = table.Column<bool>(type: "bit", nullable: false),
                    ExclusionReason = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientTypeCompliances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientTypeCompliances_FinancialSupports_FinancialSupportId",
                        column: x => x.FinancialSupportId,
                        principalTable: "FinancialSupports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DistributionChannels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FinancialSupportId = table.Column<int>(type: "int", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaxEntryFee = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: false),
                    CommissionRate = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: false),
                    HasRetrocession = table.Column<bool>(type: "bit", nullable: false),
                    CommercialName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DistributionChannels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DistributionChannels_FinancialSupports_FinancialSupportId",
                        column: x => x.FinancialSupportId,
                        principalTable: "FinancialSupports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ESGDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FinancialSupportId = table.Column<int>(type: "int", nullable: false),
                    IsSFDRApplicable = table.Column<bool>(type: "bit", nullable: false),
                    SFDRArticle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ecolabel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CarbonFootprint = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: false),
                    GenderEqualityScore = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: false),
                    WaterUseScore = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: false),
                    ESGProvider = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RiskLabel = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ESGDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ESGDetails_FinancialSupports_FinancialSupportId",
                        column: x => x.FinancialSupportId,
                        principalTable: "FinancialSupports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FundLifeCycles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FinancialSupportId = table.Column<int>(type: "int", nullable: false),
                    InceptionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSubscriptionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundLifeCycles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FundLifeCycles_FinancialSupports_FinancialSupportId",
                        column: x => x.FinancialSupportId,
                        principalTable: "FinancialSupports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FundScenarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FinancialSupportId = table.Column<int>(type: "int", nullable: false),
                    ScenarioType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProjectedPerformance = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: false),
                    CostImpact = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: false),
                    Methodology = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundScenarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FundScenarios_FinancialSupports_FinancialSupportId",
                        column: x => x.FinancialSupportId,
                        principalTable: "FinancialSupports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MarketingTargets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FinancialSupportId = table.Column<int>(type: "int", nullable: false),
                    CountryCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    ChannelType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Segment = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsDistributed = table.Column<bool>(type: "bit", nullable: false),
                    IsHighlighted = table.Column<bool>(type: "bit", nullable: false),
                    LocalName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketingTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketingTargets_FinancialSupports_FinancialSupportId",
                        column: x => x.FinancialSupportId,
                        principalTable: "FinancialSupports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MultilingualDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FinancialSupportId = table.Column<int>(type: "int", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Language = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PublicationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MultilingualDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MultilingualDocuments_FinancialSupports_FinancialSupportId",
                        column: x => x.FinancialSupportId,
                        principalTable: "FinancialSupports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShareClasses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FinancialSupportId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ISIN = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    LaunchDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DistributionPolicy = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    EntryFee = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: false),
                    ExitFee = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: false),
                    ManagementFee = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: false),
                    OngoingCharges = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: false),
                    CountryOfRegistration = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShareClasses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShareClasses_FinancialSupports_FinancialSupportId",
                        column: x => x.FinancialSupportId,
                        principalTable: "FinancialSupports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupportDistributions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FinancialSupportId = table.Column<int>(type: "int", nullable: false),
                    EntryFee = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: false),
                    ExitFee = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: false),
                    OngoingCharges = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: false),
                    HasRetrocession = table.Column<bool>(type: "bit", nullable: false),
                    DistributionFrequency = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastDistributionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AverageRetrocessionRate = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: true),
                    IsCleanShare = table.Column<bool>(type: "bit", nullable: true),
                    DistributionRegion = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportDistributions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportDistributions_FinancialSupports_FinancialSupportId",
                        column: x => x.FinancialSupportId,
                        principalTable: "FinancialSupports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupportDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FinancialSupportId = table.Column<int>(type: "int", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PublicationDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportDocuments_FinancialSupports_FinancialSupportId",
                        column: x => x.FinancialSupportId,
                        principalTable: "FinancialSupports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupportFeeDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FinancialSupportId = table.Column<int>(type: "int", nullable: false),
                    FeeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(10,4)", precision: 18, scale: 5, nullable: false),
                    Conditions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportFeeDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportFeeDetails_FinancialSupports_FinancialSupportId",
                        column: x => x.FinancialSupportId,
                        principalTable: "FinancialSupports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupportHistoricalData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FinancialSupportId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Nav = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 5, nullable: true),
                    PerformanceYTD = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: true),
                    AUM = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: true),
                    Volatility1Y = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportHistoricalData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportHistoricalData_FinancialSupports_FinancialSupportId",
                        column: x => x.FinancialSupportId,
                        principalTable: "FinancialSupports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupportLookthroughAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FinancialSupportId = table.Column<int>(type: "int", nullable: false),
                    AssetName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ISIN = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    AssetClass = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(5,2)", precision: 18, scale: 5, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportLookthroughAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportLookthroughAssets_FinancialSupports_FinancialSupportId",
                        column: x => x.FinancialSupportId,
                        principalTable: "FinancialSupports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupportPortfolioLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FinancialSupportId = table.Column<int>(type: "int", nullable: false),
                    PortfolioCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Strategy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RiskProfile = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportPortfolioLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportPortfolioLinks_FinancialSupports_FinancialSupportId",
                        column: x => x.FinancialSupportId,
                        principalTable: "FinancialSupports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupportRegulations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FinancialSupportId = table.Column<int>(type: "int", nullable: false),
                    IsSFDRApplicable = table.Column<bool>(type: "bit", nullable: false),
                    SFDRLevel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ESGScoreProvider = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MifidCategory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Ecolabel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HasPrincipalAdverseImpacts = table.Column<bool>(type: "bit", nullable: true),
                    PAIIndicators = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KIIDDocumentUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastKIIDUpdate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProspectusUrl = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportRegulations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportRegulations_FinancialSupports_FinancialSupportId",
                        column: x => x.FinancialSupportId,
                        principalTable: "FinancialSupports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupportRiskProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FinancialSupportId = table.Column<int>(type: "int", nullable: false),
                    SRRI = table.Column<int>(type: "int", nullable: false),
                    RiskDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Volatility3Y = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: true),
                    MorningstarRiskRating = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RiskRegion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RiskNarrative = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportRiskProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportRiskProfiles_FinancialSupports_FinancialSupportId",
                        column: x => x.FinancialSupportId,
                        principalTable: "FinancialSupports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupportTechnicals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FinancialSupportId = table.Column<int>(type: "int", nullable: false),
                    DataSource = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SyncStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastSyncDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTechnicals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportTechnicals_FinancialSupports_FinancialSupportId",
                        column: x => x.FinancialSupportId,
                        principalTable: "FinancialSupports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupportValuations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FinancialSupportId = table.Column<int>(type: "int", nullable: false),
                    ValuationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Nav = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ValuationCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    IsEstimated = table.Column<bool>(type: "bit", nullable: false),
                    SourceTimestamp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SourceReferenceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PerformanceYTD = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: true),
                    Performance1Y = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: true),
                    Volatility1Y = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportValuations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportValuations_FinancialSupports_FinancialSupportId",
                        column: x => x.FinancialSupportId,
                        principalTable: "FinancialSupports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaxDatas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FinancialSupportId = table.Column<int>(type: "int", nullable: false),
                    IsFATCAEligible = table.Column<bool>(type: "bit", nullable: false),
                    IsCRSReportable = table.Column<bool>(type: "bit", nullable: false),
                    CountryOfTaxation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LegalForm = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxDatas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxDatas_FinancialSupports_FinancialSupportId",
                        column: x => x.FinancialSupportId,
                        principalTable: "FinancialSupports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientTypeCompliances_FinancialSupportId",
                table: "ClientTypeCompliances",
                column: "FinancialSupportId");

            migrationBuilder.CreateIndex(
                name: "IX_DistributionChannels_FinancialSupportId",
                table: "DistributionChannels",
                column: "FinancialSupportId");

            migrationBuilder.CreateIndex(
                name: "IX_ESGDetails_FinancialSupportId",
                table: "ESGDetails",
                column: "FinancialSupportId");

            migrationBuilder.CreateIndex(
                name: "IX_FundLifeCycles_FinancialSupportId",
                table: "FundLifeCycles",
                column: "FinancialSupportId");

            migrationBuilder.CreateIndex(
                name: "IX_FundScenarios_FinancialSupportId",
                table: "FundScenarios",
                column: "FinancialSupportId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketingTargets_FinancialSupportId",
                table: "MarketingTargets",
                column: "FinancialSupportId");

            migrationBuilder.CreateIndex(
                name: "IX_MultilingualDocuments_FinancialSupportId",
                table: "MultilingualDocuments",
                column: "FinancialSupportId");

            migrationBuilder.CreateIndex(
                name: "IX_ShareClasses_FinancialSupportId",
                table: "ShareClasses",
                column: "FinancialSupportId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportDistributions_FinancialSupportId",
                table: "SupportDistributions",
                column: "FinancialSupportId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportDocuments_FinancialSupportId",
                table: "SupportDocuments",
                column: "FinancialSupportId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportFeeDetails_FinancialSupportId",
                table: "SupportFeeDetails",
                column: "FinancialSupportId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportHistoricalData_FinancialSupportId",
                table: "SupportHistoricalData",
                column: "FinancialSupportId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportLookthroughAssets_FinancialSupportId",
                table: "SupportLookthroughAssets",
                column: "FinancialSupportId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportPortfolioLinks_FinancialSupportId",
                table: "SupportPortfolioLinks",
                column: "FinancialSupportId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportRegulations_FinancialSupportId",
                table: "SupportRegulations",
                column: "FinancialSupportId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportRiskProfiles_FinancialSupportId",
                table: "SupportRiskProfiles",
                column: "FinancialSupportId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTechnicals_FinancialSupportId",
                table: "SupportTechnicals",
                column: "FinancialSupportId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportValuations_FinancialSupportId",
                table: "SupportValuations",
                column: "FinancialSupportId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxDatas_FinancialSupportId",
                table: "TaxDatas",
                column: "FinancialSupportId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientTypeCompliances");

            migrationBuilder.DropTable(
                name: "DistributionChannels");

            migrationBuilder.DropTable(
                name: "ESGDetails");

            migrationBuilder.DropTable(
                name: "FundLifeCycles");

            migrationBuilder.DropTable(
                name: "FundScenarios");

            migrationBuilder.DropTable(
                name: "MarketingTargets");

            migrationBuilder.DropTable(
                name: "MultilingualDocuments");

            migrationBuilder.DropTable(
                name: "ShareClasses");

            migrationBuilder.DropTable(
                name: "SupportDistributions");

            migrationBuilder.DropTable(
                name: "SupportDocuments");

            migrationBuilder.DropTable(
                name: "SupportFeeDetails");

            migrationBuilder.DropTable(
                name: "SupportHistoricalData");

            migrationBuilder.DropTable(
                name: "SupportLookthroughAssets");

            migrationBuilder.DropTable(
                name: "SupportPortfolioLinks");

            migrationBuilder.DropTable(
                name: "SupportRegulations");

            migrationBuilder.DropTable(
                name: "SupportRiskProfiles");

            migrationBuilder.DropTable(
                name: "SupportTechnicals");

            migrationBuilder.DropTable(
                name: "SupportValuations");

            migrationBuilder.DropTable(
                name: "TaxDatas");

            migrationBuilder.DropTable(
                name: "FinancialSupports");
        }
    }
}
