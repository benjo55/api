using api.Data;
using api.Dtos.Contract;
using api.Dtos.Generic;
using api.Helpers;
using api.Interfaces;
using api.Models;
using api.Services;
using api.Mappers;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace api.Repository
{
    public class ContractRepository : IContractRepository
    {
        private readonly ApplicationDBContext _context;
        private readonly EntityHistoryService _entityHistoryService;
        private readonly IContractValuationService _valuationService;
        private readonly IOperationEngineService _operationEngineService;
        private readonly ILogger<ContractRepository>? _logger;
        public ContractRepository(ApplicationDBContext context, EntityHistoryService entityHistoryService, IContractValuationService valuationService, IOperationEngineService operationEngineService, ILogger<ContractRepository>? logger = null)
        {
            _context = context;
            _entityHistoryService = entityHistoryService;
            _valuationService = valuationService;
            _operationEngineService = operationEngineService;
            _logger = logger;
        }

        // -------------------- CREATE --------------------
        public async Task<Contract> CreateAsync(Contract contractModel, CreateContractRequestDto dto)
        {
            // Empêche EF de réinsérer les compartiments liés au modèle
            contractModel.Compartments = new List<Compartment>();

            await _context.Contracts.AddAsync(contractModel);
            await _context.SaveChangesAsync(); // Id garanti ici

            // Vérifie si un Global est présent dans le payload
            bool hasGlobal = dto.Compartments?.Any(c =>
                c.IsDefault ||
                (!string.IsNullOrWhiteSpace(c.Label) && c.Label.Trim().ToLower() == "global")
            ) ?? false;

            var compartmentsToAdd = new List<Compartment>();

            // ➕ Si aucun Global n’est présent → création automatique
            if (!hasGlobal)
            {
                compartmentsToAdd.Add(new Compartment
                {
                    ContractId = contractModel.Id,
                    Label = "Global",
                    ManagementMode = "Standard",
                    Notes = "Compartiment principal automatique",
                    IsDefault = true,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                });
                _logger?.LogInformation("🆕 Compartiment global créé automatiquement pour le contrat {ContractId}", contractModel.Id);
            }

            // ➕ Ajoute tous les compartiments du front (y compris le Global envoyé)
            if (dto.Compartments?.Any() == true)
            {
                foreach (var compDto in dto.Compartments
                             .GroupBy(c => c.Label.Trim().ToLower())
                             .Select(g => g.First()))
                {
                    var label = compDto.Label?.Trim() ?? "";

                    compartmentsToAdd.Add(new Compartment
                    {
                        ContractId = contractModel.Id,
                        Label = label,
                        ManagementMode = compDto.ManagementMode ?? "Libre",
                        Notes = compDto.Notes,
                        IsDefault = compDto.IsDefault, // ← conserve la valeur envoyée
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow
                    });
                }
            }

            if (compartmentsToAdd.Any())
                _context.Compartments.AddRange(compartmentsToAdd);

            // Assurés
            if (dto.InsuredPersonIds?.Any() == true)
            {
                var insuredLinks = dto.InsuredPersonIds.Select(pid => new ContractInsuredPerson
                {
                    Contract = contractModel,
                    PersonId = pid
                });
                _context.ContractInsuredPersons.AddRange(insuredLinks);
            }

            // Options
            if (dto.Options?.Any() == true)
            {
                var options = dto.Options.Select(opt => new ContractOption
                {
                    Contract = contractModel,
                    ContractOptionTypeId = opt.ContractOptionTypeId,
                    Description = opt.Description,
                    IsActive = opt.IsActive,
                    CustomParameters = opt.CustomParameters
                });
                _context.ContractOptions.AddRange(options);
            }

            // Documents
            if (dto.Documents?.Any() == true)
            {
                var documents = dto.Documents.Select(d => new Document
                {
                    Contract = contractModel,
                    FileName = d.FileName,
                    FileType = d.FileType,
                    Url = d.Url,
                    UploadedAt = d.UploadedAt
                });
                _context.Documents.AddRange(documents);
            }

            await _context.SaveChangesAsync();

            if (contractModel.PersonId > 0)
            {
                await _entityHistoryService.TrackEventAsync(
                    "Person",
                    contractModel.PersonId.Value,
                    "Contrat créé",
                    null,
                    $"Contrat n°{contractModel.Id} créé",
                    "Admin"
                );
            }

            return await LoadContractById(contractModel.Id) ?? contractModel;
        }

        // -------------------- GET --------------------
        public async Task<PagedResult<Contract>> GetAllAsync(QueryObject query)
        {
            var contracts = _context.Contracts
                .Include(c => c.Person)
                .Include(c => c.Product)
                .Include(c => c.BeneficiaryClause)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                contracts = contracts.Where(c =>
                    c.ContractNumber.Contains(query.Search) ||
                    c.ContractLabel.Contains(query.Search));
            }

            if (query.ProductId.HasValue)
                contracts = contracts.Where(c => c.ProductId == query.ProductId.Value);

            contracts = query.SortBy switch
            {
                "ContractNumber" => query.IsDescending ? contracts.OrderByDescending(c => c.ContractNumber) : contracts.OrderBy(c => c.ContractNumber),
                "ContractLabel" => query.IsDescending ? contracts.OrderByDescending(c => c.ContractLabel) : contracts.OrderBy(c => c.ContractLabel),
                "CreatedDate" => query.IsDescending ? contracts.OrderByDescending(c => c.CreatedDate) : contracts.OrderBy(c => c.CreatedDate),
                _ => contracts.OrderByDescending(c => c.CreatedDate)
            };

            var totalCount = await contracts.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);
            var skipNumber = (query.PageNumber - 1) * query.PageSize;

            return new PagedResult<Contract>
            {
                Items = await contracts.Skip(skipNumber).Take(query.PageSize).ToListAsync(),
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = query.PageNumber < totalPages,
                CurrentPage = query.PageNumber
            };
        }

        public async Task<Contract?> GetByIdAsync(int id)
            => await LoadContractById(id);

        // -------------------- UPDATE --------------------
        public async Task<Contract?> UpdateAsync(int id, UpdateContractRequestDto updateContractDto)
        {
            var existingContract = await _context.Contracts
                .Include(c => c.InsuredLinks)
                .Include(c => c.Options)
                .Include(c => c.Documents)
                .Include(c => c.Compartments)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (existingContract == null)
                return null;

            var original = new Contract();
            original = existingContract;

            var updatedContract = updateContractDto.ToContractFromUpdateDto();

            // ✅ Mise à jour des champs scalaires
            existingContract.ContractNumber = updatedContract.ContractNumber;
            existingContract.ContractLabel = updatedContract.ContractLabel;
            existingContract.ContractType = updatedContract.ContractType;
            existingContract.Status = updatedContract.Status;
            existingContract.DateSign = updatedContract.DateSign;
            existingContract.DateEffect = updatedContract.DateEffect;
            existingContract.DateMaturity = updatedContract.DateMaturity;
            existingContract.PostalAddress = updatedContract.PostalAddress;
            existingContract.TaxAddress = updatedContract.TaxAddress;
            existingContract.Currency = updatedContract.Currency;
            existingContract.PersonId = updatedContract.PersonId;
            existingContract.InitialPremium = updatedContract.InitialPremium;
            existingContract.TotalPaidPremiums = updatedContract.TotalPaidPremiums;
            existingContract.RedemptionValue = updatedContract.RedemptionValue;
            existingContract.PaymentMode = updatedContract.PaymentMode;
            existingContract.ScheduledPayment = updatedContract.ScheduledPayment;
            existingContract.BeneficiaryClauseId = updatedContract.BeneficiaryClauseId;
            existingContract.EntryFeesRate = updatedContract.EntryFeesRate;
            existingContract.ManagementFeesRate = updatedContract.ManagementFeesRate;
            existingContract.ExitFeesRate = updatedContract.ExitFeesRate;
            existingContract.AdvisorComment = updatedContract.AdvisorComment;
            existingContract.HasAlert = updatedContract.HasAlert;
            existingContract.ExternalReference = updatedContract.ExternalReference;
            existingContract.LastModifiedByUserId = updatedContract.LastModifiedByUserId;
            existingContract.UpdatedDate = DateTime.UtcNow;

            // 🚫 Ne pas supprimer les compartiments ou FSA : ils sont gérés par les opérations
            _context.ContractInsuredPersons.RemoveRange(existingContract.InsuredLinks);
            _context.ContractOptions.RemoveRange(existingContract.Options);
            _context.Documents.RemoveRange(existingContract.Documents);
            await _context.SaveChangesAsync();

            // Réinsertion des enfants simples
            existingContract.InsuredLinks = updateContractDto.InsuredPersonIds
                .Select(pid => new ContractInsuredPerson { Contract = existingContract, PersonId = pid })
                .ToList();

            existingContract.Options = updateContractDto.Options
                .Select(opt => new ContractOption
                {
                    Contract = existingContract,
                    ContractOptionTypeId = opt.ContractOptionTypeId,
                    Description = opt.Description,
                    IsActive = opt.IsActive,
                    CustomParameters = opt.CustomParameters
                }).ToList();

            existingContract.Documents = updateContractDto.Documents
                .Select(d => new Document
                {
                    Contract = existingContract,
                    FileName = d.FileName,
                    FileType = d.FileType,
                    Url = d.Url,
                    UploadedAt = d.UploadedAt
                }).ToList();

            await _entityHistoryService.TrackChangesAsync(original, existingContract, "Admin");
            await _context.SaveChangesAsync();

            return await LoadContractById(id);
        }

        // -------------------- DELETE --------------------
        public async Task<Contract?> DeleteAsync(int id)
        {
            try
            {
                var contract = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == id);
                if (contract == null) return null;

                _context.Contracts.Remove(contract);
                await _context.SaveChangesAsync();
                return contract;
            }
            catch (DbUpdateException ex) when ((ex.InnerException as SqlException)?.Number == 547)
            {
                throw new InvalidOperationException("Impossible de supprimer ce contrat car il est référencé ailleurs.", ex);
            }
        }

        // -------------------- PATCH --------------------
        public async Task<Contract?> PatchBeneficiaryClauseIdAsync(int contractId, int clauseId)
        {
            var contract = await _context.Contracts.FindAsync(contractId);
            if (contract == null) return null;

            contract.BeneficiaryClauseId = clauseId;
            contract.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return contract;
        }

        public async Task<Contract?> PatchLockedAsync(int id, bool locked)
        {
            var contract = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == id);
            if (contract == null) return null;

            var original = contract;
            contract.Locked = locked;
            contract.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _entityHistoryService.TrackChangesAsync(original, contract, "Admin");
            return contract;
        }

        // -------------------- COUNT --------------------
        public async Task<int> CountRowsAsync() => await _context.Contracts.CountAsync();
        public async Task<int> CountContractsByProductIdAsync(int productId) =>
            await _context.Contracts.Where(c => c.ProductId == productId).CountAsync();

        // -------------------- LOAD CONTRACT --------------------
        public async Task<Contract?> LoadContractById(int id)
        {
            var contract = await _context.Contracts
                .Where(c => c.Id == id)
                .Include(c => c.Person)
                .Include(c => c.Product)
                .Include(c => c.BeneficiaryClause)
                .Include(c => c.Options).ThenInclude(o => o.ContractOptionType)
                .Include(c => c.Documents)
                .Include(c => c.Compartments)
                .Include(c => c.ContractSupportHoldings)
                    .ThenInclude(h => h.Support)
                .Include(c => c.Operations)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (contract == null)
                return null;

            // 🔹 Charger toutes les FSA
            var allocations = await _context.FinancialSupportAllocations
                .Where(fsa => fsa.ContractId == contract.Id)
                .Include(fsa => fsa.Support)
                .AsNoTracking()
                .ToListAsync();

            // 🔹 Reconstruction supports par compartiment (SOURCE DE VÉRITÉ = FSA)
            foreach (var comp in contract.Compartments)
            {
                comp.Supports = allocations
                    .Where(f => f.CompartmentId == comp.Id)
                    .ToList();
            }

            // 🔹 Nettoyage compartiments
            contract.Compartments = contract.Compartments
                .GroupBy(c => c.Id)
                .Select(g => g.First())
                .OrderByDescending(c => c.IsDefault)
                .ThenBy(c => c.Label)
                .ToList();

            // =====================================================
            // 🔥 VUE CONSOLIDÉE BASÉE UNIQUEMENT SUR HOLDINGS
            // =====================================================

            contract.Supports = contract.ContractSupportHoldings
                .Select(h => new FinancialSupportAllocation
                {
                    ContractId = contract.Id,
                    SupportId = h.SupportId,
                    Support = h.Support,
                    CurrentShares = h.TotalShares,
                    CurrentAmount = h.CurrentAmount ?? 0m,
                    InvestedAmount = h.TotalInvested,
                    Pru = h.Pru,
                    Performance = h.PerformancePercent ?? 0m,
                    CompartmentId = null
                })
                .OrderByDescending(s => s.CurrentAmount)
                .ToList();

            // =====================================================
            // 🔹 Enrichissement PRU / Perf dans les compartiments
            // (SANS toucher à InvestedAmount)
            // =====================================================

            var holdingBySupportId = contract.ContractSupportHoldings
                .ToDictionary(h => h.SupportId);

            foreach (var comp in contract.Compartments)
            {
                foreach (var s in comp.Supports)
                {
                    if (holdingBySupportId.TryGetValue(s.SupportId, out var holding))
                    {
                        s.Pru = holding.Pru;
                        s.Performance = holding.PerformancePercent ?? 0m;
                        // ⚠️ NE PAS TOUCHER s.InvestedAmount
                    }
                    else
                    {
                        s.Pru = null;
                        s.Performance = 0m;
                    }
                }
            }

            // =====================================================
            // 🔹 Agrégation UX uniquement
            // =====================================================

            contract.WithdrawnExecuted = contract.Operations
                .Where(o => o.Status == OperationStatus.Executed &&
                            (o.Type == OperationType.PartialWithdrawal ||
                             o.Type == OperationType.TotalWithdrawal))
                .Sum(o => o.Amount ?? 0m);

            contract.WithdrawnPending = contract.Operations
                .Where(o => o.Status == OperationStatus.Pending &&
                            (o.Type == OperationType.PartialWithdrawal ||
                             o.Type == OperationType.TotalWithdrawal))
                .Sum(o => o.Amount ?? 0m);

            contract.PaidExecuted = contract.Operations
                .Where(o => o.Status == OperationStatus.Executed &&
                            (o.Type == OperationType.InitialPayment ||
                             o.Type == OperationType.FreePayment ||
                             o.Type == OperationType.ScheduledPayment))
                .Sum(o => o.Amount ?? 0m);

            contract.PaidPending = contract.Operations
                .Where(o => o.Status == OperationStatus.Pending &&
                            (o.Type == OperationType.InitialPayment ||
                             o.Type == OperationType.FreePayment ||
                             o.Type == OperationType.ScheduledPayment))
                .Sum(o => o.Amount ?? 0m);

            return contract;
        }

        public async Task<IEnumerable<FinancialSupportAllocation>> GetAvailableSupportsAsync(int contractId, int? compartmentId)
        {
            Console.WriteLine($"🔎 Récupération des supports disponibles pour le contrat {contractId}" +
                              (compartmentId.HasValue ? $" (compartiment {compartmentId})" : " (tous compartiments)"));

            // Vérifie existence du contrat
            var contractExists = await _context.Contracts.AnyAsync(c => c.Id == contractId);
            if (!contractExists)
            {
                Console.WriteLine($"❌ Contrat {contractId} introuvable.");
                return Enumerable.Empty<FinancialSupportAllocation>();
            }

            // ==========================================================
            // 🟩 CAS 1 : un compartiment spécifique est demandé
            // ==========================================================
            if (compartmentId.HasValue && compartmentId > 0)
            {
                var compSupports = await _context.FinancialSupportAllocations
                    .Where(fsa => fsa.ContractId == contractId && fsa.CompartmentId == compartmentId)
                    .Include(fsa => fsa.Support)
                    .ToListAsync();

                if (!compSupports.Any())
                {
                    Console.WriteLine($"⚠️ Aucun support trouvé pour le contrat {contractId} / compartiment {compartmentId}");
                    return Enumerable.Empty<FinancialSupportAllocation>();
                }

                var result = compSupports
                    .Where(s => s.Support != null)
                    .Select(s => new FinancialSupportAllocation
                    {
                        ContractId = s.ContractId,
                        CompartmentId = s.CompartmentId,
                        SupportId = s.SupportId,
                        Support = s.Support,
                        CurrentShares = s.CurrentShares,
                        CurrentAmount = s.CurrentAmount,
                        Compartments = s.CompartmentId.HasValue
                            ? new List<int> { s.CompartmentId.Value }
                            : new List<int>(),
                        IsMultiCompartment = false,
                        CreatedDate = s.CreatedDate,
                        UpdatedDate = s.UpdatedDate
                    })
                    .OrderByDescending(s => s.CurrentAmount)
                    .ToList();

                Console.WriteLine($"📊 {result.Count} supports trouvés pour le compartiment {compartmentId}");
                return result;
            }

            // ==========================================================
            // 🟦 CAS 2 : consolidation de tous les compartiments du contrat
            // ==========================================================
            var allAllocations = await _context.FinancialSupportAllocations
                .Where(fsa => fsa.ContractId == contractId)
                .Include(fsa => fsa.Support)
                .ToListAsync();

            if (!allAllocations.Any())
            {
                Console.WriteLine($"⚠️ Aucun support valorisé pour le contrat {contractId}");
                return Enumerable.Empty<FinancialSupportAllocation>();
            }

            var consolidated = allAllocations
                .Where(s => s.Support != null)
                .GroupBy(s => s.SupportId)
                .Select(g =>
                {
                    var first = g.First();
                    var compartments = g
                        .Select(x => x.CompartmentId)
                        .Where(cid => cid.HasValue)
                        .Select(cid => cid!.Value)
                        .Distinct()
                        .ToList();

                    return new FinancialSupportAllocation
                    {
                        ContractId = contractId,
                        SupportId = first.SupportId,
                        Support = first.Support!,
                        CurrentShares = g.Sum(x => x.CurrentShares),
                        CurrentAmount = g.Sum(x => x.CurrentAmount),
                        Compartments = compartments,
                        IsMultiCompartment = compartments.Count > 1,
                        CompartmentId = null,
                        CreatedDate = first.CreatedDate,
                        UpdatedDate = first.UpdatedDate
                    };
                })
                .OrderByDescending(x => x.CurrentAmount)
                .ToList();

            Console.WriteLine($"📊 Vue consolidée calculée : {consolidated.Count} supports regroupés (contrat {contractId})");

            return consolidated;
        }

        public async Task<Contract?> UpdateCurrentValueAsync(int contractId, decimal newValue)
        {
            var contract = await _context.Contracts.FindAsync(contractId);
            if (contract == null) return null;

            contract.CurrentValue = newValue;
            contract.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return contract;
        }

        public async Task<List<ContractSupportHoldingDto>> GetHoldingsByContractAsync(int contractId)
        {
            var holdings = await _context.ContractSupportHoldings
                .Include(h => h.Support)
                .Where(h => h.ContractId == contractId)
                .ToListAsync();

            var result = new List<ContractSupportHoldingDto>();

            foreach (var h in holdings)
            {
                var vl = h.Support?.LastValuationAmount ?? 0m;

                // 🔍 Nettoyage
                var pru = Math.Round(h.Pru, 7);
                var shares = Math.Round(h.TotalShares, 7);
                var invested = Math.Round(h.TotalInvested, 7);

                // 💰 Valeur actuelle (la vraie !)
                var currentValue = Math.Round(shares * vl, 7);

                // 📈 Performance : (VL / PRU - 1) × 100
                decimal performancePercent = 0m;
                if (pru > 0 && vl > 0)
                {
                    performancePercent = Math.Round(((vl - pru) / pru) * 100m, 4);
                }

                Console.WriteLine(
                    $"[Holding] {h.Support?.ISIN} | PRU={pru} | VL={vl} | Parts={shares} | Value={currentValue} | Perf={performancePercent}%");

                // 🔄 Ajout DTO
                result.Add(new ContractSupportHoldingDto
                {
                    SupportId = h.SupportId,
                    SupportLabel = h.Support?.Label ?? "",
                    ISIN = h.Support?.ISIN ?? "",
                    Vl = vl,
                    Pru = pru,
                    TotalShares = shares,
                    TotalInvested = invested,
                    CurrentValue = currentValue,
                    PerformancePercent = performancePercent,
                    LastUpdated = h.Support?.UpdatedDate
                });
            }

            return result;
        }

        public void DetachAllEntities()
        {
            var entries = _context.ChangeTracker.Entries().ToList();
            foreach (var entry in entries)
                entry.State = EntityState.Detached;
        }

        // ==========================================================
        // 🔹 Façade de recalcul complet (Controller → Repository → Service)
        // ==========================================================
        public async Task<object?> RecalculateAndLoadAsync(int contractId, IContractValuationService valuationService)
        {
            Console.WriteLine($"🧩 [Repo] Début du recalcul complet pour contrat {contractId}");

            // 1️⃣ Calcul de la valeur via le service métier centralisé
            var totalValue = await valuationService.ComputeContractValueAsync(contractId);

            // 2️⃣ Recharge du contrat complet depuis la base (avec CurrentValue à jour)
            var contract = await LoadContractById(contractId);
            if (contract == null)
            {
                Console.WriteLine($"❌ Contrat {contractId} introuvable après recalcul.");
                return null;
            }

            // 3️⃣ Construction de la réponse synthétique pour le front / Swagger
            var compartments = (contract.Compartments ?? new List<Compartment>())
                .Select(c => new
                {
                    CompartmentId = c.Id,
                    Label = c.Label,
                    CurrentValue = c.CurrentValue
                })
                .ToList();

            var globalSupportsValue = (contract.Supports ?? Enumerable.Empty<FinancialSupportAllocation>())
                .Where(s => s.CompartmentId == null)
                .Sum(s => s.CurrentAmount);

            var result = new
            {
                contractId = contract.Id,
                contractNumber = contract.ContractNumber,
                currentValue = contract.CurrentValue,
                totalPayments = contract.TotalPayments,
                totalWithdrawals = contract.TotalWithdrawals,
                netInvested = contract.NetInvested,
                compartments,
                globalSupportsValue
            };

            Console.WriteLine($"✅ [Repo] Contrat {contract.ContractNumber} recalculé : {contract.CurrentValue:F2} €");
            return result;
        }

        // ==========================================================
        // 🔹 RecalculateValueAsync – version verrouillée et cohérente
        // ==========================================================
        private static readonly SemaphoreSlim _recalcLock = new(1, 1);

        public async Task<object?> RecalculateValueAsync(int id, IContractValuationService valuationService, string source = "Unknown")
        {
            await _recalcLock.WaitAsync();
            try
            {
                Console.WriteLine($"🔁 [RecalculateValue] Début recalcul pour contrat {id} (source: {source})");

                // 1️⃣ Calcul complet et persistance via le service métier
                var totalValue = await valuationService.ComputeContractValueAsync(id);
                Console.WriteLine($"💾 Valeur totale calculée et mise à jour en base = {totalValue:F2} €");

                // 2️⃣ Recharge du contrat depuis la base après commit
                var contract = await _context.Contracts
                    .Include(c => c.Person)
                    .Include(c => c.Product)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (contract == null)
                {
                    Console.WriteLine($"❌ Contrat {id} introuvable après recalcul.");
                    return new { message = $"Contrat {id} introuvable après recalcul." };
                }

                await _context.Entry(contract).ReloadAsync();
                Console.WriteLine($"🔁 Contrat {id} relu depuis SQL : CurrentValue={contract.CurrentValue:F2}€, Perf={contract.PerformancePercent:F2}%");

                // 3️⃣ Rechargement complet avec relations
                var fullContract = await LoadContractById(id);
                if (fullContract == null)
                {
                    Console.WriteLine($"❌ Contrat {id} introuvable après recalcul (détails).");
                    return new { message = $"Contrat {id} introuvable après recalcul." };
                }

                // ========================================================================
                // 🔹 Net investi exécuté (hors opérations en attente de dénouement)
                // ========================================================================
                var netInvestedExecuted =
                    fullContract.PaidExecuted
                  - fullContract.WithdrawnExecuted;

                decimal performanceExecuted = 0m;

                if (netInvestedExecuted > 0)
                {
                    performanceExecuted = Math.Round(
                        (fullContract.CurrentValue - netInvestedExecuted)
                        / netInvestedExecuted
                        * 100m,
                        2
                    );
                }

                // 4️⃣ Préparation DTO synthétique
                var compartments = (fullContract.Compartments ?? new List<Compartment>())
                    .Select(c => new
                    {
                        CompartmentId = c.Id,
                        Label = c.Label,
                        CurrentValue = c.CurrentValue
                    })
                    .ToList();

                var globalSupportsValue = (fullContract.Supports ?? Enumerable.Empty<FinancialSupportAllocation>())
                    .Where(s => s.CompartmentId == null)
                    .Sum(s => s.CurrentAmount);

                var response = new
                {
                    contractId = fullContract.Id,
                    contractNumber = fullContract.ContractNumber,
                    currentValue = fullContract.CurrentValue,
                    totalPayments = fullContract.TotalPayments,
                    totalWithdrawals = fullContract.TotalWithdrawals,
                    netInvested = netInvestedExecuted,
                    performancePercent = performanceExecuted,
                    initialPremium = fullContract.InitialPremium,
                    compartments,
                    globalSupportsValue
                };

                Console.WriteLine(
                    $"✅ [RecalculateValue] Contrat {fullContract.ContractNumber} ({id}) recalculé avec succès : " +
                    $"Valeur = {fullContract.CurrentValue:F2} €, " +
                    $"Perf exécutée = {performanceExecuted:F2} % (source: {source})"
                );

                return response;
            }
            finally
            {
                _recalcLock.Release();
            }
        }

        public async Task<object?> RebuildHoldingsAsync(int contractId)
        {
            Console.WriteLine($"🔄 Reconstruction complète des holdings pour contrat {contractId}");

            // 1️⃣ Supprimer les holdings existants
            var oldHoldings = await _context.ContractSupportHoldings
                .Where(h => h.ContractId == contractId)
                .ToListAsync();

            _context.ContractSupportHoldings.RemoveRange(oldHoldings);
            await _context.SaveChangesAsync();

            // 2️⃣ Récupérer les opérations exécutées
            var executedOps = await _context.Operations
                .Include(o => o.Allocations)
                .ThenInclude(a => a.Support)
                .Where(o => o.ContractId == contractId && o.Status == OperationStatus.Executed)
                .OrderBy(o => o.OperationDate)
                .ToListAsync();

            // 3️⃣ Rejouer chaque opération exécutée
            foreach (var op in executedOps)
            {
                await _operationEngineService.ApplyOperationAsync(op);
            }

            // 4️⃣ Revaloriser le contrat
            await _valuationService.ComputeContractValueAsync(contractId);

            // 5️⃣ Recharger le contrat
            return await LoadContractById(contractId);
        }

        private decimal ComputeNetInvested(int contractId, int supportId, int? compartmentId = null)
        {
            var query = _context.OperationSupportAllocations
                .Include(a => a.Operation)
                .Where(a =>
                    a.SupportId == supportId &&
                    a.Operation != null &&
                    a.Operation.ContractId == contractId &&
                    a.Operation.Status == OperationStatus.Executed   // 🔥 CLÉ
                );

            // 🔎 Filtrage compartiment si nécessaire
            if (compartmentId.HasValue)
                query = query.Where(a => a.CompartmentId == compartmentId.Value);

            var allocations = query.ToList();

            // Versements (cash entrant)
            var depositTypes = new[]
            {
        OperationType.InitialPayment,
        OperationType.ScheduledPayment,
        OperationType.FreePayment,
        OperationType.ParticipationBenefit,
        OperationType.InterestPayment,
        OperationType.CouponDetachment
    };

            // Retraits / frais (cash sortant)
            var withdrawalTypes = new[]
            {
        OperationType.PartialWithdrawal,
        OperationType.ScheduledWithdrawal,
        OperationType.TotalWithdrawal,
        OperationType.ManagementFee,
        OperationType.OperationFee
    };

            var deposits = allocations
                .Where(a => depositTypes.Contains(a.Operation!.Type))
                .Sum(a => a.Amount ?? 0m);

            var withdrawals = allocations
                .Where(a => withdrawalTypes.Contains(a.Operation!.Type))
                .Sum(a => a.Amount ?? 0m);

            return Math.Round(deposits - withdrawals, 7);
        }

    }
}

