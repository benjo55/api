using api.Data;
using api.Dtos.Compartment;
using api.Dtos.Contract;
using api.Interfaces;
using api.Models;
using api.Repository;
using api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace api.Tests;

[TestClass]
public class ContractRepositoryLockedUpdateTests
{
    [TestMethod]
    public async Task UpdateAsync_LockedWithOperations_OnlyAllowsCompartmentsAndOptions()
    {
        await using var db = CreateDbContext();
        var contract = SeedContract(db, locked: true, withOperation: true, withInvestedCompartment: false);
        var originalStatus = contract.Status;
        var originalType = contract.ContractType;
        var originalCurrency = contract.Currency;
        var originalDateSign = contract.DateSign;

        var repository = CreateRepository(db);
        var customCompartment = contract.Compartments.First(c => !c.IsDefault);

        var dto = BuildUpdateDto(contract);
        dto.Status = "Résilié";
        dto.ContractType = "Retraite";
        dto.Currency = "USD";
        dto.DateSign = contract.DateSign.AddDays(10);
        dto.Options =
        [
            new ContractOptionDto
            {
                ContractOptionTypeId = 2002,
                IsActive = true,
                Description = "Nouvelle option"
            }
        ];
        dto.Compartments =
        [
            new UpdateCompartmentRequestDto
            {
                Id = customCompartment.Id,
                ContractId = contract.Id,
                Label = "Renommé",
                ManagementMode = customCompartment.ManagementMode,
                Notes = customCompartment.Notes,
                Description = customCompartment.Description
            },
            new UpdateCompartmentRequestDto
            {
                Id = 0,
                ContractId = contract.Id,
                Label = "Nouveau compartiment",
                ManagementMode = "Libre",
                Notes = "Ajout"
            }
        ];

        var updated = await repository.UpdateAsync(contract.Id, dto);

        Assert.IsNotNull(updated);
        Assert.AreEqual(originalStatus, updated!.Status);
        Assert.AreEqual(originalType, updated.ContractType);
        Assert.AreEqual(originalCurrency, updated.Currency);
        Assert.AreEqual(originalDateSign, updated.DateSign);
        Assert.IsTrue(updated.Compartments.Any(c => c.Label == "Renommé"));
        Assert.IsTrue(updated.Compartments.Any(c => c.Label == "Nouveau compartiment"));

        var persistedOptions = db.ContractOptions
            .Where(o => o.ContractId == contract.Id)
            .ToList();
        Assert.AreEqual(1, persistedOptions.Count);
        Assert.AreEqual(2002, persistedOptions[0].ContractOptionTypeId);
    }

    [TestMethod]
    public async Task UpdateAsync_LockedWithOperations_ThrowsWhenDeletingInvestedCompartment()
    {
        await using var db = CreateDbContext();
        var contract = SeedContract(db, locked: true, withOperation: true, withInvestedCompartment: true);
        var repository = CreateRepository(db);
        var global = contract.Compartments.First(c => c.IsDefault);

        var dto = BuildUpdateDto(contract);
        dto.Compartments =
        [
            new UpdateCompartmentRequestDto
            {
                Id = global.Id,
                ContractId = contract.Id,
                Label = global.Label,
                ManagementMode = global.ManagementMode,
                Notes = global.Notes,
                Description = global.Description
            }
        ];

        var ex = await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
            repository.UpdateAsync(contract.Id, dto));

