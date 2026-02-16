USE [master]
GO
/****** Object:  Database [Life]    Script Date: 14/12/2025 14:23:27 ******/
CREATE DATABASE [Life]
 CONTAINMENT = NONE
 ON  PRIMARY 
( NAME = N'Life', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\DATA\Life.mdf' , SIZE = 280512KB , MAXSIZE = UNLIMITED, FILEGROWTH = 65536KB )
 LOG ON 
( NAME = N'Life_log', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\DATA\Life_log.ldf' , SIZE = 659392KB , MAXSIZE = 2048GB , FILEGROWTH = 65536KB )
 WITH CATALOG_COLLATION = DATABASE_DEFAULT, LEDGER = OFF
GO
ALTER DATABASE [Life] SET COMPATIBILITY_LEVEL = 150
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [Life].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [Life] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [Life] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [Life] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [Life] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [Life] SET ARITHABORT OFF 
GO
ALTER DATABASE [Life] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [Life] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [Life] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [Life] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [Life] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [Life] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [Life] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [Life] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [Life] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [Life] SET  DISABLE_BROKER 
GO
ALTER DATABASE [Life] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [Life] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [Life] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [Life] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [Life] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [Life] SET READ_COMMITTED_SNAPSHOT OFF 
GO
ALTER DATABASE [Life] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [Life] SET RECOVERY FULL 
GO
ALTER DATABASE [Life] SET  MULTI_USER 
GO
ALTER DATABASE [Life] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [Life] SET DB_CHAINING OFF 
GO
ALTER DATABASE [Life] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [Life] SET TARGET_RECOVERY_TIME = 60 SECONDS 
GO
ALTER DATABASE [Life] SET DELAYED_DURABILITY = DISABLED 
GO
ALTER DATABASE [Life] SET ACCELERATED_DATABASE_RECOVERY = OFF  
GO
ALTER DATABASE [Life] SET QUERY_STORE = OFF
GO
USE [Life]
GO
/****** Object:  FullTextCatalog [ft]    Script Date: 14/12/2025 14:23:27 ******/
CREATE FULLTEXT CATALOG [ft] AS DEFAULT
GO
/****** Object:  UserDefinedFunction [dbo].[ProperCase]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- 1. Création d'une fonction pour mettre un mot en nom propre
CREATE FUNCTION [dbo].[ProperCase] (@input NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS
BEGIN
    DECLARE @output NVARCHAR(MAX) = ''
    DECLARE @word NVARCHAR(100)
    DECLARE @position INT = 1
    DECLARE @delimiter CHAR(1)
    DECLARE @i INT
    DECLARE @length INT

    -- Remplace les tirets et les espaces par un caractère spécial temporaire
    SET @input = LOWER(@input)
    SET @input = REPLACE(REPLACE(@input, '-', '|-|'), ' ', '| |')

    -- Boucle sur chaque segment
    WHILE CHARINDEX('|', @input) > 0
    BEGIN
        SET @word = LEFT(@input, CHARINDEX('|', @input) - 1)
        IF LEN(@word) > 0
            SET @word = UPPER(LEFT(@word, 1)) + SUBSTRING(@word, 2, LEN(@word))

        SET @output = @output + @word

        SET @delimiter = SUBSTRING(@input, CHARINDEX('|', @input) + 1, 1)
        SET @output = @output + 
            CASE @delimiter 
                WHEN '-' THEN '-'
                WHEN ' ' THEN ' '
                ELSE ''
            END

        SET @input = SUBSTRING(@input, CHARINDEX('|', @input) + 2, LEN(@input))
    END

    -- Dernier mot (s’il n'y a plus de séparateur)
    IF LEN(@input) > 0
        SET @output = @output + UPPER(LEFT(@input, 1)) + SUBSTRING(@input, 2, LEN(@input))

    RETURN @output
END
GO
/****** Object:  Table [dbo].[Operations]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Operations](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ContractId] [int] NOT NULL,
	[CompartmentId] [int] NULL,
	[Type] [int] NOT NULL,
	[Status] [int] NOT NULL,
	[OperationDate] [datetime2](7) NOT NULL,
	[Amount] [decimal](20, 7) NULL,
	[Currency] [nvarchar](max) NOT NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[UpdatedDate] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_Operations] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Contracts]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Contracts](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ContractLabel] [nvarchar](max) NOT NULL,
	[DateSign] [datetime2](7) NOT NULL,
	[PersonId] [int] NULL,
	[ContractNumber] [nvarchar](max) NOT NULL,
	[ProductId] [int] NULL,
	[CreatedDate] [datetime2](7) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[PostalAddress] [nvarchar](max) NOT NULL,
	[TaxAddress] [nvarchar](max) NOT NULL,
	[BeneficiaryClauseId] [int] NULL,
	[AdvisorComment] [nvarchar](max) NULL,
	[ContractType] [nvarchar](max) NOT NULL,
	[CreatedByUserId] [int] NULL,
	[Currency] [nvarchar](max) NOT NULL,
	[CurrentValue] [decimal](20, 7) NOT NULL,
	[DateEffect] [datetime2](7) NOT NULL,
	[DateMaturity] [datetime2](7) NULL,
	[EntryFeesRate] [decimal](5, 2) NULL,
	[ExitFeesRate] [decimal](5, 2) NULL,
	[ExternalReference] [nvarchar](max) NULL,
	[HasAlert] [bit] NOT NULL,
	[InitialPremium] [decimal](20, 7) NOT NULL,
	[IsJointContract] [bit] NOT NULL,
	[LastModifiedByUserId] [int] NULL,
	[ManagementFeesRate] [decimal](5, 2) NULL,
	[PaymentMode] [nvarchar](max) NOT NULL,
	[RedemptionValue] [decimal](20, 7) NULL,
	[ScheduledPayment] [decimal](20, 7) NULL,
	[Status] [nvarchar](max) NOT NULL,
	[TotalPaidPremiums] [decimal](20, 7) NOT NULL,
	[Locked] [bit] NOT NULL,
	[NetInvested] [decimal](18, 5) NOT NULL,
	[PerformancePercent] [decimal](18, 5) NULL,
	[TotalPayments] [decimal](18, 5) NOT NULL,
	[TotalWithdrawals] [decimal](18, 5) NOT NULL,
 CONSTRAINT [PK_Contracts] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ADD SENSITIVITY CLASSIFICATION TO [dbo].[Contracts].[PostalAddress] WITH (label = 'Confidential', label_id = '331f0b13-76b5-2f1b-a77b-def5a73c73c2', information_type = 'Contact Info', information_type_id = '5c503e21-22c6-81fa-620b-f369b8ec38d1');
GO
/****** Object:  Table [dbo].[OperationSupportAllocations]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[OperationSupportAllocations](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[OperationId] [int] NOT NULL,
	[SupportId] [int] NOT NULL,
	[Amount] [decimal](20, 7) NULL,
	[Percentage] [decimal](18, 4) NULL,
	[NavAtOperation] [decimal](20, 7) NULL,
	[NavDateAtOperation] [datetime2](7) NULL,
	[CompartmentId] [int] NOT NULL,
	[Shares] [decimal](18, 7) NULL,
	[EstimatedNav] [decimal](20, 7) NULL,
	[EstimatedShares] [decimal](20, 7) NULL,
 CONSTRAINT [PK_OperationSupportAllocations] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[FinancialSupportAllocations]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FinancialSupportAllocations](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ContractId] [int] NOT NULL,
	[SupportId] [int] NOT NULL,
	[AllocationPercentage] [decimal](18, 4) NOT NULL,
	[CompartmentId] [int] NOT NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[UpdatedDate] [datetime2](7) NOT NULL,
	[CurrentAmount] [decimal](20, 7) NOT NULL,
	[CurrentShares] [decimal](18, 7) NOT NULL,
	[InvestedAmount] [decimal](20, 7) NOT NULL,
 CONSTRAINT [PK_FinancialSupportAllocations] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[FinancialSupports]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FinancialSupports](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Code] [nvarchar](20) NOT NULL,
	[Label] [nvarchar](200) NOT NULL,
	[ISIN] [nvarchar](12) NOT NULL,
	[SupportType] [nvarchar](30) NOT NULL,
	[Currency] [nvarchar](3) NOT NULL,
	[Status] [nvarchar](20) NOT NULL,
	[MarketingName] [nvarchar](100) NULL,
	[LegalName] [nvarchar](100) NULL,
	[AMFCode] [nvarchar](20) NULL,
	[BloombergCode] [nvarchar](20) NULL,
	[MorningstarCode] [nvarchar](20) NULL,
	[CUSIP] [nvarchar](12) NULL,
	[SEDOL] [nvarchar](12) NULL,
	[AssetManager] [nvarchar](50) NULL,
	[DepositaryBank] [nvarchar](100) NULL,
	[Custodian] [nvarchar](100) NULL,
	[InceptionDate] [datetime2](7) NULL,
	[ClosureDate] [datetime2](7) NULL,
	[IsClosed] [bit] NOT NULL,
	[AssetClass] [nvarchar](50) NULL,
	[SubAssetClass] [nvarchar](50) NULL,
	[GeographicFocus] [nvarchar](50) NULL,
	[SectorFocus] [nvarchar](50) NULL,
	[CapitalizationPolicy] [nvarchar](30) NULL,
	[InvestmentStrategy] [nvarchar](50) NULL,
	[LegalForm] [nvarchar](50) NULL,
	[ManagementStyle] [nvarchar](50) NULL,
	[UCITSCategory] [nvarchar](50) NULL,
	[MinimumSubscription] [decimal](18, 5) NULL,
	[MinimumHolding] [decimal](18, 5) NULL,
	[ManagementFee] [decimal](18, 5) NULL,
	[PerformanceFee] [decimal](18, 5) NULL,
	[TurnoverRate] [decimal](18, 5) NULL,
	[AUM] [decimal](18, 5) NULL,
	[IsCapitalGuaranteed] [bit] NULL,
	[IsCurrencyHedged] [bit] NULL,
	[Benchmark] [nvarchar](10) NULL,
	[HasESGLabel] [bit] NULL,
	[ESGLabel] [nvarchar](50) NULL,
	[SFDRClassification] [nvarchar](50) NULL,
	[ESGScore] [decimal](18, 5) NULL,
	[ESGScoreProvider] [nvarchar](100) NULL,
	[MifidTargetMarket] [nvarchar](50) NULL,
	[MifidRiskTolerance] [nvarchar](50) NULL,
	[MifidClientType] [nvarchar](50) NULL,
	[LastValuationAmount] [decimal](18, 5) NULL,
	[LastValuationDate] [datetime2](7) NULL,
	[WeeklyVolatility] [decimal](18, 5) NULL,
	[MaxDrawdown1Y] [decimal](18, 5) NULL,
	[Distributor] [nvarchar](100) NULL,
	[IsAvailableOnline] [bit] NULL,
	[IsAdvisedSale] [bit] NULL,
	[IsEligiblePEA] [bit] NULL,
	[CountryOfDistribution] [nvarchar](100) NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[UpdatedDate] [datetime2](7) NOT NULL,
	[FundDomicile] [nvarchar](50) NULL,
	[PrimaryListingMarket] [nvarchar](50) NULL,
	[IsFundOfFunds] [bit] NULL,
 CONSTRAINT [PK_FinancialSupports] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  View [dbo].[vw_DataIntegrityCheck]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [dbo].[vw_DataIntegrityCheck]
AS

-- 1️⃣ Opérations sans allocations
SELECT 
    '1️⃣ Opération sans allocation' AS CheckName,
    CAST(o.Id AS NVARCHAR(50)) AS RefId,
    o.ContractId,
    NULL AS SupportId,
    NULL AS ISIN,
    NULL AS Label,
    o.Amount,
    NULL AS Percentage,
    'OperationSupportAllocations' AS TableSource
FROM dbo.Operations AS o
LEFT JOIN dbo.OperationSupportAllocations AS a ON a.OperationId = o.Id
WHERE a.Id IS NULL

UNION ALL

-- 2️⃣ Allocations orphelines
SELECT 
    '2️⃣ Allocation orpheline' AS CheckName,
    CAST(a.Id AS NVARCHAR(50)) AS RefId,
    NULL AS ContractId,
    a.SupportId,
    s.ISIN,
    s.Label,
    a.Amount,
    a.Percentage,
    'OperationSupportAllocations' AS TableSource
FROM dbo.OperationSupportAllocations AS a
LEFT JOIN dbo.Operations AS o ON o.Id = a.OperationId
LEFT JOIN dbo.FinancialSupports AS s ON s.Id = a.SupportId
WHERE o.Id IS NULL

UNION ALL

-- 3️⃣ Allocations à zéro
SELECT 
    '3️⃣ Allocation à zéro' AS CheckName,
    CAST(a.Id AS NVARCHAR(50)) AS RefId,
    NULL AS ContractId,
    a.SupportId,
    s.ISIN,
    s.Label,
    a.Amount,
    a.Percentage,
    'OperationSupportAllocations' AS TableSource
FROM dbo.OperationSupportAllocations AS a
INNER JOIN dbo.FinancialSupports AS s ON s.Id = a.SupportId
WHERE ISNULL(a.Amount,0)=0 AND ISNULL(a.Percentage,0)=0

UNION ALL

-- 4️⃣ Contrat incohérent
SELECT 
    '4️⃣ Contrat incohérent' AS CheckName,
    CAST(a.Id AS NVARCHAR(50)) AS RefId,
    o.ContractId,
    a.SupportId,
    s.ISIN,
    s.Label,
    NULL AS Amount,
    NULL AS Percentage,
    'OperationSupportAllocations' AS TableSource
FROM dbo.OperationSupportAllocations a
INNER JOIN dbo.Operations o ON o.Id = a.OperationId
LEFT JOIN dbo.Contracts c ON c.Id = o.ContractId
LEFT JOIN dbo.FinancialSupports s ON s.Id = a.SupportId
WHERE o.ContractId <> c.Id

UNION ALL

-- 5️⃣ Supports incohérents (shares négatifs ou montants incorrects)
SELECT 
    '5️⃣ FSA incohérente' AS CheckName,
    CAST(fsa.Id AS NVARCHAR(50)) AS RefId,
    fsa.ContractId,
    fsa.SupportId,
    fs.ISIN,
    fs.Label,
    fsa.CurrentAmount,
    fsa.CurrentShares,
    'FinancialSupportAllocations' AS TableSource
FROM dbo.FinancialSupportAllocations fsa
INNER JOIN dbo.FinancialSupports fs ON fs.Id = fsa.SupportId
WHERE fsa.CurrentShares < 0 
   OR ABS(ISNULL(fsa.CurrentAmount,0) - ISNULL(fsa.CurrentShares * fs.LastValuationAmount,0)) > 0.01

UNION ALL

-- 6️⃣ Supports présents dans OSA mais absents de FSA
SELECT 
    '6️⃣ Support manquant dans FSA' AS CheckName,
    CAST(a.Id AS NVARCHAR(50)) AS RefId,
    NULL AS ContractId,
    a.SupportId,
    fs.ISIN,
    fs.Label,
    NULL AS Amount,
    NULL AS Percentage,
    'CrossCheck' AS TableSource
FROM dbo.OperationSupportAllocations a
LEFT JOIN dbo.FinancialSupportAllocations fsa ON fsa.SupportId = a.SupportId
INNER JOIN dbo.FinancialSupports fs ON fs.Id = a.SupportId
WHERE fsa.SupportId IS NULL;
GO
/****** Object:  Table [dbo].[__EFMigrationsHistory]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[__EFMigrationsHistory](
	[MigrationId] [nvarchar](150) NOT NULL,
	[ProductVersion] [nvarchar](32) NOT NULL,
 CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY CLUSTERED 
(
	[MigrationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AdvanceDetails]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AdvanceDetails](
	[OperationId] [int] NOT NULL,
	[Amount] [decimal](20, 7) NOT NULL,
	[InterestRate] [decimal](18, 4) NOT NULL,
	[MaturityDate] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_AdvanceDetails] PRIMARY KEY CLUSTERED 
(
	[OperationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ArbitrageDetails]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ArbitrageDetails](
	[OperationId] [int] NOT NULL,
	[FromSupportId] [int] NOT NULL,
	[ToSupportId] [int] NOT NULL,
	[Percentage] [decimal](18, 4) NOT NULL,
 CONSTRAINT [PK_ArbitrageDetails] PRIMARY KEY CLUSTERED 
(
	[OperationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[BeneficiaryClausePersons]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BeneficiaryClausePersons](
	[ClauseId] [int] NOT NULL,
	[PersonId] [int] NOT NULL,
	[RelationWithClause] [nvarchar](max) NOT NULL,
	[Percentage] [decimal](5, 2) NOT NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[UpdatedDate] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_BeneficiaryClausePersons] PRIMARY KEY CLUSTERED 
(
	[ClauseId] ASC,
	[PersonId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[BeneficiaryClauses]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BeneficiaryClauses](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ClauseType] [nvarchar](max) NOT NULL,
	[ContractId] [int] NULL,
	[Description] [nvarchar](max) NOT NULL,
	[Status] [nvarchar](max) NOT NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[UpdatedDate] [datetime2](7) NOT NULL,
	[Locked] [bit] NOT NULL,
 CONSTRAINT [PK_BeneficiaryClauses] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Brands]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Brands](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[BrandName] [nvarchar](max) NOT NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[BrandCode] [nvarchar](max) NOT NULL,
	[City] [nvarchar](max) NOT NULL,
	[ContactEmail] [nvarchar](max) NOT NULL,
	[Country] [nvarchar](max) NOT NULL,
	[Description] [nvarchar](max) NOT NULL,
	[FacebookUrl] [nvarchar](max) NOT NULL,
	[FoundedYear] [int] NULL,
	[Founder] [nvarchar](max) NOT NULL,
	[Industry] [nvarchar](max) NOT NULL,
	[InstagramUrl] [nvarchar](max) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[LinkedInUrl] [nvarchar](max) NOT NULL,
	[LogoUrl] [nvarchar](max) NOT NULL,
	[MainColor] [nvarchar](max) NOT NULL,
	[Notes] [nvarchar](max) NOT NULL,
	[ParentGroup] [nvarchar](max) NOT NULL,
	[Slogan] [nvarchar](max) NOT NULL,
	[Website] [nvarchar](max) NOT NULL,
	[Locked] [bit] NOT NULL,
 CONSTRAINT [PK_Brands] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ClientTypeCompliances]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ClientTypeCompliances](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FinancialSupportId] [int] NOT NULL,
	[ClientType] [nvarchar](max) NOT NULL,
	[MifidCategory] [nvarchar](max) NOT NULL,
	[IsEligible] [bit] NOT NULL,
	[ExclusionReason] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_ClientTypeCompliances] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Compartments]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Compartments](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ContractId] [int] NOT NULL,
	[Label] [nvarchar](100) NOT NULL,
	[Description] [nvarchar](max) NOT NULL,
	[ManagementMode] [nvarchar](max) NULL,
	[Notes] [nvarchar](max) NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[UpdatedDate] [datetime2](7) NOT NULL,
	[CurrentValue] [decimal](18, 5) NOT NULL,
	[IsDefault] [bit] NOT NULL,
 CONSTRAINT [PK_Compartments] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ContractInsuredPersons]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ContractInsuredPersons](
	[ContractId] [int] NOT NULL,
	[PersonId] [int] NOT NULL,
 CONSTRAINT [PK_ContractInsuredPersons] PRIMARY KEY CLUSTERED 
(
	[ContractId] ASC,
	[PersonId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ContractOptions]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ContractOptions](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ContractId] [int] NOT NULL,
	[Description] [nvarchar](max) NULL,
	[IsActive] [bit] NOT NULL,
	[ContractOptionTypeId] [int] NOT NULL,
	[CustomParameters] [nvarchar](max) NULL,
 CONSTRAINT [PK_ContractOptions] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ContractOptionTypes]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ContractOptionTypes](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Code] [nvarchar](450) NOT NULL,
	[Category] [nvarchar](max) NOT NULL,
	[Label] [nvarchar](max) NOT NULL,
	[Objective] [nvarchar](max) NOT NULL,
	[Mechanism] [nvarchar](max) NOT NULL,
	[DefaultCost] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_ContractOptionTypes] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ContractSupportHoldings]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ContractSupportHoldings](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ContractId] [int] NOT NULL,
	[SupportId] [int] NOT NULL,
	[TotalShares] [decimal](20, 7) NOT NULL,
	[TotalInvested] [decimal](20, 7) NOT NULL,
	[Pru] [decimal](20, 7) NOT NULL,
	[LastUpdated] [datetime2](7) NOT NULL,
	[CurrentAmount] [decimal](18, 7) NULL,
	[PerformancePercent] [decimal](18, 4) NULL,
 CONSTRAINT [PK_ContractSupportHoldings] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ContractValuations]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ContractValuations](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ContractId] [int] NOT NULL,
	[Value] [decimal](18, 2) NOT NULL,
	[ValuationDate] [datetime2](7) NOT NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_ContractValuations] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DistributionChannels]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DistributionChannels](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FinancialSupportId] [int] NOT NULL,
	[Channel] [nvarchar](max) NOT NULL,
	[MaxEntryFee] [decimal](18, 5) NOT NULL,
	[CommissionRate] [decimal](18, 5) NOT NULL,
	[HasRetrocession] [bit] NOT NULL,
	[CommercialName] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_DistributionChannels] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Documents]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Documents](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FileName] [nvarchar](max) NOT NULL,
	[FileType] [nvarchar](max) NOT NULL,
	[Url] [nvarchar](max) NOT NULL,
	[UploadedAt] [datetime2](7) NOT NULL,
	[ContractId] [int] NULL,
 CONSTRAINT [PK_Documents] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[EntityHistories]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[EntityHistories](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[EntityName] [nvarchar](max) NOT NULL,
	[EntityId] [int] NOT NULL,
	[PropertyName] [nvarchar](max) NOT NULL,
	[OldValue] [nvarchar](max) NULL,
	[NewValue] [nvarchar](max) NULL,
	[ModifiedAt] [datetime2](7) NOT NULL,
	[ModifiedBy] [nvarchar](max) NOT NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[UpdatedDate] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_EntityHistories] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ESGDetails]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ESGDetails](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FinancialSupportId] [int] NOT NULL,
	[IsSFDRApplicable] [bit] NOT NULL,
	[SFDRArticle] [nvarchar](max) NOT NULL,
	[Ecolabel] [nvarchar](max) NOT NULL,
	[CarbonFootprint] [decimal](18, 5) NOT NULL,
	[GenderEqualityScore] [decimal](18, 5) NOT NULL,
	[WaterUseScore] [decimal](18, 5) NOT NULL,
	[ESGProvider] [nvarchar](max) NOT NULL,
	[RiskLabel] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_ESGDetails] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[FieldDescriptions]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FieldDescriptions](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[EntityName] [nvarchar](max) NOT NULL,
	[FieldName] [nvarchar](max) NOT NULL,
	[Description] [nvarchar](max) NOT NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[UpdatedDate] [datetime2](7) NULL,
 CONSTRAINT [PK_FieldDescriptions] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[FundLifeCycles]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FundLifeCycles](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FinancialSupportId] [int] NOT NULL,
	[InceptionDate] [datetime2](7) NULL,
	[ClosingDate] [datetime2](7) NULL,
	[LastSubscriptionDate] [datetime2](7) NULL,
	[IsActive] [bit] NOT NULL,
 CONSTRAINT [PK_FundLifeCycles] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[FundScenarios]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FundScenarios](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FinancialSupportId] [int] NOT NULL,
	[ScenarioType] [nvarchar](max) NOT NULL,
	[ProjectedPerformance] [decimal](18, 5) NOT NULL,
	[CostImpact] [decimal](18, 5) NOT NULL,
	[Methodology] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_FundScenarios] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Insurers]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Insurers](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](max) NOT NULL,
	[RegistrationNumber] [nvarchar](max) NOT NULL,
	[FoundedYear] [int] NOT NULL,
	[HeadQuarters] [nvarchar](max) NULL,
	[PhoneNumber] [nvarchar](max) NULL,
	[Email] [nvarchar](max) NULL,
	[WebSite] [nvarchar](max) NULL,
	[IsActive] [nvarchar](max) NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[PostalAddress] [nvarchar](max) NULL,
	[Locked] [bit] NOT NULL,
 CONSTRAINT [PK_InsuranceCompanies] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MarketingTargets]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MarketingTargets](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FinancialSupportId] [int] NOT NULL,
	[CountryCode] [nvarchar](5) NOT NULL,
	[ChannelType] [nvarchar](20) NOT NULL,
	[Segment] [nvarchar](50) NOT NULL,
	[IsDistributed] [bit] NOT NULL,
	[IsHighlighted] [bit] NOT NULL,
	[LocalName] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_MarketingTargets] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MultilingualDocuments]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MultilingualDocuments](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FinancialSupportId] [int] NOT NULL,
	[DocumentType] [nvarchar](max) NOT NULL,
	[Language] [nvarchar](max) NOT NULL,
	[Url] [nvarchar](max) NOT NULL,
	[PublicationDate] [datetime2](7) NOT NULL,
	[Version] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_MultilingualDocuments] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Notaries]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Notaries](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[RaisonSociale] [nvarchar](max) NULL,
	[Adresse1] [nvarchar](max) NULL,
	[Adresse2] [nvarchar](max) NULL,
	[CodePostal] [nvarchar](max) NULL,
	[Ville] [nvarchar](max) NULL,
	[Telephone] [nvarchar](max) NULL,
	[Fax] [nvarchar](max) NULL,
	[SiteWeb] [nvarchar](max) NULL,
	[AdresseMail] [nvarchar](max) NULL,
	[NomContactNotaire] [nvarchar](max) NULL,
 CONSTRAINT [PK_Notaries] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ADD SENSITIVITY CLASSIFICATION TO [dbo].[Notaries].[CodePostal] WITH (label = 'Confidential', label_id = '331f0b13-76b5-2f1b-a77b-def5a73c73c2', information_type = 'Contact Info', information_type_id = '5c503e21-22c6-81fa-620b-f369b8ec38d1');
GO
ADD SENSITIVITY CLASSIFICATION TO [dbo].[Notaries].[Telephone] WITH (label = 'Confidential', label_id = '331f0b13-76b5-2f1b-a77b-def5a73c73c2', information_type = 'Contact Info', information_type_id = '5c503e21-22c6-81fa-620b-f369b8ec38d1');
GO
ADD SENSITIVITY CLASSIFICATION TO [dbo].[Notaries].[AdresseMail] WITH (label = 'Confidential', label_id = '331f0b13-76b5-2f1b-a77b-def5a73c73c2', information_type = 'Contact Info', information_type_id = '5c503e21-22c6-81fa-620b-f369b8ec38d1');
GO
/****** Object:  Table [dbo].[PaymentDetails]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PaymentDetails](
	[OperationId] [int] NOT NULL,
	[PaymentMethod] [nvarchar](max) NOT NULL,
	[Frequency] [nvarchar](max) NULL,
	[SourceOfFunds] [nvarchar](max) NULL,
	[Amount] [decimal](20, 7) NOT NULL,
 CONSTRAINT [PK_PaymentDetails] PRIMARY KEY CLUSTERED 
(
	[OperationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Permissions]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Permissions](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[PermissionName] [nvarchar](50) NOT NULL,
	[Description] [nvarchar](max) NOT NULL,
	[PermissionCode] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_Permissions] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Persons]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Persons](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FirstName] [nvarchar](200) NOT NULL,
	[LastName] [nvarchar](200) NOT NULL,
	[BirthCountry] [nvarchar](max) NOT NULL,
	[BirthDate] [datetime2](7) NOT NULL,
	[Sex] [nvarchar](max) NOT NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[PostalAddress] [nvarchar](max) NOT NULL,
	[TaxAddress] [nvarchar](max) NOT NULL,
	[BirthCity] [nvarchar](max) NOT NULL,
	[PhoneNumber] [nvarchar](30) NOT NULL,
	[Role] [nvarchar](max) NOT NULL,
	[Email1] [nvarchar](30) NOT NULL,
	[Email2] [nvarchar](30) NOT NULL,
	[Status] [nvarchar](max) NOT NULL,
	[Locked] [bit] NOT NULL,
 CONSTRAINT [PK_Persons] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ADD SENSITIVITY CLASSIFICATION TO [dbo].[Persons].[FirstName] WITH (label = 'Confidential - GDPR', label_id = '989adc05-3f3f-0588-a635-f475b994915b', information_type = 'Name', information_type_id = '57845286-7598-22f5-9659-15b24aeb125e');
GO
ADD SENSITIVITY CLASSIFICATION TO [dbo].[Persons].[LastName] WITH (label = 'Confidential - GDPR', label_id = '989adc05-3f3f-0588-a635-f475b994915b', information_type = 'Name', information_type_id = '57845286-7598-22f5-9659-15b24aeb125e');
GO
ADD SENSITIVITY CLASSIFICATION TO [dbo].[Persons].[BirthDate] WITH (label = 'Confidential - GDPR', label_id = '989adc05-3f3f-0588-a635-f475b994915b', information_type = 'Date Of Birth', information_type_id = '3de7cc52-710d-4e96-7e20-4d5188d2590c');
GO
ADD SENSITIVITY CLASSIFICATION TO [dbo].[Persons].[PostalAddress] WITH (label = 'Confidential', label_id = '331f0b13-76b5-2f1b-a77b-def5a73c73c2', information_type = 'Contact Info', information_type_id = '5c503e21-22c6-81fa-620b-f369b8ec38d1');
GO
ADD SENSITIVITY CLASSIFICATION TO [dbo].[Persons].[TaxAddress] WITH (label = 'Confidential', label_id = '331f0b13-76b5-2f1b-a77b-def5a73c73c2', information_type = 'Contact Info', information_type_id = '5c503e21-22c6-81fa-620b-f369b8ec38d1');
GO
/****** Object:  Table [dbo].[Products]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Products](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ProductCode] [nvarchar](max) NOT NULL,
	[ProductName] [nvarchar](max) NOT NULL,
	[CreatedDate] [datetime2](7) NULL,
	[UpdatedDate] [datetime2](7) NULL,
	[ContractCount] [int] NOT NULL,
	[InsurerId] [int] NULL,
	[Locked] [bit] NOT NULL,
 CONSTRAINT [PK_Products] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[QRTZ_BLOB_TRIGGERS]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[QRTZ_BLOB_TRIGGERS](
	[SCHED_NAME] [nvarchar](120) NOT NULL,
	[TRIGGER_NAME] [nvarchar](200) NOT NULL,
	[TRIGGER_GROUP] [nvarchar](200) NOT NULL,
	[BLOB_DATA] [varbinary](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[SCHED_NAME] ASC,
	[TRIGGER_NAME] ASC,
	[TRIGGER_GROUP] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[QRTZ_CALENDARS]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[QRTZ_CALENDARS](
	[SCHED_NAME] [nvarchar](120) NOT NULL,
	[CALENDAR_NAME] [nvarchar](200) NOT NULL,
	[CALENDAR] [varbinary](max) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[SCHED_NAME] ASC,
	[CALENDAR_NAME] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[QRTZ_CRON_TRIGGERS]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[QRTZ_CRON_TRIGGERS](
	[SCHED_NAME] [nvarchar](120) NOT NULL,
	[TRIGGER_NAME] [nvarchar](200) NOT NULL,
	[TRIGGER_GROUP] [nvarchar](200) NOT NULL,
	[CRON_EXPRESSION] [nvarchar](120) NOT NULL,
	[TIME_ZONE_ID] [nvarchar](80) NULL,
PRIMARY KEY CLUSTERED 
(
	[SCHED_NAME] ASC,
	[TRIGGER_NAME] ASC,
	[TRIGGER_GROUP] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[QRTZ_FIRED_TRIGGERS]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[QRTZ_FIRED_TRIGGERS](
	[SCHED_NAME] [nvarchar](120) NOT NULL,
	[ENTRY_ID] [nvarchar](95) NOT NULL,
	[TRIGGER_NAME] [nvarchar](200) NOT NULL,
	[TRIGGER_GROUP] [nvarchar](200) NOT NULL,
	[INSTANCE_NAME] [nvarchar](200) NOT NULL,
	[FIRED_TIME] [bigint] NOT NULL,
	[SCHED_TIME] [bigint] NOT NULL,
	[PRIORITY] [int] NOT NULL,
	[STATE] [nvarchar](16) NOT NULL,
	[JOB_NAME] [nvarchar](200) NULL,
	[JOB_GROUP] [nvarchar](200) NULL,
	[IS_NONCONCURRENT] [bit] NULL,
	[REQUESTS_RECOVERY] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[SCHED_NAME] ASC,
	[ENTRY_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[QRTZ_JOB_DETAILS]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[QRTZ_JOB_DETAILS](
	[SCHED_NAME] [nvarchar](120) NOT NULL,
	[JOB_NAME] [nvarchar](200) NOT NULL,
	[JOB_GROUP] [nvarchar](200) NOT NULL,
	[DESCRIPTION] [nvarchar](250) NULL,
	[JOB_CLASS_NAME] [nvarchar](250) NOT NULL,
	[IS_DURABLE] [bit] NOT NULL,
	[IS_NONCONCURRENT] [bit] NOT NULL,
	[IS_UPDATE_DATA] [bit] NOT NULL,
	[REQUESTS_RECOVERY] [bit] NOT NULL,
	[JOB_DATA] [varbinary](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[SCHED_NAME] ASC,
	[JOB_NAME] ASC,
	[JOB_GROUP] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[QRTZ_LOCKS]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[QRTZ_LOCKS](
	[SCHED_NAME] [nvarchar](120) NOT NULL,
	[LOCK_NAME] [nvarchar](40) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[SCHED_NAME] ASC,
	[LOCK_NAME] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[QRTZ_PAUSED_TRIGGER_GRPS]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[QRTZ_PAUSED_TRIGGER_GRPS](
	[SCHED_NAME] [nvarchar](120) NOT NULL,
	[TRIGGER_GROUP] [nvarchar](200) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[SCHED_NAME] ASC,
	[TRIGGER_GROUP] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[QRTZ_SCHEDULER_STATE]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[QRTZ_SCHEDULER_STATE](
	[SCHED_NAME] [nvarchar](120) NOT NULL,
	[INSTANCE_NAME] [nvarchar](200) NOT NULL,
	[LAST_CHECKIN_TIME] [bigint] NOT NULL,
	[CHECKIN_INTERVAL] [bigint] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[SCHED_NAME] ASC,
	[INSTANCE_NAME] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[QRTZ_SIMPLE_TRIGGERS]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[QRTZ_SIMPLE_TRIGGERS](
	[SCHED_NAME] [nvarchar](120) NOT NULL,
	[TRIGGER_NAME] [nvarchar](200) NOT NULL,
	[TRIGGER_GROUP] [nvarchar](200) NOT NULL,
	[REPEAT_COUNT] [bigint] NOT NULL,
	[REPEAT_INTERVAL] [bigint] NOT NULL,
	[TIMES_TRIGGERED] [bigint] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[SCHED_NAME] ASC,
	[TRIGGER_NAME] ASC,
	[TRIGGER_GROUP] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[QRTZ_SIMPROP_TRIGGERS]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[QRTZ_SIMPROP_TRIGGERS](
	[SCHED_NAME] [nvarchar](120) NOT NULL,
	[TRIGGER_NAME] [nvarchar](200) NOT NULL,
	[TRIGGER_GROUP] [nvarchar](200) NOT NULL,
	[STR_PROP_1] [nvarchar](512) NULL,
	[STR_PROP_2] [nvarchar](512) NULL,
	[STR_PROP_3] [nvarchar](512) NULL,
	[INT_PROP_1] [int] NULL,
	[INT_PROP_2] [int] NULL,
	[LONG_PROP_1] [bigint] NULL,
	[LONG_PROP_2] [bigint] NULL,
	[DEC_PROP_1] [numeric](13, 4) NULL,
	[DEC_PROP_2] [numeric](13, 4) NULL,
	[BOOL_PROP_1] [bit] NULL,
	[BOOL_PROP_2] [bit] NULL,
	[TIME_ZONE_ID] [nvarchar](80) NULL,
PRIMARY KEY CLUSTERED 
(
	[SCHED_NAME] ASC,
	[TRIGGER_NAME] ASC,
	[TRIGGER_GROUP] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[QRTZ_TRIGGERS]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[QRTZ_TRIGGERS](
	[SCHED_NAME] [nvarchar](120) NOT NULL,
	[TRIGGER_NAME] [nvarchar](200) NOT NULL,
	[TRIGGER_GROUP] [nvarchar](200) NOT NULL,
	[JOB_NAME] [nvarchar](200) NOT NULL,
	[JOB_GROUP] [nvarchar](200) NOT NULL,
	[DESCRIPTION] [nvarchar](250) NULL,
	[NEXT_FIRE_TIME] [bigint] NULL,
	[PREV_FIRE_TIME] [bigint] NULL,
	[PRIORITY] [int] NULL,
	[TRIGGER_STATE] [nvarchar](16) NOT NULL,
	[TRIGGER_TYPE] [nvarchar](8) NOT NULL,
	[START_TIME] [bigint] NOT NULL,
	[END_TIME] [bigint] NULL,
	[CALENDAR_NAME] [nvarchar](200) NULL,
	[MISFIRE_INSTR] [smallint] NULL,
	[JOB_DATA] [varbinary](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[SCHED_NAME] ASC,
	[TRIGGER_NAME] ASC,
	[TRIGGER_GROUP] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[RolePermissions]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RolePermissions](
	[RoleId] [int] NOT NULL,
	[PermissionId] [int] NOT NULL,
 CONSTRAINT [PK_RolePermissions] PRIMARY KEY CLUSTERED 
(
	[RoleId] ASC,
	[PermissionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Roles]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Roles](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[RoleCode] [nvarchar](50) NOT NULL,
	[Description] [nvarchar](max) NOT NULL,
	[RoleName] [nvarchar](max) NOT NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[UpdatedDate] [datetime2](7) NULL,
 CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ShareClasses]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ShareClasses](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FinancialSupportId] [int] NOT NULL,
	[Code] [nvarchar](20) NOT NULL,
	[ISIN] [nvarchar](12) NOT NULL,
	[Currency] [nvarchar](10) NOT NULL,
	[LaunchDate] [datetime2](7) NULL,
	[DistributionPolicy] [nvarchar](30) NOT NULL,
	[Category] [nvarchar](30) NOT NULL,
	[EntryFee] [decimal](18, 5) NOT NULL,
	[ExitFee] [decimal](18, 5) NOT NULL,
	[ManagementFee] [decimal](18, 5) NOT NULL,
	[OngoingCharges] [decimal](18, 5) NOT NULL,
	[CountryOfRegistration] [nvarchar](10) NOT NULL,
 CONSTRAINT [PK_ShareClasses] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SupportDistributions]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SupportDistributions](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FinancialSupportId] [int] NOT NULL,
	[EntryFee] [decimal](18, 5) NOT NULL,
	[ExitFee] [decimal](18, 5) NOT NULL,
	[OngoingCharges] [decimal](18, 5) NOT NULL,
	[HasRetrocession] [bit] NOT NULL,
	[DistributionFrequency] [nvarchar](max) NULL,
	[LastDistributionDate] [datetime2](7) NULL,
	[AverageRetrocessionRate] [decimal](18, 5) NULL,
	[IsCleanShare] [bit] NULL,
	[DistributionRegion] [nvarchar](max) NULL,
 CONSTRAINT [PK_SupportDistributions] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SupportDocuments]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SupportDocuments](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FinancialSupportId] [int] NOT NULL,
	[DocumentType] [nvarchar](100) NOT NULL,
	[Url] [nvarchar](max) NOT NULL,
	[PublicationDate] [datetime2](7) NULL,
 CONSTRAINT [PK_SupportDocuments] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SupportFeeDetails]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SupportFeeDetails](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FinancialSupportId] [int] NOT NULL,
	[FeeType] [nvarchar](100) NOT NULL,
	[Rate] [decimal](10, 4) NOT NULL,
	[Conditions] [nvarchar](500) NULL,
	[Currency] [nvarchar](3) NULL,
 CONSTRAINT [PK_SupportFeeDetails] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SupportHistoricalData]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SupportHistoricalData](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FinancialSupportId] [int] NOT NULL,
	[Date] [datetime2](7) NOT NULL,
	[Nav] [decimal](18, 4) NULL,
	[PerformanceYTD] [decimal](18, 5) NULL,
	[AUM] [decimal](18, 5) NULL,
	[Volatility1Y] [decimal](18, 5) NULL,
	[Close] [decimal](18, 5) NULL,
	[High] [decimal](18, 5) NULL,
	[Low] [decimal](18, 5) NULL,
	[Open] [decimal](18, 5) NULL,
	[Volume] [bigint] NULL,
 CONSTRAINT [PK_SupportHistoricalData] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SupportLookthroughAssets]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SupportLookthroughAssets](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FinancialSupportId] [int] NOT NULL,
	[AssetName] [nvarchar](200) NOT NULL,
	[ISIN] [nvarchar](12) NOT NULL,
	[AssetClass] [nvarchar](100) NOT NULL,
	[Weight] [decimal](5, 2) NOT NULL,
 CONSTRAINT [PK_SupportLookthroughAssets] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SupportPortfolioLinks]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SupportPortfolioLinks](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FinancialSupportId] [int] NOT NULL,
	[PortfolioCode] [nvarchar](max) NOT NULL,
	[Strategy] [nvarchar](max) NOT NULL,
	[RiskProfile] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_SupportPortfolioLinks] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SupportRegulations]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SupportRegulations](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FinancialSupportId] [int] NOT NULL,
	[IsSFDRApplicable] [bit] NOT NULL,
	[SFDRLevel] [nvarchar](max) NULL,
	[ESGScoreProvider] [nvarchar](max) NULL,
	[MifidCategory] [nvarchar](max) NULL,
	[Ecolabel] [nvarchar](max) NULL,
	[HasPrincipalAdverseImpacts] [bit] NULL,
	[PAIIndicators] [nvarchar](max) NULL,
	[KIIDDocumentUrl] [nvarchar](max) NULL,
	[LastKIIDUpdate] [datetime2](7) NULL,
	[ProspectusUrl] [nvarchar](max) NULL,
 CONSTRAINT [PK_SupportRegulations] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SupportRiskProfiles]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SupportRiskProfiles](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FinancialSupportId] [int] NOT NULL,
	[SRRI] [int] NOT NULL,
	[RiskDescription] [nvarchar](max) NULL,
	[Volatility3Y] [decimal](18, 5) NULL,
	[MorningstarRiskRating] [nvarchar](max) NULL,
	[RiskRegion] [nvarchar](max) NULL,
	[RiskNarrative] [nvarchar](1000) NULL,
 CONSTRAINT [PK_SupportRiskProfiles] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SupportTechnicals]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SupportTechnicals](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FinancialSupportId] [int] NOT NULL,
	[DataSource] [nvarchar](max) NOT NULL,
	[SyncStatus] [nvarchar](max) NOT NULL,
	[LastSyncDate] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_SupportTechnicals] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SupportValuations]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SupportValuations](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FinancialSupportId] [int] NOT NULL,
	[ValuationDate] [datetime2](7) NOT NULL,
	[Nav] [decimal](18, 5) NOT NULL,
	[Source] [nvarchar](50) NOT NULL,
	[ValuationCurrency] [nvarchar](3) NOT NULL,
	[IsEstimated] [bit] NOT NULL,
	[SourceTimestamp] [datetime2](7) NULL,
	[SourceReferenceId] [nvarchar](max) NULL,
	[PerformanceYTD] [decimal](18, 5) NULL,
	[Performance1Y] [decimal](18, 5) NULL,
	[Volatility1Y] [decimal](18, 5) NULL,
 CONSTRAINT [PK_SupportValuations] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TaxDatas]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TaxDatas](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FinancialSupportId] [int] NOT NULL,
	[IsFATCAEligible] [bit] NOT NULL,
	[IsCRSReportable] [bit] NOT NULL,
	[CountryOfTaxation] [nvarchar](max) NOT NULL,
	[LegalForm] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_TaxDatas] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[UserRoles]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UserRoles](
	[UserId] [int] NOT NULL,
	[RoleId] [int] NOT NULL,
 CONSTRAINT [PK_UserRoles] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Users]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Users](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Username] [nvarchar](100) NOT NULL,
	[PasswordHash] [nvarchar](max) NOT NULL,
	[Email] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[WithdrawalDetails]    Script Date: 14/12/2025 14:23:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[WithdrawalDetails](
	[OperationId] [int] NOT NULL,
	[GrossAmount] [decimal](20, 7) NOT NULL,
	[IsScheduled] [bit] NOT NULL,
	[Frequency] [nvarchar](max) NULL,
 CONSTRAINT [PK_WithdrawalDetails] PRIMARY KEY CLUSTERED 
(
	[OperationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Index [IX_BeneficiaryClausePersons_PersonId]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_BeneficiaryClausePersons_PersonId] ON [dbo].[BeneficiaryClausePersons]
(
	[PersonId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_BeneficiaryClauses_ContractId]    Script Date: 14/12/2025 14:23:27 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_BeneficiaryClauses_ContractId] ON [dbo].[BeneficiaryClauses]
(
	[ContractId] ASC
)
WHERE ([ContractId] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ClientTypeCompliances_FinancialSupportId]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_ClientTypeCompliances_FinancialSupportId] ON [dbo].[ClientTypeCompliances]
(
	[FinancialSupportId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Compartments_ContractId_IsDefault]    Script Date: 14/12/2025 14:23:27 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_Compartments_ContractId_IsDefault] ON [dbo].[Compartments]
(
	[ContractId] ASC,
	[IsDefault] ASC
)
WHERE ([IsDefault]=(1))
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ContractInsuredPersons_PersonId]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_ContractInsuredPersons_PersonId] ON [dbo].[ContractInsuredPersons]
(
	[PersonId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ContractOptions_ContractId]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_ContractOptions_ContractId] ON [dbo].[ContractOptions]
(
	[ContractId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ContractOptions_ContractOptionTypeId]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_ContractOptions_ContractOptionTypeId] ON [dbo].[ContractOptions]
(
	[ContractOptionTypeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_ContractOptionTypes_Code]    Script Date: 14/12/2025 14:23:27 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_ContractOptionTypes_Code] ON [dbo].[ContractOptionTypes]
(
	[Code] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Contracts_PersonId]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_Contracts_PersonId] ON [dbo].[Contracts]
(
	[PersonId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Contracts_ProductId]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_Contracts_ProductId] ON [dbo].[Contracts]
(
	[ProductId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ContractSupportHoldings_ContractId_SupportId]    Script Date: 14/12/2025 14:23:27 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_ContractSupportHoldings_ContractId_SupportId] ON [dbo].[ContractSupportHoldings]
(
	[ContractId] ASC,
	[SupportId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ContractSupportHoldings_SupportId]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_ContractSupportHoldings_SupportId] ON [dbo].[ContractSupportHoldings]
(
	[SupportId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ContractValuations_ContractId]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_ContractValuations_ContractId] ON [dbo].[ContractValuations]
(
	[ContractId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_DistributionChannels_FinancialSupportId]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_DistributionChannels_FinancialSupportId] ON [dbo].[DistributionChannels]
(
	[FinancialSupportId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Documents_ContractId]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_Documents_ContractId] ON [dbo].[Documents]
(
	[ContractId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ESGDetails_FinancialSupportId]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_ESGDetails_FinancialSupportId] ON [dbo].[ESGDetails]
(
	[FinancialSupportId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_FinancialSupportAllocations_CompartmentId]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_FinancialSupportAllocations_CompartmentId] ON [dbo].[FinancialSupportAllocations]
(
	[CompartmentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_FinancialSupportAllocations_SupportId]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_FinancialSupportAllocations_SupportId] ON [dbo].[FinancialSupportAllocations]
(
	[SupportId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UX_FSA_Contract_Compartment_Support]    Script Date: 14/12/2025 14:23:27 ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_FSA_Contract_Compartment_Support] ON [dbo].[FinancialSupportAllocations]
(
	[ContractId] ASC,
	[CompartmentId] ASC,
	[SupportId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_FinancialSupports_ISIN]    Script Date: 14/12/2025 14:23:27 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_FinancialSupports_ISIN] ON [dbo].[FinancialSupports]
(
	[ISIN] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_FundLifeCycles_FinancialSupportId]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_FundLifeCycles_FinancialSupportId] ON [dbo].[FundLifeCycles]
(
	[FinancialSupportId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_FundScenarios_FinancialSupportId]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_FundScenarios_FinancialSupportId] ON [dbo].[FundScenarios]
(
	[FinancialSupportId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_MarketingTargets_FinancialSupportId]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_MarketingTargets_FinancialSupportId] ON [dbo].[MarketingTargets]
(
	[FinancialSupportId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_MultilingualDocuments_FinancialSupportId]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_MultilingualDocuments_FinancialSupportId] ON [dbo].[MultilingualDocuments]
(
	[FinancialSupportId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Operations_CompartmentId]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_Operations_CompartmentId] ON [dbo].[Operations]
(
	[CompartmentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Operations_ContractId]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_Operations_ContractId] ON [dbo].[Operations]
(
	[ContractId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_OperationSupportAllocations_CompartmentId]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_OperationSupportAllocations_CompartmentId] ON [dbo].[OperationSupportAllocations]
(
	[CompartmentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_OperationSupportAllocations_SupportId]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_OperationSupportAllocations_SupportId] ON [dbo].[OperationSupportAllocations]
(
	[SupportId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UX_OSA_Operation_Support]    Script Date: 14/12/2025 14:23:27 ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_OSA_Operation_Support] ON [dbo].[OperationSupportAllocations]
(
	[OperationId] ASC,
	[SupportId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Persons_CreatedDate]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_Persons_CreatedDate] ON [dbo].[Persons]
(
	[CreatedDate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Persons_Email1]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_Persons_Email1] ON [dbo].[Persons]
(
	[Email1] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Persons_FirstName]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_Persons_FirstName] ON [dbo].[Persons]
(
	[FirstName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Persons_FirstName_LastName]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_Persons_FirstName_LastName] ON [dbo].[Persons]
(
	[FirstName] ASC,
	[LastName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Persons_LastName]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_Persons_LastName] ON [dbo].[Persons]
(
	[LastName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Persons_PhoneNumber]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_Persons_PhoneNumber] ON [dbo].[Persons]
(
	[PhoneNumber] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Persons_Search]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_Persons_Search] ON [dbo].[Persons]
(
	[FirstName] ASC,
	[LastName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [ui_idPerson]    Script Date: 14/12/2025 14:23:27 ******/
CREATE UNIQUE NONCLUSTERED INDEX [ui_idPerson] ON [dbo].[Persons]
(
	[Id] ASC,
	[CreatedDate] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Products_InsurerId]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_Products_InsurerId] ON [dbo].[Products]
(
	[InsurerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IDX_QRTZ_T_NEXT_FIRE_TIME]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IDX_QRTZ_T_NEXT_FIRE_TIME] ON [dbo].[QRTZ_TRIGGERS]
(
	[SCHED_NAME] ASC,
	[NEXT_FIRE_TIME] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IDX_QRTZ_T_NFT_MISFIRE]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IDX_QRTZ_T_NFT_MISFIRE] ON [dbo].[QRTZ_TRIGGERS]
