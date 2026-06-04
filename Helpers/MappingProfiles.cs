using api.Dtos.BeneficiaryClause;
using api.Dtos.Person;
using api.Dtos.Brand;
using api.Dtos.Contract;
using api.Dtos.Insurer;
using api.Dtos.Product;
using api.Models;
using api.Dtos.Yahoo;
using api.Dtos.FinancialSupport;
using api.Dtos.Eod;
using api.Dtos.Compartment;
using api.Dtos.Operation;
using Mapster;

namespace api.Helpers
{
    public class MappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            // --------- PERSON ---------
            config.NewConfig<Person, PersonDto>();
            config.NewConfig<PersonDto, Person>();
            config.NewConfig<UpdatePersonRequestDto, Person>()
                .Map(dest => dest.UpdatedDate, src => DateTime.UtcNow);

            // --------- BENEFICIARY CLAUSE ---------
            config.NewConfig<BeneficiaryClausePerson, BeneficiaryClausePersonDto>()
                .Map(dest => dest.Person, src => src.Person);
            config.NewConfig<BeneficiaryClausePersonDto, BeneficiaryClausePerson>();
            config.NewConfig<UpdateBeneficiaryClauseRequestDto, BeneficiaryClause>();
            config.NewConfig<CreateBeneficiaryClauseRequestDto, BeneficiaryClause>();

            // --------- BRAND ---------
            config.NewConfig<UpdateBrandRequestDto, Brand>();

            // --------- FINANCIAL SUPPORT ---------
            config.NewConfig<FinancialSupport, FinancialSupportDto>();
            config.NewConfig<FinancialSupportAllocation, FinancialSupportAllocationDto>()
                .Map(dest => dest.Support, src => src.Support!)
                .Map(dest => dest.CompartmentId, src => src.CompartmentId);
            config.NewConfig<FinancialSupportAllocationDto, FinancialSupportAllocation>()
                .Ignore(dest => dest.Support)
                .Map(dest => dest.CreatedDate, src => src.CreatedDate)
                .Map(dest => dest.UpdatedDate, src => DateTime.UtcNow)
                .Map(dest => dest.CompartmentId, src => src.CompartmentId);

            config.NewConfig<FinancialSupport, FinancialSupportLightDto>()
                .Map(dest => dest.Isin, src => src.ISIN)
                .Map(dest => dest.Label, src => src.Label);

            // --------- COMPARTMENT ---------
            config.NewConfig<Compartment, CompartmentDto>();
            config.NewConfig<CompartmentDto, Compartment>();

            // --------- INSURER ---------
            config.NewConfig<UpdateInsurerRequestDto, Insurer>();

            // --------- PRODUCT ---------
            config.NewConfig<UpdateProductRequestDto, Product>();

            // --------- FINANCIAL SUPPORT CREATE/UPDATE ---------
            config.NewConfig<CreateFinancialSupportRequestDto, FinancialSupport>();
            config.NewConfig<UpdateFinancialSupportRequestDto, FinancialSupport>()
                .IgnoreNullValues(true);

            config.NewConfig<EodFundProfile, CreateFinancialSupportRequestDto>()
                .IgnoreNullValues(true);

            // Mapping Yahoo → UpdateDto : seulement les 4 champs explicites, reste ignoré
            config.NewConfig<YahooETFDto, UpdateFinancialSupportRequestDto>()
                .Map(dest => dest.Label, src => src.Label)
                .Map(dest => dest.Currency, src => src.Currency)
                .Map(dest => dest.LastValuationAmount, src => src.LastNav)
                .Map(dest => dest.LastValuationDate, src => src.LastNavDate)
                .IgnoreNonMapped(true);

            // Mapping Yahoo → CreateDto : seulement les 4 champs explicites, reste ignoré
            config.NewConfig<YahooETFDto, CreateFinancialSupportRequestDto>()
                .Map(dest => dest.Label, src => src.Label)
                .Map(dest => dest.Currency, src => src.Currency)
                .Map(dest => dest.LastValuationAmount, src => src.LastNav)
                .Map(dest => dest.LastValuationDate, src => src.LastNavDate)
                .IgnoreNonMapped(true);

            // --------- CONTRACT ---------
            config.NewConfig<ContractOption, ContractOptionDto>();
            config.NewConfig<ContractOptionDto, ContractOption>();
            config.NewConfig<Contract, ContractDto>()
                .Map(dest => dest.Person, src => src.Person)
                .Map(dest => dest.Options, src => src.Options);
            config.NewConfig<ContractDto, Contract>();
            config.NewConfig<Contract, ContractDto>();

            // --------- OPERATION ---------
            config.NewConfig<Operation, OperationDto>()
                .Map(dest => dest.Details, src => OperationDetailsMapper.ToDto(src))
                .Map(dest => dest.AdvanceDetail, src => src.AdvanceDetail)
                .Map(dest => dest.ContractId, src => src.ContractId)
                .Map(dest => dest.Allocations, src => src.Allocations)
                .Map(dest => dest.Contract, src => src.Contract);

            config.NewConfig<OperationDto, Operation>()
                .Ignore(dest => dest.Contract)
                .Map(dest => dest.PaymentDetail, src => OperationDetailsMapper.ToPaymentModel(src.Details))
                .Map(dest => dest.WithdrawalDetail, src => OperationDetailsMapper.ToWithdrawalModel(src.Details))
                .Map(dest => dest.ArbitrageDetail, src => (ArbitrageDetail?)OperationDetailsMapper.ToArbitrageModel(src.Details))
                .Map(dest => dest.AdvanceDetail, src => src.AdvanceDetail)
                .Map(dest => dest.Allocations, src => src.Allocations);

            // --------- DETAILS (Advance) ---------
            config.NewConfig<AdvanceDetail, AdvanceDetailDto>();
            config.NewConfig<AdvanceDetailDto, AdvanceDetail>();

            // --------- ALLOCATIONS (Operation) ---------
            config.NewConfig<OperationSupportAllocation, OperationSupportAllocationDto>()
                .Map(dest => dest.Support, src => src.Support!)
                .Map(dest => dest.CompartmentId, src => src.CompartmentId)
                .Map(dest => dest.Shares, src => src.Shares);
            config.NewConfig<OperationSupportAllocationDto, OperationSupportAllocation>()
                .Ignore(dest => dest.Operation!)
                .Ignore(dest => dest.Support!)
                .Map(dest => dest.CompartmentId, src => src.CompartmentId)
                .Map(dest => dest.Shares, src => src.Shares);
        }
    }
}