// public async Task<Contract?> LoadContractById(int id)
// {
//     var contract = await _context.Contracts
//         .Where(c => c.Id == id)
//         .Include(c => c.Person)
//         .Include(c => c.Product)
//         .Include(c => c.BeneficiaryClause)
//         .Include(c => c.Options).ThenInclude(o => o.ContractOptionType)
//         .Include(c => c.Documents)
//         .Include(c => c.Compartments)
//             .ThenInclude(comp => comp.Supports)
//                 .ThenInclude(s => s.Support)
//         .Include(c => c.Supports)
//             .ThenInclude(s => s.Support)
//         .Include(c => c.ContractSupportHoldings)
//             .ThenInclude(h => h.Support)
//         .Include(c => c.Operations)
//         .AsNoTracking()
//         .FirstOrDefaultAsync();

//     if (contract == null)
//     {
//         Console.WriteLine($"❌ Contrat {id} introuvable.");
//         return null;
//     }

//     Console.WriteLine($"➡️ Chargement contrat {contract.Id} - {contract.ContractNumber}");

//     var holdingBySupportId = contract.ContractSupportHoldings
//         .ToDictionary(h => h.SupportId);


//     // 2️⃣ Récupération de toutes les allocations (globaux + compartiments)
//     var allAllocations = await _context.FinancialSupportAllocations
//         .Where(fsa => fsa.ContractId == contract.Id)
//         .Include(fsa => fsa.Support)
//         .AsNoTracking()
//         .ToListAsync();