(
	[SCHED_NAME] ASC,
	[MISFIRE_INSTR] ASC,
	[NEXT_FIRE_TIME] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IDX_QRTZ_T_NFT_ST]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IDX_QRTZ_T_NFT_ST] ON [dbo].[QRTZ_TRIGGERS]
(
	[SCHED_NAME] ASC,
	[NEXT_FIRE_TIME] ASC,
	[TRIGGER_STATE] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IDX_QRTZ_T_NFT_ST_MISFIRE]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IDX_QRTZ_T_NFT_ST_MISFIRE] ON [dbo].[QRTZ_TRIGGERS]
(
	[SCHED_NAME] ASC,
	[MISFIRE_INSTR] ASC,
	[NEXT_FIRE_TIME] ASC,
	[TRIGGER_STATE] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IDX_QRTZ_T_NFT_ST_MISFIRE_GRP]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IDX_QRTZ_T_NFT_ST_MISFIRE_GRP] ON [dbo].[QRTZ_TRIGGERS]
(
	[SCHED_NAME] ASC,
	[MISFIRE_INSTR] ASC,
	[NEXT_FIRE_TIME] ASC,
	[TRIGGER_GROUP] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IDX_QRTZ_T_STATE]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IDX_QRTZ_T_STATE] ON [dbo].[QRTZ_TRIGGERS]