        StringAssert.Contains(ex.Message, "investi");
    }

    [TestMethod]
    public async Task UpdateAsync_LockedWithOperations_AllowsDeletingNonInvestedCompartment()
    {
        await using var db = CreateDbContext();
        var contract = SeedContract(db, locked: true, withOperation: true, withInvestedCompartment: false);
        var repository = CreateRepository(db);
        var global = contract.Compartments.First(c => c.IsDefault);

        var dto = BuildUpdateDto(contract);
        dto.Compartments =
        [
            new UpdateCompartmentRequestDto
            {
                Id = global.Id,
                ContractId = contract.Id,
                Label = global.Label,
                ManagementMode = global.ManagementMode,
                Notes = global.Notes,
                Description = global.Description
            }
        ];

        var updated = await repository.UpdateAsync(contract.Id, dto);

        Assert.IsNotNull(updated);
        Assert.AreEqual(1, updated!.Compartments.Count);
        Assert.IsTrue(updated.Compartments.Single().IsDefault);
    }

    [TestMethod]
    public async Task UpdateAsync_Unlocked_AllowsScalarFieldChanges()
    {
        await using var db = CreateDbContext();
        var contract = SeedContract(db, locked: false, withOperation: true, withInvestedCompartment: false);
        var repository = CreateRepository(db);

        var dto = BuildUpdateDto(contract);
        dto.Status = "Résilié";
        dto.ContractType = "Retraite";
        dto.Currency = "USD";

        var updated = await repository.UpdateAsync(contract.Id, dto);

        Assert.IsNotNull(updated);
        Assert.AreEqual("Résilié", updated!.Status);
        Assert.AreEqual("Retraite", updated.ContractType);
        Assert.AreEqual("USD", updated.Currency);
    }

    private static ApplicationDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase($"contracts-tests-{Guid.NewGuid()}")
            .Options;

        var db = new ApplicationDBContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static ContractRepository CreateRepository(ApplicationDBContext db)
    {
        var historyService = new EntityHistoryService(new FakeEntityHistoryRepository(), db);

        return new ContractRepository(
            db,
            historyService,
            new FakeContractValuationService(),
            new FakeOperationEngineService(),
            NullLogger<ContractRepository>.Instance);
    }

    private static Contract SeedContract(
        ApplicationDBContext db,
        bool locked,
        bool withOperation,
        bool withInvestedCompartment)
    {
        var contract = new Contract
        {
            ContractNumber = "TST-00001",
            ContractLabel = "Contrat test",
            ContractType = "Assurance Vie",
            Status = "Actif",
            Locked = locked,
            DateSign = new DateTime(2026, 01, 01),
            DateEffect = new DateTime(2026, 01, 01),
            DateMaturity = new DateTime(2036, 01, 01),
            PostalAddress = "Adresse",
            TaxAddress = "Adresse fiscale",
            Currency = "EUR",
            PersonId = null,
            PaymentMode = "Libre",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow,
            Options =
            [
                new ContractOption
                {
                    ContractOptionTypeId = 1001,
                    IsActive = true,
                    Description = "Option initiale"
                }
            ],
            Compartments =
            [
                new Compartment
                {
                    Label = "Global",
                    IsDefault = true,
                    Description = "Global",
                    ManagementMode = "Standard",
                    Notes = "Auto",
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                },
                new Compartment
                {
                    Label = "Compartiment 1",
                    IsDefault = false,
                    Description = "Libre",
                    ManagementMode = "Libre",
                    Notes = "Test",
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                }
            ]
        };

        db.Contracts.Add(contract);
        db.SaveChanges();

        if (withOperation)
        {
            db.Operations.Add(new Operation
            {
                ContractId = contract.Id,
                Type = OperationType.InitialPayment,
                Status = OperationStatus.Executed,
                OperationDate = DateTime.UtcNow,
                Amount = 1000m,
                Currency = "EUR"
            });
            db.SaveChanges();
        }

        if (withInvestedCompartment)
        {
            var investedCompartment = db.Compartments
                .Where(c => c.ContractId == contract.Id && !c.IsDefault)
                .OrderBy(c => c.Id)
                .First();

            db.ContractSupportHoldings.Add(new ContractSupportHolding
            {
                ContractId = contract.Id,
                CompartmentId = investedCompartment.Id,
                SupportId = 999,
                TotalShares = 1m,
                TotalInvested = 100m,
                Pru = 100m,
                CurrentAmount = 120m,
                LastUpdated = DateTime.UtcNow
            });
            db.SaveChanges();
        }

        return db.Contracts
            .Include(c => c.Compartments)
            .Include(c => c.Options)
            .First(c => c.Id == contract.Id);
    }

    private static UpdateContractRequestDto BuildUpdateDto(Contract contract)
    {
        return new UpdateContractRequestDto
        {
            Id = contract.Id,
            ContractNumber = contract.ContractNumber,
            ContractLabel = contract.ContractLabel,
            ContractType = contract.ContractType,
            Status = contract.Status,
            Locked = contract.Locked,
            DateSign = contract.DateSign,
            DateEffect = contract.DateEffect,
            DateMaturity = contract.DateMaturity,
            PostalAddress = contract.PostalAddress,
            TaxAddress = contract.TaxAddress,
            Currency = contract.Currency,
            PersonId = contract.PersonId,
            InitialPremium = contract.InitialPremium,
            TotalPaidPremiums = contract.TotalPaidPremiums,
            CurrentValue = contract.CurrentValue,
            RedemptionValue = contract.RedemptionValue,
            PaymentMode = contract.PaymentMode,
            ScheduledPayment = contract.ScheduledPayment,
            BeneficiaryClauseId = contract.BeneficiaryClauseId,
            EntryFeesRate = contract.EntryFeesRate,
            ManagementFeesRate = contract.ManagementFeesRate,
            ExitFeesRate = contract.ExitFeesRate,
            AdvisorComment = contract.AdvisorComment,
            HasAlert = contract.HasAlert,
            ExternalReference = contract.ExternalReference,
            LastModifiedByUserId = contract.LastModifiedByUserId,
            CreatedDate = contract.CreatedDate,
            UpdatedDate = DateTime.UtcNow,
            InsuredPersonIds = [],
            Documents = [],
            Options = contract.Options
                .Select(o => new ContractOptionDto
                {
                    Id = o.Id,
                    ContractOptionTypeId = o.ContractOptionTypeId,
                    IsActive = o.IsActive,
                    Description = o.Description,
                    CustomParameters = o.CustomParameters
                })
                .ToList(),
            Compartments = contract.Compartments
                .Select(c => new UpdateCompartmentRequestDto
                {
                    Id = c.Id,
                    ContractId = contract.Id,
                    Label = c.Label,
                    Description = c.Description,
                    ManagementMode = c.ManagementMode,
                    Notes = c.Notes,
                    CreatedDate = c.CreatedDate,
                    UpdatedDate = c.UpdatedDate
                })
                .ToList()
        };
    }

    private sealed class FakeEntityHistoryRepository : IEntityHistoryRepository
    {
        public Task SaveEntityHistoryAsync(EntityHistory history) => Task.CompletedTask;

        public Task<IEnumerable<EntityHistory>> GetHistoryForEntityAsync(string EntityName, int entityId)
            => Task.FromResult(Enumerable.Empty<EntityHistory>());
    }

    private sealed class FakeContractValuationService : IContractValuationService
    {
        public Task<decimal> ComputeContractValueAsync(int contractId) => Task.FromResult(0m);

        public Task<int> UpdateFsaAmountsForSupportAsync(int supportId) => Task.FromResult(0);
    }

    private sealed class FakeOperationEngineService : IOperationEngineService
    {
        public Task UpdateValuationsAsync() => Task.CompletedTask;

        public Task ProcessPendingOperationsAsync() => Task.CompletedTask;

        public Task ApplyRulesAsync() => Task.CompletedTask;

        public Task ApplyManagementFeesAsync() => Task.CompletedTask;

        public Task RebuildContractAsync(int contractId) => Task.CompletedTask;

        public Task ApplyOperationAsync(Operation op) => Task.CompletedTask;
    }
}
