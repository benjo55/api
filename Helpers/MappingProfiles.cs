using api.Dtos.BeneficiaryClause;
using api.Dtos.Person;
using api.Dtos.Brand;
using api.Dtos.Contract;
using api.Dtos.Insurer;
using api.Dtos.Product;
using api.Models;
using api.Dtos.Yahoo;
using api.Dtos.FinancialSupport;
using AutoMapper;
using api.Dtos.Eod;
using api.Dtos.Compartment;
using api.Dtos.Operation; // 🔹 contient OperationDto & détails

namespace api.Helpers
{
    public class MappingProfiles : Profile
    {
        public MappingProfiles()
        {
            // --------- PERSON ---------
            CreateMap<Person, PersonDto>().ReverseMap(); // ✅ mapping bidirectionnel
            CreateMap<UpdatePersonRequestDto, Person>()
                .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(_ => DateTime.UtcNow));
            CreateMap<Person, Person>(); // Pour l'historisation

            // --------- BENEFICIARY CLAUSE ---------
            CreateMap<BeneficiaryClausePerson, BeneficiaryClausePersonDto>()
                .ForMember(dest => dest.Person, opt => opt.MapFrom(src => src.Person));
            CreateMap<BeneficiaryClausePersonDto, BeneficiaryClausePerson>();
            CreateMap<UpdateBeneficiaryClauseRequestDto, BeneficiaryClause>();
            CreateMap<CreateBeneficiaryClauseRequestDto, BeneficiaryClause>();
            CreateMap<BeneficiaryClause, BeneficiaryClause>(); // Historisation

            // --------- BRAND ---------
            CreateMap<UpdateBrandRequestDto, Brand>();
            CreateMap<Brand, Brand>();

            // --------- FINANCIAL SUPPORT ---------
            CreateMap<FinancialSupport, FinancialSupportDto>();
            CreateMap<FinancialSupportAllocation, FinancialSupportAllocationDto>()
                .ForMember(dest => dest.Support, opt => opt.MapFrom(src => src.Support))
                .ForMember(dest => dest.CompartmentId, opt => opt.MapFrom(src => src.CompartmentId)); // ✅ ajouté