(
	[SCHED_NAME] ASC,
	[TRIGGER_STATE] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_RolePermissions_PermissionId]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_RolePermissions_PermissionId] ON [dbo].[RolePermissions]
(
	[PermissionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ShareClasses_FinancialSupportId]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_ShareClasses_FinancialSupportId] ON [dbo].[ShareClasses]
(
	[FinancialSupportId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_SupportDistributions_FinancialSupportId]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_SupportDistributions_FinancialSupportId] ON [dbo].[SupportDistributions]
(
	[FinancialSupportId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_SupportDocuments_FinancialSupportId]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_SupportDocuments_FinancialSupportId] ON [dbo].[SupportDocuments]
(
	[FinancialSupportId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_SupportFeeDetails_FinancialSupportId]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_SupportFeeDetails_FinancialSupportId] ON [dbo].[SupportFeeDetails]
(
	[FinancialSupportId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_SupportHistoricalData_FinancialSupportId]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_SupportHistoricalData_FinancialSupportId] ON [dbo].[SupportHistoricalData]
(
	[FinancialSupportId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_SupportLookthroughAssets_FinancialSupportId]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_SupportLookthroughAssets_FinancialSupportId] ON [dbo].[SupportLookthroughAssets]
(
	[FinancialSupportId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_SupportPortfolioLinks_FinancialSupportId]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_SupportPortfolioLinks_FinancialSupportId] ON [dbo].[SupportPortfolioLinks]