//     // Synchronisation navigation
//     foreach (var comp in contract.Compartments)
//         comp.Supports = allAllocations.Where(f => f.CompartmentId == comp.Id).ToList();

//     // Suppression des doublons
//     foreach (var comp in contract.Compartments)
//         comp.Supports = comp.Supports.GroupBy(s => s.SupportId).Select(g => g.First()).ToList();

//     // 3️⃣ Calculs
//     // Après calculs :
//     decimal totalContractValue = 0m;
//     // ========================================================================
//     // 🔥 PATCH — Reconstruction correcte des supports par compartiment
//     //     + Ajout du montant investi par support & compartiment
//     // ========================================================================

//     // On récupère toutes les allocations de supports (déjà chargées dans allAllocations)
//     foreach (var comp in contract.Compartments)
//     {
//         // 1) Sélection des supports appartenant à ce compartiment
//         comp.Supports = allAllocations
//             .Where(f => f.CompartmentId == comp.Id)
//             .ToList();

//         // 2) Suppression des doublons (un support peut apparaitre plusieurs fois)
//         comp.Supports = comp.Supports
//             .GroupBy(s => s.SupportId)
//             .Select(g => g.First())
//             .ToList();

//         // 3) Enrichissement INVESTED par support pour ce compartiment
//         foreach (var s in comp.Supports)
//         {
//             if (holdingBySupportId.TryGetValue(s.SupportId, out var holding))
//             {
//                 s.InvestedAmount = Math.Round(holding.TotalInvested, 7);
//                 s.Pru = holding.Pru;
//             }
//             else
//             {
//                 s.InvestedAmount = 0m;
//                 s.Pru = null;
//             }

