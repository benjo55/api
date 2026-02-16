using System;
using System.Collections.Generic;
using System.Linq;
using api.Dtos.Contract;
using api.Dtos.Document;
using api.Dtos.FinancialSupport;
using api.Dtos.Person;
using api.Dtos.Compartment;
using api.Models;
using Contract = api.Models.Contract;

namespace api.Mappers
{
    public static class ContractMapper
    {
        // 🔹 Model -> DTO
        public static ContractDto ToContractDto(this Contract contractModel)
        {
            return new ContractDto
            {
                Id = contractModel.Id,
                ContractNumber = contractModel.ContractNumber,
                ContractLabel = contractModel.ContractLabel,
                ContractType = contractModel.ContractType,
                Status = contractModel.Status,
                Locked = contractModel.Locked,

                DateSign = contractModel.DateSign,
                DateEffect = contractModel.DateEffect,
                DateMaturity = contractModel.DateMaturity,

                PostalAddress = contractModel.PostalAddress,
                TaxAddress = contractModel.TaxAddress,
                Currency = contractModel.Currency,
                PersonId = contractModel.PersonId,
                ProductId = contractModel.ProductId,

                BeneficiaryClauseId = contractModel.BeneficiaryClauseId,
                InitialPremium = contractModel.InitialPremium,
                TotalPaidPremiums = contractModel.TotalPaidPremiums,

                PaidExecuted = contractModel.PaidExecuted,
                PaidPending = contractModel.PaidPending,
                TotalPayments = contractModel.TotalPayments,

                WithdrawnExecuted = contractModel.WithdrawnExecuted,
                WithdrawnPending = contractModel.WithdrawnPending,
                TotalWithdrawals = contractModel.WithdrawnExecuted + contractModel.WithdrawnPending,

                NetInvested = contractModel.NetInvested,
                PerformancePercent = contractModel.PerformancePercent,
                CurrentValue = contractModel.CurrentValue,
                RedemptionValue = contractModel.RedemptionValue,

                PaymentMode = contractModel.PaymentMode,
                ScheduledPayment = contractModel.ScheduledPayment,

                EntryFeesRate = contractModel.EntryFeesRate,
                ManagementFeesRate = contractModel.ManagementFeesRate,
                ExitFeesRate = contractModel.ExitFeesRate,
                AdvisorComment = contractModel.AdvisorComment,
                HasAlert = contractModel.HasAlert,
                ExternalReference = contractModel.ExternalReference,

                CreatedByUserId = contractModel.CreatedByUserId,
                LastModifiedByUserId = contractModel.LastModifiedByUserId,
                CreatedDate = contractModel.CreatedDate,
                UpdatedDate = contractModel.UpdatedDate,

                // 🔹 Personne principale
                Person = contractModel.Person == null ? null : new PersonDto
                {
                    Id = contractModel.Person.Id,
                    FirstName = contractModel.Person.FirstName,
                    LastName = contractModel.Person.LastName,
                    BirthDate = contractModel.Person.BirthDate,
                    BirthCity = contractModel.Person.BirthCity
                },

                // 🔹 Assurés
                InsuredPersons = contractModel.InsuredLinks?
                    .Select(link => link.Person.ToPersonDto())
                    .ToList() ?? new(),

                // 🔹 Options
                Options = contractModel.Options?
                    .Select(o => o.ToDto())
                    .ToList() ?? new(),

                // 🔹 Global supports (holdings consolidés)
                Supports = contractModel.Supports?
                    .Select(s => new FinancialSupportAllocationDto
                    {
                        Id = s.Id,
                        ContractId = s.ContractId,
                        SupportId = s.SupportId,
                        CompartmentId = s.CompartmentId ?? 0,

                        AllocationPercentage = s.AllocationPercentage,

                        CurrentShares = s.CurrentShares,
                        CurrentAmount = s.CurrentAmount,
                        InvestedAmount = s.InvestedAmount,     // 🟢 FIX 1

                        CreatedDate = s.CreatedDate,
                        UpdatedDate = s.UpdatedDate,

                        Support = s.Support?.ToFinancialSupportDto()
                    }).ToList() ?? new(),

                // 🔹 Compartiments (multi-support)
                Compartments = contractModel.Compartments?
                    .Select(c => new CompartmentDto
                    {
                        Id = c.Id,
                        ContractId = c.ContractId,
                        Label = c.Label,
                        Description = c.Description,
                        ManagementMode = c.ManagementMode,
                        Notes = c.Notes,
                        IsDefault = c.IsDefault,

                        CreatedDate = c.CreatedDate,
                        UpdatedDate = c.UpdatedDate,
                        CurrentValue = c.CurrentValue,

                        // ------ Supports du compartiment ------
                        Supports = c.Supports?
                            .Select(s => new FinancialSupportAllocationDto
                            {
                                Id = s.Id,
                                ContractId = s.ContractId,
                                SupportId = s.SupportId,
                                CompartmentId = s.CompartmentId ?? 0,      // 🟢 FIX 3

                                AllocationPercentage = s.AllocationPercentage,

                                CurrentShares = s.CurrentShares,
                                CurrentAmount = s.CurrentAmount,
                                InvestedAmount = s.InvestedAmount,    // 🟢 FIX 4

                                CreatedDate = s.CreatedDate,
                                UpdatedDate = s.UpdatedDate,

                                Support = s.Support?.ToFinancialSupportDto()
                            }).ToList() ?? new()
                    })
                .ToList() ?? new(),

                // 🔹 Documents
                Documents = contractModel.Documents?
                    .Select(d => new DocumentDto
                    {
                        Id = d.Id,
                        FileName = d.FileName,
                        FileType = d.FileType,
                        Url = d.Url,
                        UploadedAt = d.UploadedAt
                    })
                    .ToList() ?? new()
            };
        }