(
	[FinancialSupportId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_SupportRegulations_FinancialSupportId]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_SupportRegulations_FinancialSupportId] ON [dbo].[SupportRegulations]
(
	[FinancialSupportId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_SupportRiskProfiles_FinancialSupportId]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_SupportRiskProfiles_FinancialSupportId] ON [dbo].[SupportRiskProfiles]
(
	[FinancialSupportId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_SupportTechnicals_FinancialSupportId]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_SupportTechnicals_FinancialSupportId] ON [dbo].[SupportTechnicals]
(
	[FinancialSupportId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_SupportValuations_FinancialSupportId]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_SupportValuations_FinancialSupportId] ON [dbo].[SupportValuations]
(
	[FinancialSupportId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_TaxDatas_FinancialSupportId]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_TaxDatas_FinancialSupportId] ON [dbo].[TaxDatas]
(
	[FinancialSupportId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_UserRoles_RoleId]    Script Date: 14/12/2025 14:23:27 ******/
CREATE NONCLUSTERED INDEX [IX_UserRoles_RoleId] ON [dbo].[UserRoles]
(
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[BeneficiaryClausePersons] ADD  DEFAULT ('0001-01-01T00:00:00.0000000') FOR [CreatedDate]
GO
ALTER TABLE [dbo].[BeneficiaryClausePersons] ADD  DEFAULT ('0001-01-01T00:00:00.0000000') FOR [UpdatedDate]
GO
ALTER TABLE [dbo].[BeneficiaryClauses] ADD  DEFAULT (CONVERT([bit],(0))) FOR [Locked]
GO
ALTER TABLE [dbo].[Brands] ADD  DEFAULT (N'') FOR [BrandCode]
GO
ALTER TABLE [dbo].[Brands] ADD  DEFAULT (N'') FOR [City]
GO
ALTER TABLE [dbo].[Brands] ADD  DEFAULT (N'') FOR [ContactEmail]
GO
ALTER TABLE [dbo].[Brands] ADD  DEFAULT (N'') FOR [Country]
GO
ALTER TABLE [dbo].[Brands] ADD  DEFAULT (N'') FOR [Description]
GO
ALTER TABLE [dbo].[Brands] ADD  DEFAULT (N'') FOR [FacebookUrl]
GO
ALTER TABLE [dbo].[Brands] ADD  DEFAULT (N'') FOR [Founder]
GO
ALTER TABLE [dbo].[Brands] ADD  DEFAULT (N'') FOR [Industry]
GO
ALTER TABLE [dbo].[Brands] ADD  DEFAULT (N'') FOR [InstagramUrl]
GO
ALTER TABLE [dbo].[Brands] ADD  DEFAULT (CONVERT([bit],(0))) FOR [IsActive]
GO
ALTER TABLE [dbo].[Brands] ADD  DEFAULT (N'') FOR [LinkedInUrl]
GO
ALTER TABLE [dbo].[Brands] ADD  DEFAULT (N'') FOR [LogoUrl]
GO
ALTER TABLE [dbo].[Brands] ADD  DEFAULT (N'') FOR [MainColor]
GO
ALTER TABLE [dbo].[Brands] ADD  DEFAULT (N'') FOR [Notes]
GO
ALTER TABLE [dbo].[Brands] ADD  DEFAULT (N'') FOR [ParentGroup]
GO
ALTER TABLE [dbo].[Brands] ADD  DEFAULT (N'') FOR [Slogan]
GO
ALTER TABLE [dbo].[Brands] ADD  DEFAULT (N'') FOR [Website]
GO
ALTER TABLE [dbo].[Brands] ADD  DEFAULT (CONVERT([bit],(0))) FOR [Locked]
GO
ALTER TABLE [dbo].[Compartments] ADD  DEFAULT ('0001-01-01T00:00:00.0000000') FOR [UpdatedDate]
GO
ALTER TABLE [dbo].[Compartments] ADD  DEFAULT (CONVERT([bit],(0))) FOR [IsDefault]
GO
ALTER TABLE [dbo].[ContractOptions] ADD  DEFAULT ((0)) FOR [ContractOptionTypeId]
GO
ALTER TABLE [dbo].[Contracts] ADD  DEFAULT (N'') FOR [ContractNumber]
GO
ALTER TABLE [dbo].[Contracts] ADD  DEFAULT (getdate()) FOR [CreatedDate]
GO
ALTER TABLE [dbo].[Contracts] ADD  DEFAULT ('1970-01-01 00:00:00.000') FOR [UpdatedDate]
GO
ALTER TABLE [dbo].[Contracts] ADD  DEFAULT (N'') FOR [PostalAddress]
GO
ALTER TABLE [dbo].[Contracts] ADD  DEFAULT (N'') FOR [TaxAddress]
GO
ALTER TABLE [dbo].[Contracts] ADD  DEFAULT (N'') FOR [ContractType]
GO
ALTER TABLE [dbo].[Contracts] ADD  DEFAULT (N'') FOR [Currency]
GO
ALTER TABLE [dbo].[Contracts] ADD  DEFAULT ('0001-01-01T00:00:00.0000000') FOR [DateEffect]
GO
ALTER TABLE [dbo].[Contracts] ADD  DEFAULT (CONVERT([bit],(0))) FOR [HasAlert]
GO
ALTER TABLE [dbo].[Contracts] ADD  DEFAULT (CONVERT([bit],(0))) FOR [IsJointContract]
GO
ALTER TABLE [dbo].[Contracts] ADD  DEFAULT (N'') FOR [PaymentMode]
GO
ALTER TABLE [dbo].[Contracts] ADD  DEFAULT (N'') FOR [Status]
GO
ALTER TABLE [dbo].[Contracts] ADD  DEFAULT (CONVERT([bit],(0))) FOR [Locked]
GO
ALTER TABLE [dbo].[FinancialSupportAllocations] ADD  DEFAULT ((0)) FOR [CompartmentId]
GO
ALTER TABLE [dbo].[FinancialSupportAllocations] ADD  DEFAULT ('0001-01-01T00:00:00.0000000') FOR [CreatedDate]
GO
ALTER TABLE [dbo].[FinancialSupportAllocations] ADD  DEFAULT ('0001-01-01T00:00:00.0000000') FOR [UpdatedDate]
GO
ALTER TABLE [dbo].[FinancialSupportAllocations] ADD  DEFAULT ((0.0)) FOR [CurrentShares]
GO
ALTER TABLE [dbo].[FinancialSupportAllocations] ADD  DEFAULT ((0.0)) FOR [InvestedAmount]
GO
ALTER TABLE [dbo].[Insurers] ADD  DEFAULT (CONVERT([bit],(0))) FOR [Locked]
GO
ALTER TABLE [dbo].[OperationSupportAllocations] ADD  DEFAULT ((0)) FOR [CompartmentId]
GO
ALTER TABLE [dbo].[Permissions] ADD  DEFAULT (N'') FOR [Description]
GO
ALTER TABLE [dbo].[Permissions] ADD  DEFAULT (N'') FOR [PermissionCode]
GO
ALTER TABLE [dbo].[Persons] ADD  DEFAULT (N'') FOR [BirthCountry]
GO
ALTER TABLE [dbo].[Persons] ADD  DEFAULT (getdate()) FOR [CreatedDate]
GO
ALTER TABLE [dbo].[Persons] ADD  DEFAULT ('1970-01-01 00:00:00.000') FOR [UpdatedDate]
GO
ALTER TABLE [dbo].[Persons] ADD  DEFAULT (N'') FOR [PostalAddress]
GO
ALTER TABLE [dbo].[Persons] ADD  DEFAULT (N'') FOR [TaxAddress]
GO
ALTER TABLE [dbo].[Persons] ADD  DEFAULT (N'') FOR [BirthCity]
GO
ALTER TABLE [dbo].[Persons] ADD  DEFAULT (N'') FOR [Role]
GO
ALTER TABLE [dbo].[Persons] ADD  DEFAULT (N'') FOR [Status]
GO
ALTER TABLE [dbo].[Persons] ADD  DEFAULT (CONVERT([bit],(0))) FOR [Locked]
GO
ALTER TABLE [dbo].[Products] ADD  DEFAULT (getdate()) FOR [CreatedDate]
GO
ALTER TABLE [dbo].[Products] ADD  DEFAULT ('1970-01-01 00:00:00.000') FOR [UpdatedDate]
GO
ALTER TABLE [dbo].[Products] ADD  DEFAULT ((0)) FOR [ContractCount]
GO
ALTER TABLE [dbo].[Products] ADD  DEFAULT (CONVERT([bit],(0))) FOR [Locked]
GO
ALTER TABLE [dbo].[Roles] ADD  DEFAULT (N'') FOR [Description]
GO
ALTER TABLE [dbo].[Roles] ADD  DEFAULT (N'') FOR [RoleName]
GO
ALTER TABLE [dbo].[Roles] ADD  DEFAULT ('0001-01-01T00:00:00.0000000') FOR [CreatedDate]
GO
ALTER TABLE [dbo].[AdvanceDetails]  WITH CHECK ADD  CONSTRAINT [FK_AdvanceDetails_Operations_OperationId] FOREIGN KEY([OperationId])
REFERENCES [dbo].[Operations] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AdvanceDetails] CHECK CONSTRAINT [FK_AdvanceDetails_Operations_OperationId]
GO
ALTER TABLE [dbo].[ArbitrageDetails]  WITH CHECK ADD  CONSTRAINT [FK_ArbitrageDetails_Operations_OperationId] FOREIGN KEY([OperationId])
REFERENCES [dbo].[Operations] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ArbitrageDetails] CHECK CONSTRAINT [FK_ArbitrageDetails_Operations_OperationId]
GO
ALTER TABLE [dbo].[BeneficiaryClausePersons]  WITH CHECK ADD  CONSTRAINT [FK_BeneficiaryClausePersons_BeneficiaryClauses_ClauseId] FOREIGN KEY([ClauseId])
REFERENCES [dbo].[BeneficiaryClauses] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[BeneficiaryClausePersons] CHECK CONSTRAINT [FK_BeneficiaryClausePersons_BeneficiaryClauses_ClauseId]
GO
ALTER TABLE [dbo].[BeneficiaryClausePersons]  WITH CHECK ADD  CONSTRAINT [FK_BeneficiaryClausePersons_Persons_PersonId] FOREIGN KEY([PersonId])
REFERENCES [dbo].[Persons] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[BeneficiaryClausePersons] CHECK CONSTRAINT [FK_BeneficiaryClausePersons_Persons_PersonId]
GO
ALTER TABLE [dbo].[BeneficiaryClauses]  WITH CHECK ADD  CONSTRAINT [FK_BeneficiaryClauses_Contracts_ContractId] FOREIGN KEY([ContractId])
REFERENCES [dbo].[Contracts] ([Id])
GO
ALTER TABLE [dbo].[BeneficiaryClauses] CHECK CONSTRAINT [FK_BeneficiaryClauses_Contracts_ContractId]
GO
ALTER TABLE [dbo].[ClientTypeCompliances]  WITH CHECK ADD  CONSTRAINT [FK_ClientTypeCompliances_FinancialSupports_FinancialSupportId] FOREIGN KEY([FinancialSupportId])
REFERENCES [dbo].[FinancialSupports] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ClientTypeCompliances] CHECK CONSTRAINT [FK_ClientTypeCompliances_FinancialSupports_FinancialSupportId]
GO
ALTER TABLE [dbo].[Compartments]  WITH CHECK ADD  CONSTRAINT [FK_Compartments_Contracts_ContractId] FOREIGN KEY([ContractId])
REFERENCES [dbo].[Contracts] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Compartments] CHECK CONSTRAINT [FK_Compartments_Contracts_ContractId]
GO
ALTER TABLE [dbo].[ContractInsuredPersons]  WITH CHECK ADD  CONSTRAINT [FK_ContractInsuredPersons_Contracts_ContractId] FOREIGN KEY([ContractId])
REFERENCES [dbo].[Contracts] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ContractInsuredPersons] CHECK CONSTRAINT [FK_ContractInsuredPersons_Contracts_ContractId]
GO
ALTER TABLE [dbo].[ContractInsuredPersons]  WITH CHECK ADD  CONSTRAINT [FK_ContractInsuredPersons_Persons_PersonId] FOREIGN KEY([PersonId])
REFERENCES [dbo].[Persons] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ContractInsuredPersons] CHECK CONSTRAINT [FK_ContractInsuredPersons_Persons_PersonId]
GO
ALTER TABLE [dbo].[ContractOptions]  WITH CHECK ADD  CONSTRAINT [FK_ContractOptions_ContractOptionTypes_ContractOptionTypeId] FOREIGN KEY([ContractOptionTypeId])
REFERENCES [dbo].[ContractOptionTypes] ([Id])
GO
ALTER TABLE [dbo].[ContractOptions] CHECK CONSTRAINT [FK_ContractOptions_ContractOptionTypes_ContractOptionTypeId]
GO
ALTER TABLE [dbo].[ContractOptions]  WITH CHECK ADD  CONSTRAINT [FK_ContractOptions_Contracts_ContractId] FOREIGN KEY([ContractId])
REFERENCES [dbo].[Contracts] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ContractOptions] CHECK CONSTRAINT [FK_ContractOptions_Contracts_ContractId]
GO
ALTER TABLE [dbo].[Contracts]  WITH CHECK ADD  CONSTRAINT [FK_Contracts_Persons_PersonId] FOREIGN KEY([PersonId])
REFERENCES [dbo].[Persons] ([Id])
GO
ALTER TABLE [dbo].[Contracts] CHECK CONSTRAINT [FK_Contracts_Persons_PersonId]
GO
ALTER TABLE [dbo].[Contracts]  WITH CHECK ADD  CONSTRAINT [FK_Contracts_Products_ProductId] FOREIGN KEY([ProductId])
REFERENCES [dbo].[Products] ([Id])
GO
ALTER TABLE [dbo].[Contracts] CHECK CONSTRAINT [FK_Contracts_Products_ProductId]
GO
ALTER TABLE [dbo].[ContractSupportHoldings]  WITH CHECK ADD  CONSTRAINT [FK_ContractSupportHoldings_Contracts_ContractId] FOREIGN KEY([ContractId])
REFERENCES [dbo].[Contracts] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ContractSupportHoldings] CHECK CONSTRAINT [FK_ContractSupportHoldings_Contracts_ContractId]
GO
ALTER TABLE [dbo].[ContractSupportHoldings]  WITH CHECK ADD  CONSTRAINT [FK_ContractSupportHoldings_FinancialSupports_SupportId] FOREIGN KEY([SupportId])
REFERENCES [dbo].[FinancialSupports] ([Id])
GO
ALTER TABLE [dbo].[ContractSupportHoldings] CHECK CONSTRAINT [FK_ContractSupportHoldings_FinancialSupports_SupportId]
GO
ALTER TABLE [dbo].[ContractValuations]  WITH CHECK ADD  CONSTRAINT [FK_ContractValuations_Contracts_ContractId] FOREIGN KEY([ContractId])
REFERENCES [dbo].[Contracts] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ContractValuations] CHECK CONSTRAINT [FK_ContractValuations_Contracts_ContractId]
GO
ALTER TABLE [dbo].[DistributionChannels]  WITH CHECK ADD  CONSTRAINT [FK_DistributionChannels_FinancialSupports_FinancialSupportId] FOREIGN KEY([FinancialSupportId])
REFERENCES [dbo].[FinancialSupports] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[DistributionChannels] CHECK CONSTRAINT [FK_DistributionChannels_FinancialSupports_FinancialSupportId]
GO
ALTER TABLE [dbo].[Documents]  WITH CHECK ADD  CONSTRAINT [FK_Documents_Contracts_ContractId] FOREIGN KEY([ContractId])
REFERENCES [dbo].[Contracts] ([Id])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[Documents] CHECK CONSTRAINT [FK_Documents_Contracts_ContractId]
GO
ALTER TABLE [dbo].[ESGDetails]  WITH CHECK ADD  CONSTRAINT [FK_ESGDetails_FinancialSupports_FinancialSupportId] FOREIGN KEY([FinancialSupportId])
REFERENCES [dbo].[FinancialSupports] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ESGDetails] CHECK CONSTRAINT [FK_ESGDetails_FinancialSupports_FinancialSupportId]
GO
ALTER TABLE [dbo].[FinancialSupportAllocations]  WITH CHECK ADD  CONSTRAINT [FK_FinancialSupportAllocations_Compartments_CompartmentId] FOREIGN KEY([CompartmentId])
REFERENCES [dbo].[Compartments] ([Id])
GO
ALTER TABLE [dbo].[FinancialSupportAllocations] CHECK CONSTRAINT [FK_FinancialSupportAllocations_Compartments_CompartmentId]
GO
ALTER TABLE [dbo].[FinancialSupportAllocations]  WITH CHECK ADD  CONSTRAINT [FK_FinancialSupportAllocations_Contracts_ContractId] FOREIGN KEY([ContractId])
REFERENCES [dbo].[Contracts] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[FinancialSupportAllocations] CHECK CONSTRAINT [FK_FinancialSupportAllocations_Contracts_ContractId]
GO
ALTER TABLE [dbo].[FinancialSupportAllocations]  WITH CHECK ADD  CONSTRAINT [FK_FinancialSupportAllocations_FinancialSupports_SupportId] FOREIGN KEY([SupportId])
REFERENCES [dbo].[FinancialSupports] ([Id])
GO
ALTER TABLE [dbo].[FinancialSupportAllocations] CHECK CONSTRAINT [FK_FinancialSupportAllocations_FinancialSupports_SupportId]
GO
ALTER TABLE [dbo].[FundLifeCycles]  WITH CHECK ADD  CONSTRAINT [FK_FundLifeCycles_FinancialSupports_FinancialSupportId] FOREIGN KEY([FinancialSupportId])
REFERENCES [dbo].[FinancialSupports] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[FundLifeCycles] CHECK CONSTRAINT [FK_FundLifeCycles_FinancialSupports_FinancialSupportId]
GO
ALTER TABLE [dbo].[FundScenarios]  WITH CHECK ADD  CONSTRAINT [FK_FundScenarios_FinancialSupports_FinancialSupportId] FOREIGN KEY([FinancialSupportId])
REFERENCES [dbo].[FinancialSupports] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[FundScenarios] CHECK CONSTRAINT [FK_FundScenarios_FinancialSupports_FinancialSupportId]
GO
ALTER TABLE [dbo].[MarketingTargets]  WITH CHECK ADD  CONSTRAINT [FK_MarketingTargets_FinancialSupports_FinancialSupportId] FOREIGN KEY([FinancialSupportId])
REFERENCES [dbo].[FinancialSupports] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[MarketingTargets] CHECK CONSTRAINT [FK_MarketingTargets_FinancialSupports_FinancialSupportId]
GO
ALTER TABLE [dbo].[MultilingualDocuments]  WITH CHECK ADD  CONSTRAINT [FK_MultilingualDocuments_FinancialSupports_FinancialSupportId] FOREIGN KEY([FinancialSupportId])
REFERENCES [dbo].[FinancialSupports] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[MultilingualDocuments] CHECK CONSTRAINT [FK_MultilingualDocuments_FinancialSupports_FinancialSupportId]
GO
ALTER TABLE [dbo].[Operations]  WITH CHECK ADD  CONSTRAINT [FK_Operations_Compartments_CompartmentId] FOREIGN KEY([CompartmentId])
REFERENCES [dbo].[Compartments] ([Id])
GO
ALTER TABLE [dbo].[Operations] CHECK CONSTRAINT [FK_Operations_Compartments_CompartmentId]
GO
ALTER TABLE [dbo].[Operations]  WITH CHECK ADD  CONSTRAINT [FK_Operations_Contracts_ContractId] FOREIGN KEY([ContractId])
REFERENCES [dbo].[Contracts] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Operations] CHECK CONSTRAINT [FK_Operations_Contracts_ContractId]
GO
ALTER TABLE [dbo].[OperationSupportAllocations]  WITH CHECK ADD  CONSTRAINT [FK_OperationSupportAllocations_Compartments_CompartmentId] FOREIGN KEY([CompartmentId])
REFERENCES [dbo].[Compartments] ([Id])
GO
ALTER TABLE [dbo].[OperationSupportAllocations] CHECK CONSTRAINT [FK_OperationSupportAllocations_Compartments_CompartmentId]
GO
ALTER TABLE [dbo].[OperationSupportAllocations]  WITH CHECK ADD  CONSTRAINT [FK_OperationSupportAllocations_FinancialSupports_SupportId] FOREIGN KEY([SupportId])
REFERENCES [dbo].[FinancialSupports] ([Id])
GO
ALTER TABLE [dbo].[OperationSupportAllocations] CHECK CONSTRAINT [FK_OperationSupportAllocations_FinancialSupports_SupportId]
GO
ALTER TABLE [dbo].[OperationSupportAllocations]  WITH CHECK ADD  CONSTRAINT [FK_OperationSupportAllocations_Operations_OperationId] FOREIGN KEY([OperationId])
REFERENCES [dbo].[Operations] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[OperationSupportAllocations] CHECK CONSTRAINT [FK_OperationSupportAllocations_Operations_OperationId]
GO
ALTER TABLE [dbo].[PaymentDetails]  WITH CHECK ADD  CONSTRAINT [FK_PaymentDetails_Operations_OperationId] FOREIGN KEY([OperationId])
REFERENCES [dbo].[Operations] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[PaymentDetails] CHECK CONSTRAINT [FK_PaymentDetails_Operations_OperationId]
GO
ALTER TABLE [dbo].[Products]  WITH CHECK ADD  CONSTRAINT [FK_Products_Insurers_InsurerId] FOREIGN KEY([InsurerId])
REFERENCES [dbo].[Insurers] ([Id])
GO
ALTER TABLE [dbo].[Products] CHECK CONSTRAINT [FK_Products_Insurers_InsurerId]
GO
ALTER TABLE [dbo].[QRTZ_BLOB_TRIGGERS]  WITH CHECK ADD FOREIGN KEY([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP])
REFERENCES [dbo].[QRTZ_TRIGGERS] ([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP])
GO
ALTER TABLE [dbo].[QRTZ_CRON_TRIGGERS]  WITH CHECK ADD FOREIGN KEY([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP])
REFERENCES [dbo].[QRTZ_TRIGGERS] ([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP])
GO
ALTER TABLE [dbo].[QRTZ_SIMPLE_TRIGGERS]  WITH CHECK ADD FOREIGN KEY([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP])
REFERENCES [dbo].[QRTZ_TRIGGERS] ([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP])
GO
ALTER TABLE [dbo].[QRTZ_SIMPROP_TRIGGERS]  WITH CHECK ADD FOREIGN KEY([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP])
REFERENCES [dbo].[QRTZ_TRIGGERS] ([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP])
GO
ALTER TABLE [dbo].[QRTZ_TRIGGERS]  WITH CHECK ADD FOREIGN KEY([SCHED_NAME], [JOB_NAME], [JOB_GROUP])
REFERENCES [dbo].[QRTZ_JOB_DETAILS] ([SCHED_NAME], [JOB_NAME], [JOB_GROUP])
GO
ALTER TABLE [dbo].[RolePermissions]  WITH CHECK ADD  CONSTRAINT [FK_RolePermissions_Permissions_PermissionId] FOREIGN KEY([PermissionId])
REFERENCES [dbo].[Permissions] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[RolePermissions] CHECK CONSTRAINT [FK_RolePermissions_Permissions_PermissionId]
GO
ALTER TABLE [dbo].[RolePermissions]  WITH CHECK ADD  CONSTRAINT [FK_RolePermissions_Roles_RoleId] FOREIGN KEY([RoleId])
REFERENCES [dbo].[Roles] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[RolePermissions] CHECK CONSTRAINT [FK_RolePermissions_Roles_RoleId]
GO
ALTER TABLE [dbo].[ShareClasses]  WITH CHECK ADD  CONSTRAINT [FK_ShareClasses_FinancialSupports_FinancialSupportId] FOREIGN KEY([FinancialSupportId])
REFERENCES [dbo].[FinancialSupports] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ShareClasses] CHECK CONSTRAINT [FK_ShareClasses_FinancialSupports_FinancialSupportId]
GO
ALTER TABLE [dbo].[SupportDistributions]  WITH CHECK ADD  CONSTRAINT [FK_SupportDistributions_FinancialSupports_FinancialSupportId] FOREIGN KEY([FinancialSupportId])
REFERENCES [dbo].[FinancialSupports] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[SupportDistributions] CHECK CONSTRAINT [FK_SupportDistributions_FinancialSupports_FinancialSupportId]
GO
ALTER TABLE [dbo].[SupportDocuments]  WITH CHECK ADD  CONSTRAINT [FK_SupportDocuments_FinancialSupports_FinancialSupportId] FOREIGN KEY([FinancialSupportId])
REFERENCES [dbo].[FinancialSupports] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[SupportDocuments] CHECK CONSTRAINT [FK_SupportDocuments_FinancialSupports_FinancialSupportId]
GO
ALTER TABLE [dbo].[SupportFeeDetails]  WITH CHECK ADD  CONSTRAINT [FK_SupportFeeDetails_FinancialSupports_FinancialSupportId] FOREIGN KEY([FinancialSupportId])
REFERENCES [dbo].[FinancialSupports] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[SupportFeeDetails] CHECK CONSTRAINT [FK_SupportFeeDetails_FinancialSupports_FinancialSupportId]
GO
ALTER TABLE [dbo].[SupportHistoricalData]  WITH CHECK ADD  CONSTRAINT [FK_SupportHistoricalData_FinancialSupports_FinancialSupportId] FOREIGN KEY([FinancialSupportId])
REFERENCES [dbo].[FinancialSupports] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[SupportHistoricalData] CHECK CONSTRAINT [FK_SupportHistoricalData_FinancialSupports_FinancialSupportId]
GO
ALTER TABLE [dbo].[SupportLookthroughAssets]  WITH CHECK ADD  CONSTRAINT [FK_SupportLookthroughAssets_FinancialSupports_FinancialSupportId] FOREIGN KEY([FinancialSupportId])
REFERENCES [dbo].[FinancialSupports] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[SupportLookthroughAssets] CHECK CONSTRAINT [FK_SupportLookthroughAssets_FinancialSupports_FinancialSupportId]
GO
ALTER TABLE [dbo].[SupportPortfolioLinks]  WITH CHECK ADD  CONSTRAINT [FK_SupportPortfolioLinks_FinancialSupports_FinancialSupportId] FOREIGN KEY([FinancialSupportId])
REFERENCES [dbo].[FinancialSupports] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[SupportPortfolioLinks] CHECK CONSTRAINT [FK_SupportPortfolioLinks_FinancialSupports_FinancialSupportId]
GO
ALTER TABLE [dbo].[SupportRegulations]  WITH CHECK ADD  CONSTRAINT [FK_SupportRegulations_FinancialSupports_FinancialSupportId] FOREIGN KEY([FinancialSupportId])
REFERENCES [dbo].[FinancialSupports] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[SupportRegulations] CHECK CONSTRAINT [FK_SupportRegulations_FinancialSupports_FinancialSupportId]
GO
ALTER TABLE [dbo].[SupportRiskProfiles]  WITH CHECK ADD  CONSTRAINT [FK_SupportRiskProfiles_FinancialSupports_FinancialSupportId] FOREIGN KEY([FinancialSupportId])
REFERENCES [dbo].[FinancialSupports] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[SupportRiskProfiles] CHECK CONSTRAINT [FK_SupportRiskProfiles_FinancialSupports_FinancialSupportId]
GO
ALTER TABLE [dbo].[SupportTechnicals]  WITH CHECK ADD  CONSTRAINT [FK_SupportTechnicals_FinancialSupports_FinancialSupportId] FOREIGN KEY([FinancialSupportId])
REFERENCES [dbo].[FinancialSupports] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[SupportTechnicals] CHECK CONSTRAINT [FK_SupportTechnicals_FinancialSupports_FinancialSupportId]
GO
ALTER TABLE [dbo].[SupportValuations]  WITH CHECK ADD  CONSTRAINT [FK_SupportValuations_FinancialSupports_FinancialSupportId] FOREIGN KEY([FinancialSupportId])
REFERENCES [dbo].[FinancialSupports] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[SupportValuations] CHECK CONSTRAINT [FK_SupportValuations_FinancialSupports_FinancialSupportId]
GO
ALTER TABLE [dbo].[TaxDatas]  WITH CHECK ADD  CONSTRAINT [FK_TaxDatas_FinancialSupports_FinancialSupportId] FOREIGN KEY([FinancialSupportId])
REFERENCES [dbo].[FinancialSupports] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[TaxDatas] CHECK CONSTRAINT [FK_TaxDatas_FinancialSupports_FinancialSupportId]
GO
ALTER TABLE [dbo].[UserRoles]  WITH CHECK ADD  CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY([RoleId])
REFERENCES [dbo].[Roles] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[UserRoles] CHECK CONSTRAINT [FK_UserRoles_Roles_RoleId]
GO
ALTER TABLE [dbo].[UserRoles]  WITH CHECK ADD  CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[UserRoles] CHECK CONSTRAINT [FK_UserRoles_Users_UserId]
GO
ALTER TABLE [dbo].[WithdrawalDetails]  WITH CHECK ADD  CONSTRAINT [FK_WithdrawalDetails_Operations_OperationId] FOREIGN KEY([OperationId])
REFERENCES [dbo].[Operations] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[WithdrawalDetails] CHECK CONSTRAINT [FK_WithdrawalDetails_Operations_OperationId]
GO
USE [master]
GO
ALTER DATABASE [Life] SET  READ_WRITE 
GO