//             if (s.InvestedAmount > 0)
//             {
//                 s.Performance = Math.Round(
//                     (s.CurrentAmount - s.InvestedAmount) / s.InvestedAmount,
//                     6
//                 );
//             }
//             else
//             {
//                 s.Performance = 0m;
//             }
//         }


//         // 4) Valeur actuelle du compartiment = somme des CurrentAmount
//         comp.CurrentValue = Math.Round(
//             comp.Supports.Sum(s => s.CurrentAmount),
//             7
//         );

//         // 5) Total investi du compartiment
//         comp.TotalInvested = Math.Round(
//             comp.Supports.Sum(s => s.InvestedAmount),
//             7
//         );
//     }


//     // ✅ Nettoyage : un seul "Global" et suppression des doublons EF
//     contract.Compartments = contract.Compartments
//         .GroupBy(c => c.Id)
//         .Select(g => g.First())
//         .OrderByDescending(c => c.IsDefault)
//         .ThenBy(c => c.Label)
//         .ToList();

//     var globalComp = contract.Compartments.FirstOrDefault(c => c.IsDefault || c.Label.ToLower() == "global");
//     if (globalComp != null)
//         contract.Supports = globalComp.Supports;

//     Console.WriteLine($"💰 Total = {totalContractValue:F2}€");
//     Console.WriteLine($"📊 Vue consolidée calculée : {contract.Compartments.Count} compartiments.");