            CreateMap<FinancialSupportAllocationDto, FinancialSupportAllocation>()
                .ForMember(dest => dest.Support, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedDate))
                .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.CompartmentId, opt => opt.MapFrom(src => src.CompartmentId)); // ✅ ajouté

            CreateMap<FinancialSupport, FinancialSupportLightDto>()
                .ForMember(dest => dest.Isin, opt => opt.MapFrom(src => src.ISIN))
                .ForMember(dest => dest.Label, opt => opt.MapFrom(src => src.Label));

            // --------- COMPARTMENT ---------
            CreateMap<Compartment, CompartmentDto>().ReverseMap();

            // --------- INSURER ---------
            CreateMap<UpdateInsurerRequestDto, Insurer>();
            CreateMap<Insurer, Insurer>();

            // --------- PRODUCT ---------
            CreateMap<UpdateProductRequestDto, Product>();
            CreateMap<Product, Product>();

            // --------- FINANCIAL SUPPORT CREATE/UPDATE ---------
            CreateMap<CreateFinancialSupportRequestDto, FinancialSupport>();
            CreateMap<UpdateFinancialSupportRequestDto, FinancialSupport>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<EodFundProfile, CreateFinancialSupportRequestDto>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<YahooETFDto, UpdateFinancialSupportRequestDto>()
                .ForMember(dest => dest.Label, opt => opt.MapFrom(src => src.Label))
                .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Currency))
                .ForMember(dest => dest.LastValuationAmount, opt => opt.MapFrom(src => src.LastNav))
                .ForMember(dest => dest.LastValuationDate, opt => opt.MapFrom(src => src.LastNavDate))
                .ForAllMembers(opt => opt.Ignore());

            CreateMap<YahooETFDto, CreateFinancialSupportRequestDto>()
                .ForMember(dest => dest.Label, opt => opt.MapFrom(src => src.Label))
                .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Currency))
                .ForMember(dest => dest.LastValuationAmount, opt => opt.MapFrom(src => src.LastNav))
                .ForMember(dest => dest.LastValuationDate, opt => opt.MapFrom(src => src.LastNavDate))
                .ForAllMembers(opt => opt.Ignore());

            // --------- CONTRACT ---------
            CreateMap<ContractOption, ContractOptionDto>().ReverseMap(); // ✅ ajout essentiel
            CreateMap<Contract, ContractDto>()
                .ForMember(dest => dest.Person, opt => opt.MapFrom(src => src.Person))
                .ForMember(dest => dest.Options, opt => opt.MapFrom(src => src.Options)); // ✅ ajouté
            CreateMap<ContractDto, Contract>().ReverseMap();


            // --------- OPERATION ---------
            CreateMap<Operation, OperationDto>()
                .ForMember(dest => dest.WithdrawalDetail, opt => opt.MapFrom(src => src.WithdrawalDetail))
                .ForMember(dest => dest.ArbitrageDetail, opt => opt.MapFrom(src => src.ArbitrageDetail))
                .ForMember(dest => dest.AdvanceDetail, opt => opt.MapFrom(src => src.AdvanceDetail))
                .ForMember(dest => dest.PaymentDetail, opt => opt.MapFrom(src => src.PaymentDetail))
                .ForMember(dest => dest.ContractId, opt => opt.MapFrom(src => src.ContractId))
                .ForMember(dest => dest.CompartmentId, opt => opt.MapFrom(src => src.CompartmentId))
                .ForMember(dest => dest.Allocations, opt => opt.MapFrom(src => src.Allocations))
                .ForMember(dest => dest.Contract, opt => opt.MapFrom(src => src.Contract)); // ✅ inclure contrat complet

            CreateMap<OperationDto, Operation>()
                .ForMember(dest => dest.Contract, opt => opt.Ignore())
                .ForMember(dest => dest.Compartment, opt => opt.Ignore())
                .ForMember(dest => dest.WithdrawalDetail, opt => opt.MapFrom(src => src.WithdrawalDetail))
                .ForMember(dest => dest.ArbitrageDetail, opt => opt.MapFrom(src => src.ArbitrageDetail))
                .ForMember(dest => dest.AdvanceDetail, opt => opt.MapFrom(src => src.AdvanceDetail))
                .ForMember(dest => dest.PaymentDetail, opt => opt.MapFrom(src => src.PaymentDetail))
                .ForMember(dest => dest.Allocations, opt => opt.MapFrom(src => src.Allocations));

            // --------- DETAILS (Withdrawal, Arbitrage, Advance, Payment) ---------
            CreateMap<WithdrawalDetail, WithdrawalDetailDto>().ReverseMap();
            CreateMap<ArbitrageDetail, ArbitrageDetailDto>().ReverseMap();
            CreateMap<AdvanceDetail, AdvanceDetailDto>().ReverseMap();
            CreateMap<PaymentDetail, PaymentDetailDto>().ReverseMap();

            // --------- ALLOCATIONS (Operation) ---------
            CreateMap<OperationSupportAllocation, OperationSupportAllocationDto>()
                .ForMember(dest => dest.Support, opt => opt.MapFrom(src => src.Support))
                .ForMember(dest => dest.CompartmentId, opt => opt.MapFrom(src => src.CompartmentId))
                .ForMember(dest => dest.Shares, opt => opt.MapFrom(src => src.Shares));
            CreateMap<OperationSupportAllocationDto, OperationSupportAllocation>()
                .ForMember(dest => dest.Operation, opt => opt.Ignore())
                .ForMember(dest => dest.Support, opt => opt.Ignore())
                .ForMember(dest => dest.CompartmentId, opt => opt.MapFrom(src => src.CompartmentId))
                .ForMember(dest => dest.Shares, opt => opt.MapFrom(src => src.Shares));
        }
    }
}
