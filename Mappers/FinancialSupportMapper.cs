using AutoMapper;
using api.Models;
using api.Dtos.FinancialSupport;

namespace api.Mappers
{
    public class FinancialSupportProfile : Profile
    {
        public FinancialSupportProfile()
        {
            // Création (DTO -> entity)
            CreateMap<CreateFinancialSupportRequestDto, FinancialSupport>();

            // Mise à jour partielle (DTO -> entity)
            CreateMap<UpdateFinancialSupportRequestDto, FinancialSupport>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Lecture (entity -> DTO)
            CreateMap<FinancialSupport, FinancialSupportDto>();
        }
    }

    public static class FinancialSupportMapper
    {
        public static FinancialSupportDto ToFinancialSupportDto(this FinancialSupport model)
        {
            return new FinancialSupportDto
            {
                Id = model.Id,
                Code = model.Code,
                Label = model.Label,
                ISIN = model.ISIN,
                SupportType = model.SupportType,
                Currency = model.Currency,
                Status = model.Status,
                MarketingName = model.MarketingName,
                LegalName = model.LegalName,
                AMFCode = model.AMFCode,
                BloombergCode = model.BloombergCode,
                MorningstarCode = model.MorningstarCode,
                CUSIP = model.CUSIP,
                SEDOL = model.SEDOL,
                AssetManager = model.AssetManager,
                DepositaryBank = model.DepositaryBank,
                Custodian = model.Custodian,
                InceptionDate = model.InceptionDate,
                ClosureDate = model.ClosureDate,
                IsClosed = model.IsClosed,

                AssetClass = model.AssetClass,
                SubAssetClass = model.SubAssetClass,
                GeographicFocus = model.GeographicFocus,
                SectorFocus = model.SectorFocus,
                CapitalizationPolicy = model.CapitalizationPolicy,
                InvestmentStrategy = model.InvestmentStrategy,
                LegalForm = model.LegalForm,
                ManagementStyle = model.ManagementStyle,
                UCITSCategory = model.UCITSCategory,

                MinimumSubscription = model.MinimumSubscription,
                MinimumHolding = model.MinimumHolding,
                ManagementFee = model.ManagementFee,
                PerformanceFee = model.PerformanceFee,
                TurnoverRate = model.TurnoverRate,
                AUM = model.AUM,
                IsCapitalGuaranteed = model.IsCapitalGuaranteed,
                IsCurrencyHedged = model.IsCurrencyHedged,
                Benchmark = model.Benchmark,

                HasESGLabel = model.HasESGLabel,
                ESGLabel = model.ESGLabel,
                SFDRClassification = model.SFDRClassification,
                ESGScore = model.ESGScore,
                ESGScoreProvider = model.ESGScoreProvider,
                MifidTargetMarket = model.MifidTargetMarket,
                MifidRiskTolerance = model.MifidRiskTolerance,
                MifidClientType = model.MifidClientType,

                LastValuationAmount = model.LastValuationAmount,
                LastValuationDate = model.LastValuationDate,
                WeeklyVolatility = model.WeeklyVolatility,
                MaxDrawdown1Y = model.MaxDrawdown1Y,

                Distributor = model.Distributor,
                IsAvailableOnline = model.IsAvailableOnline,
                IsAdvisedSale = model.IsAdvisedSale,
                IsEligiblePEA = model.IsEligiblePEA,
                CountryOfDistribution = model.CountryOfDistribution,

                CreatedDate = model.CreatedDate,
                UpdatedDate = model.UpdatedDate,

                FundDomicile = model.FundDomicile,
                PrimaryListingMarket = model.PrimaryListingMarket,
                IsFundOfFunds = model.IsFundOfFunds
            };
        }

    }
}