//     // ========================================================================
//     // 🔹 Agrégation des retraits : Executed vs Pending (UX only)
//     // ========================================================================

//     var withdrawalOperations = contract.Operations
//         .Where(o =>
//             o.Type == OperationType.PartialWithdrawal ||
//             o.Type == OperationType.TotalWithdrawal)
//         .ToList();

//     var withdrawnExecuted = withdrawalOperations
//         .Where(o => o.Status == OperationStatus.Executed)
//         .Sum(o => o.Amount);

//     var withdrawnPending = withdrawalOperations
//         .Where(o => o.Status == OperationStatus.Pending)
//         .Sum(o => o.Amount);

//     // 👉 On stocke ces infos sur le contrat (ou futur DTO)
//     contract.WithdrawnExecuted = Math.Round(withdrawnExecuted ?? 0m, 7);
//     contract.WithdrawnPending = Math.Round(withdrawnPending ?? 0m, 7);

//     // ========================================================================
//     // 🔹 Agrégation des versements : Executed vs Pending (UX only)
//     // ========================================================================

//     var paymentOperations = contract.Operations
//         .Where(o =>
//             o.Type == OperationType.InitialPayment ||
//             o.Type == OperationType.FreePayment ||
//             o.Type == OperationType.ScheduledPayment)
//         .ToList();

//     var paidExecuted = paymentOperations
//         .Where(o => o.Status == OperationStatus.Executed)
//         .Sum(o => o.Amount);

//     var paidPending = paymentOperations
//         .Where(o => o.Status == OperationStatus.Pending)
//         .Sum(o => o.Amount);

//     contract.PaidExecuted = Math.Round(paidExecuted ?? 0m, 7);
//     contract.PaidPending = Math.Round(paidPending ?? 0m, 7);

//     // ========================================================================
//     // 🔹 Net investi EFFECTIF (hors opérations en attente)
//     // ========================================================================

//     contract.NetInvested = Math.Round(
//         contract.PaidExecuted
//         - contract.WithdrawnExecuted,
//         7
//     );

//     // ========================================================================
//     // 🔹 Performance EFFECTIVE (hors opérations en attente de dénouement)
//     // ========================================================================

//     decimal performanceEffective = 0m;

//     if (contract.NetInvested > 0)
//     {
//         performanceEffective = Math.Round(
//             ((contract.CurrentValue / contract.NetInvested) - 1m) * 100m,
//             4
//         );
//     }

//     // ⚠️ ÉCRASEMENT VOLONTAIRE POUR L’UX
//     contract.PerformancePercent = performanceEffective;

//     return contract;

// }