        // 🔹 DTO (Create) -> Model
        public static Contract ToContractFromCreateDto(this CreateContractRequestDto contractDto)
        {
            return new Contract
            {
                ContractNumber = contractDto.ContractNumber,
                ContractLabel = contractDto.ContractLabel,
                ContractType = contractDto.ContractType,
                Status = contractDto.Status,
                DateSign = contractDto.DateSign,
                DateEffect = contractDto.DateEffect,
                DateMaturity = contractDto.DateMaturity,
                PostalAddress = contractDto.PostalAddress,
                TaxAddress = contractDto.TaxAddress,
                Currency = contractDto.Currency,
                PersonId = contractDto.PersonId,
                ProductId = contractDto.ProductId,
                BeneficiaryClauseId = contractDto.BeneficiaryClauseId,
                InitialPremium = contractDto.InitialPremium,
                TotalPaidPremiums = contractDto.TotalPaidPremiums,
                CurrentValue = contractDto.CurrentValue,
                RedemptionValue = contractDto.RedemptionValue,
                PaymentMode = contractDto.PaymentMode,
                ScheduledPayment = contractDto.ScheduledPayment,
                EntryFeesRate = contractDto.EntryFeesRate,
                ManagementFeesRate = contractDto.ManagementFeesRate,
                ExitFeesRate = contractDto.ExitFeesRate,
                AdvisorComment = contractDto.AdvisorComment,
                HasAlert = contractDto.HasAlert,
                ExternalReference = contractDto.ExternalReference,
                CreatedByUserId = contractDto.CreatedByUserId,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now,

                Options = contractDto.Options?
                    .Select(o => o.ToModel())
                    .ToList() ?? new(),

                Compartments = contractDto.Compartments?
                    .Select(c => new Compartment
                    {
                        Label = c.Label,
                        ManagementMode = c.ManagementMode,
                        Notes = c.Notes,
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now
                    }).ToList() ?? new()
            };
        }

        // 🔹 DTO (Update) -> Model
        public static Contract ToContractFromUpdateDto(this UpdateContractRequestDto contractDto)
        {
            return new Contract
            {
                Id = contractDto.Id,
                ContractNumber = contractDto.ContractNumber,
                ContractLabel = contractDto.ContractLabel,
                ContractType = contractDto.ContractType,
                Status = contractDto.Status,
                DateSign = contractDto.DateSign,
                DateEffect = contractDto.DateEffect,
                DateMaturity = contractDto.DateMaturity,
                PostalAddress = contractDto.PostalAddress,
                TaxAddress = contractDto.TaxAddress,
                Currency = contractDto.Currency,
                PersonId = contractDto.PersonId,
                InitialPremium = contractDto.InitialPremium,
                TotalPaidPremiums = contractDto.TotalPaidPremiums,
                CurrentValue = contractDto.CurrentValue,
                RedemptionValue = contractDto.RedemptionValue,
                PaymentMode = contractDto.PaymentMode,
                ScheduledPayment = contractDto.ScheduledPayment,
                BeneficiaryClauseId = contractDto.BeneficiaryClauseId,
                EntryFeesRate = contractDto.EntryFeesRate,
                ManagementFeesRate = contractDto.ManagementFeesRate,
                ExitFeesRate = contractDto.ExitFeesRate,
                AdvisorComment = contractDto.AdvisorComment,
                HasAlert = contractDto.HasAlert,
                ExternalReference = contractDto.ExternalReference,
                LastModifiedByUserId = contractDto.LastModifiedByUserId,

                CreatedDate = contractDto.CreatedDate,
                UpdatedDate = DateTime.Now,

                Options = contractDto.Options?
                    .Select(o => o.ToModel())
                    .ToList() ?? new(),

                Compartments = contractDto.Compartments?
                    .Select(c => new Compartment
                    {
                        Label = c.Label,
                        ManagementMode = c.ManagementMode,
                        Notes = c.Notes,
                        CreatedDate = c.CreatedDate,
                        UpdatedDate = DateTime.Now
                    })
                    .ToList() ?? new()
            };
        }
    }
}
