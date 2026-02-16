using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.FinancialSupport;
using Microsoft.EntityFrameworkCore;
using api.Interfaces;
using api.Models;
using AutoMapper;
using api.Data;

namespace api.Services
{
    public class FinancialSupportImportService : IFinancialSupportImportService
    {
        private readonly IYahooFinanceProvider _yahooProvider;
        private readonly IEodDataProvider _eodProvider;
        private readonly ITwelveDataProvider _twelveProvider;
        private readonly IFinancialSupportRepository _supportRepo;
        private readonly ISupportHistoricalDataRepository _historyRepo;
        private readonly IContractSupportHoldingRepository _holdingRepo;
        private readonly IServiceProvider _serviceProvider;
        private readonly IContractRepository _contractRepository;
        private readonly IContractValuationService _valuationService;
        private readonly IMapper _mapper;

        public FinancialSupportImportService(
            IYahooFinanceProvider yahooProvider,
            IEodDataProvider eodProvider,
            ITwelveDataProvider twelveProvider,
            IFinancialSupportRepository supportRepo,
            ISupportHistoricalDataRepository historyRepo,
            IContractSupportHoldingRepository holdingRepo,
            IServiceProvider serviceProvider,
            IContractRepository contractRepository,
            IContractValuationService valuationService,
            IMapper mapper)
        {
            _yahooProvider = yahooProvider;
            _eodProvider = eodProvider;
            _twelveProvider = twelveProvider;
            _supportRepo = supportRepo;
            _historyRepo = historyRepo;
            _holdingRepo = holdingRepo;
            _serviceProvider = serviceProvider;
            _contractRepository = contractRepository;
            _valuationService = valuationService;
            _mapper = mapper;
        }

        public async Task ImportFromYahooAsync(string ticker)
        {
            DebugLogger.Log($"[YahooFinance] ImportFromYahooAsync: ticker={ticker}");
            var dto = await _yahooProvider.GetEtfDataAsync(ticker);
            var existing = await _supportRepo.GetByCodeAsync(dto.Ticker);
            FinancialSupportDto supportDto;

            if (existing == null)
            {
                var createDto = _mapper.Map<CreateFinancialSupportRequestDto>(dto);
                supportDto = await _supportRepo.CreateAsync(createDto);
                DebugLogger.Log($"[YahooFinance] Created support {dto.Ticker}");
            }
            else
            {
                var updateDto = _mapper.Map<UpdateFinancialSupportRequestDto>(dto);
                var updatedDto = await _supportRepo.UpdateAsync(existing.Id, updateDto);
                supportDto = updatedDto ?? existing;
                DebugLogger.Log($"[YahooFinance] Updated support {dto.Ticker}");
            }

            await _historyRepo.DeleteBySupportIdAsync(supportDto.Id);

            var historicals = dto.Historicals.Select(h => new SupportHistoricalData
            {
                FinancialSupportId = supportDto.Id,
                Date = h.Date,
                Nav = h.Nav
            }).ToList();

            await _historyRepo.InsertRangeAsync(historicals);
            DebugLogger.Log($"[YahooFinance] Inserted historicals for {dto.Ticker}");

            await _valuationService.UpdateFsaAmountsForSupportAsync(supportDto.Id);
            DebugLogger.Log($"[YahooFinance] 🔁 FSA mises à jour pour support {supportDto.Id} (VL × Shares)");

        }

        public async Task<List<HistoricalPrice>> ImportFromEodAsync(string ticker)
        {
            DebugLogger.Log($"[EOD] ImportFromEodAsync: ticker={ticker}");

            var from = DateTime.Today.AddMonths(-6);
            var to = DateTime.Today;

            // 🔹 Récupération des données de marché
            var result = await _eodProvider.GetHistoricalDataAsync(ticker, from, to);
            DebugLogger.Log($"[EOD] HistoricalData for {ticker}: count={result?.Count}");

            if (result == null || result.Count == 0)
            {
                DebugLogger.Log($"[EOD] Aucun historique trouvé pour {ticker}");
                return new List<HistoricalPrice>();
            }

            // 🔹 Détermination de la dernière VL (valeur liquidative)
            var lastPrice = result.OrderByDescending(p => p.Date).FirstOrDefault();
            var lastValuationAmount = lastPrice?.Close ?? 0;
            var lastValuationDate = lastPrice?.Date ?? DateTime.Today;

            // 🔹 Recherche du support existant correspondant au ticker
            var support = await _supportRepo.GetByCodeAsync(ticker);
            if (support == null)
            {
                DebugLogger.Log($"[EOD] Aucun support trouvé pour le ticker {ticker}. Aucun recalcul effectué.");
                return result;
            }

            // 🔹 Mise à jour du support avec la VL la plus récente
            var updateDto = new UpdateFinancialSupportRequestDto
            {
                LastValuationAmount = lastValuationAmount,
                LastValuationDate = lastValuationDate
            };

            await _supportRepo.UpdateAsync(support.Id, updateDto);
            DebugLogger.Log($"[EOD] ✅ SupportId={support.Id} mis à jour avec VL={lastValuationAmount}");

            // 🔁 Recalcule les montants FSA via ContractValuationService
            var countUpdated = await _valuationService.UpdateFsaAmountsForSupportAsync(support.Id);
            DebugLogger.Log($"[EOD] 🔁 {countUpdated} FSA mises à jour (CurrentAmount = VL × Shares)");

            // 🔁 Recalcul des contrats impactés
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
                var contractRepo = scope.ServiceProvider.GetRequiredService<IContractRepository>();
                var valuationService = scope.ServiceProvider.GetRequiredService<IContractValuationService>();

                var affectedContracts = await context.ContractSupportHoldings
                    .Where(h => h.SupportId == support.Id)
                    .Select(h => h.ContractId)
                    .Distinct()
                    .ToListAsync();

                foreach (var cid in affectedContracts)
                    await contractRepo.RecalculateValueAsync(cid, valuationService, source: "FinancialSupportImportService : ImportFromEodAsync");
            }

            DebugLogger.Log($"[EOD] ♻️ Recalcul complet terminé pour {ticker}");

            return result;
        }

        public async Task<object?> SearchSupportByIsinFromEodAsync(string isin)
        {
            DebugLogger.Log($"[EOD] SearchSupportByIsinFromEodAsync: isin={isin}");

            var ticker = await _eodProvider.FindTickerByIsinAsync(isin);
            DebugLogger.Log($"[EOD] FindTickerByIsinAsync: {ticker}");
            if (string.IsNullOrEmpty(ticker)) return null;

            // Récupération du profil et des données de cours
            var profile = await _eodProvider.GetFundProfileAsync(ticker);
            var prices = await _eodProvider.GetHistoricalDataAsync(
                ticker,
                DateTime.Today.AddMonths(-6),
                DateTime.Today
            );

            DebugLogger.Log($"[EOD] GetFundProfileAsync: {profile?.LegalName}");

            var lastPrice = prices?
                .OrderByDescending(p => p.Date)
                .FirstOrDefault();

            var lastValuationAmount = lastPrice?.Close ?? 0;
            var lastValuationDate = lastPrice?.Date ?? DateTime.Today;

            var dto = new FinancialSupportDto
            {
                Code = ticker,
                ISIN = (profile?.ISIN ?? isin)?.Trim().ToUpperInvariant(),
                Label = profile?.LegalName
                    ?? profile?.MarketingName
                    ?? profile?.Label
                    ?? profile?.Name
                    ?? $"Support récupéré depuis EOD [{ticker}]",
                Currency = profile?.Currency ?? "EUR",
                SupportType = profile?.SupportType ?? "ETF",
                MarketingName = profile?.MarketingName,
                LegalName = profile?.LegalName,
                AMFCode = profile?.AMFCode,
                BloombergCode = profile?.BloombergCode,
                MorningstarCode = profile?.MorningstarCode,
                CUSIP = profile?.CUSIP,
                SEDOL = profile?.SEDOL,
                Status = profile?.Status ?? "À valider",
                AssetManager = profile?.AssetManager,
                DepositaryBank = profile?.DepositaryBank,
                Custodian = profile?.Custodian,
                InceptionDate = profile?.InceptionDate,
                ClosureDate = profile?.ClosureDate,
                IsClosed = profile?.Status?.ToLowerInvariant() == "closed",
                AssetClass = profile?.AssetClass,
                SubAssetClass = profile?.SubAssetClass,
                GeographicFocus = profile?.GeographicFocus,
                SectorFocus = profile?.SectorFocus,
                CapitalizationPolicy = profile?.CapitalizationPolicy,
                InvestmentStrategy = profile?.InvestmentStrategy,
                LegalForm = profile?.LegalForm,
                ManagementStyle = profile?.ManagementStyle,
                UCITSCategory = profile?.UCITSCategory,
                MinimumSubscription = profile?.MinimumSubscription,
                MinimumHolding = profile?.MinimumHolding,
                ManagementFee = profile?.ManagementFee,
                PerformanceFee = profile?.PerformanceFee,
                TurnoverRate = profile?.TurnoverRate,
                AUM = profile?.AUM,
                IsCapitalGuaranteed = profile?.IsCapitalGuaranteed,
                IsCurrencyHedged = profile?.IsCurrencyHedged,
                Benchmark = profile?.Benchmark,
                HasESGLabel = profile?.HasESGLabel,
                ESGLabel = profile?.ESGLabel,
                SFDRClassification = profile?.SFDRClassification,
                ESGScore = profile?.ESGScore,
                ESGScoreProvider = profile?.ESGScoreProvider,
                MifidTargetMarket = profile?.MifidTargetMarket,
                MifidRiskTolerance = profile?.MifidRiskTolerance,
                MifidClientType = profile?.MifidClientType,

                // ✅ valeurs corrigées de VL
                LastValuationAmount = lastValuationAmount,
                LastValuationDate = lastValuationDate,

                WeeklyVolatility = profile?.WeeklyVolatility,
                MaxDrawdown1Y = profile?.MaxDrawdown1Y,
                Distributor = profile?.Distributor,
                IsAvailableOnline = profile?.IsAvailableOnline,
                IsAdvisedSale = profile?.IsAdvisedSale,
                IsEligiblePEA = profile?.IsEligiblePEA,
                CountryOfDistribution = profile?.CountryOfDistribution,
                FundDomicile = profile?.FundDomicile,
                PrimaryListingMarket = profile?.PrimaryListingMarket,
                IsFundOfFunds = profile?.IsFundOfFunds
            };

            DebugLogger.Log($"[EOD] Last valuation for {ticker}: {lastValuationAmount} on {lastValuationDate:yyyy-MM-dd}");
            return new { ticker, dto, prices };
        }

        // ===================================================================
        // 📌 IMPORT EOD PAR ISIN — VERSION CORRIGÉE (VL mise à jour correctement)
        // ===================================================================
        public async Task ImportFromEodByIsinAsync(int supportId, string isin)
        {
            DebugLogger.Log($"[EOD] ImportFromEodByIsinAsync: supportId={supportId}, ISIN={isin}");

            // 1️⃣ Recherche du ticker
            var ticker = await _eodProvider.FindTickerByIsinAsync(isin);
            DebugLogger.Log($"[EOD] FindTickerByIsinAsync(ISIN={isin}) => {ticker}");

            if (string.IsNullOrEmpty(ticker))
                throw new Exception($"Aucun ticker trouvé pour l'ISIN : {isin}");

            // 2️⃣ Historique
            var from = DateTime.Today.AddYears(-1);
            var to = DateTime.Today;

            var historicals = await _eodProvider.GetHistoricalDataAsync(ticker, from, to);
            DebugLogger.Log($"[EOD] HistoricalData fetched: {historicals?.Count} points for {ticker}");

            if (historicals == null || historicals.Count == 0)
                throw new Exception($"Aucun historique trouvé pour le ticker {ticker}");

            // 3️⃣ Dernière VL
            var lastPrice = historicals.OrderByDescending(h => h.Date).First();
            var lastValuationAmount = lastPrice.Close;
            var lastValuationDate = lastPrice.Date;

            DebugLogger.Log($"[EOD] Latest NAV = {lastValuationAmount} on {lastValuationDate:yyyy-MM-dd}");

            // 4️⃣ Import des historiques
            var entities = historicals.Select(h => new SupportHistoricalData
            {
                FinancialSupportId = supportId,
                Date = h.Date,
                Nav = h.Close,
                Close = h.Close,
                Open = h.Open,
                High = h.High,
                Low = h.Low,
                Volume = h.Volume
            }).ToList();

            await _historyRepo.DeleteBySupportIdAsync(supportId);
            await _historyRepo.InsertRangeAsync(entities);

            DebugLogger.Log($"[EOD] ✔ Historicals imported ({entities.Count} rows).");

            // ===================================================================
            // 5️⃣ MISE À JOUR VL — CORRIGÉE (via repository)
            // ===================================================================
            var updateDto = new UpdateFinancialSupportRequestDto
            {
                LastValuationAmount = lastValuationAmount,
                LastValuationDate = lastValuationDate
            };

            var updated = await _supportRepo.UpdateAsync(supportId, updateDto);

            if (updated == null)
                DebugLogger.Log($"❌ [EOD] Impossible de mettre à jour FinancialSupport {supportId}");
            else
                DebugLogger.Log($"[EOD] ✔ FinancialSupport updated via repo: VL={lastValuationAmount}, Date={lastValuationDate:yyyy-MM-dd}");

            // ===================================================================
            // 6️⃣ Mise à jour FSA
            // ===================================================================
            var countUpdated = await _valuationService.UpdateFsaAmountsForSupportAsync(supportId);
            DebugLogger.Log($"[EOD] ✔ {countUpdated} FSA updated.");

            // ===================================================================
            // 7️⃣ Recalcul des contrats impactés
            // ===================================================================
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
                var contractRepo = scope.ServiceProvider.GetRequiredService<IContractRepository>();
                var valuationService = scope.ServiceProvider.GetRequiredService<IContractValuationService>();

                var affectedContracts = await context.ContractSupportHoldings
                    .Where(h => h.SupportId == supportId)
                    .Select(h => h.ContractId)
                    .Distinct()
                    .ToListAsync();

                DebugLogger.Log($"[EOD] Contracts impacted: {string.Join(", ", affectedContracts)}");

                foreach (var cid in affectedContracts)
                    await contractRepo.RecalculateValueAsync(cid, valuationService,
                        source: "ImportFromEodByIsinAsync");

                DebugLogger.Log("[EOD] ✔ All impacted contracts recalculated.");
            }
        }

        public async Task<(CreateFinancialSupportRequestDto?, string?)> ImportFromYahooThenEodAsync(string isin)
        {
            DebugLogger.Log($"[Combined] ImportFromYahooThenEodAsync: isin={isin}");
            CreateFinancialSupportRequestDto? yahooDto = null;

            try
            {
                yahooDto = await _yahooProvider.GetBasicInfoByIsinAsync(isin);
                DebugLogger.Log($"[YahooFinance] GetBasicInfoByIsinAsync: {isin} => {(yahooDto != null ? "OK" : "NULL")}");
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"[YahooFinance] Exception GetBasicInfoByIsinAsync: {isin} => {ex}");
            }

            var ticker = await _eodProvider.FindTickerByIsinAsync(isin);
            DebugLogger.Log($"[EOD] FindTickerByIsinAsync: {isin} => {ticker}");

            if (ticker == null && yahooDto == null)
                return (null, null);

            var profile = ticker != null ? await _eodProvider.GetFundProfileAsync(ticker) : null;
            DebugLogger.Log($"[EOD] GetFundProfileAsync: {ticker} => {(profile != null ? "OK" : "NULL")}");

            if (profile != null)
            {
                if (yahooDto == null)
                {
                    if (ticker == null)
                    {
                        DebugLogger.Log("[Combined] Impossible de construire un DTO sans Yahoo ni ticker.");
                        throw new InvalidOperationException("Impossible de construire un DTO sans Yahoo ni ticker.");
                    }

                    yahooDto = new CreateFinancialSupportRequestDto
                    {
                        ISIN = isin,
                        Code = ticker
                    };
                }

                _mapper.Map(profile, yahooDto);
            }

            return (yahooDto, ticker);
        }

        public async Task<(CreateFinancialSupportRequestDto?, string?)> ImportFromTwelveAsync(string isin)
        {
            DebugLogger.Log($"[TwelveData] ImportFromTwelveAsync: isin={isin}");
            try
            {
                var symbol = await _twelveProvider.FindSymbolByIsinAsync(isin);
                DebugLogger.Log($"[TwelveData] FindSymbolByIsinAsync: {isin} => {symbol}");
                if (symbol == null) return (null, null);

                var price = await _twelveProvider.GetLastPriceAsync(symbol);
                DebugLogger.Log($"[TwelveData] GetLastPriceAsync: {symbol} => {price}");

                // 🔹 Recherche du support existant correspondant à l’ISIN
                // Le repository n'expose pas GetByIsinAsync : on tente par code (symbol) puis, à défaut, par ISIN via GetByCodeAsync pour éviter l'erreur de compilation.
                var existingSupport = await _supportRepo.GetByCodeAsync(symbol) ?? await _supportRepo.GetByCodeAsync(isin);
                if (existingSupport == null)
                {
                    DebugLogger.Log($"[TwelveData] Aucun support existant trouvé pour ISIN={isin} → création nécessaire.");
                    var dto = new CreateFinancialSupportRequestDto
                    {
                        ISIN = isin,
                        Code = symbol,
                        Label = $"Importé depuis TwelveData [{symbol}]",
                        Currency = "EUR",
                        LastValuationAmount = price,
                        LastValuationDate = DateTime.Today,
                        Status = "À valider"
                    };
                    return (dto, symbol);
                }

                // 🔹 Mise à jour de la dernière valorisation du support existant
                var updateDto = new UpdateFinancialSupportRequestDto
                {
                    LastValuationAmount = price,
                    LastValuationDate = DateTime.Today
                };

                await _supportRepo.UpdateAsync(existingSupport.Id, updateDto);
                DebugLogger.Log($"[TwelveData] ✅ SupportId={existingSupport.Id} mis à jour avec VL={price}");

                // ==========================================================
                // 🔁 Recalcule les montants FSA via ContractValuationService
                // ==========================================================
                var countUpdated = await _valuationService.UpdateFsaAmountsForSupportAsync(existingSupport.Id);
                DebugLogger.Log($"[TwelveData] 🔁 {countUpdated} FSA mises à jour (CurrentAmount = VL × Shares)");

                // ==========================================================
                // 🔁 Recalcul des contrats impactés
                // ==========================================================
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
                    var contractRepo = scope.ServiceProvider.GetRequiredService<IContractRepository>();
                    var valuationService = scope.ServiceProvider.GetRequiredService<IContractValuationService>();

                    var affectedContracts = await context.ContractSupportHoldings
                        .Where(h => h.SupportId == existingSupport.Id)
                        .Select(h => h.ContractId)
                        .Distinct()
                        .ToListAsync();

                    foreach (var cid in affectedContracts)
                        await contractRepo.RecalculateValueAsync(cid, valuationService, source: "FinancialSupportImportService : ImportFromTwelveAsync");
                }

                return (null, symbol);
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"[TwelveData] Exception ImportFromTwelveAsync: {isin} => {ex}");
                throw;
            }
        }

        public async Task<(CreateFinancialSupportRequestDto?, string?, string)> ImportFromAnyProviderAsync(string isin)
        {
            DebugLogger.Log($"[AnyProvider] ImportFromAnyProviderAsync: isin={isin}");

            try
            {
                var (twelveDto, twelveSymbol) = await ImportFromTwelveAsync(isin);
                if (twelveDto != null)
                {
                    DebugLogger.Log($"[AnyProvider] TwelveData OK: {twelveSymbol}");
                    return (twelveDto, twelveSymbol, "TwelveData");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"[AnyProvider] Exception TwelveData: {ex}");
            }

            try
            {
                var eodTicker = await _eodProvider.FindTickerByIsinAsync(isin);
                DebugLogger.Log($"[AnyProvider] EOD ticker: {eodTicker}");
                if (!string.IsNullOrEmpty(eodTicker))
                {
                    var eodProfile = await _eodProvider.GetFundProfileAsync(eodTicker);
                    DebugLogger.Log($"[AnyProvider] EOD profile: {(eodProfile != null ? "OK" : "NULL")}");
                    if (eodProfile != null)
                    {
                        var dto = _mapper.Map<CreateFinancialSupportRequestDto>(eodProfile);
                        dto.ISIN = (eodProfile.ISIN ?? isin)?.Trim().ToUpperInvariant();
                        dto.Code = eodTicker;
                        return (dto, eodTicker, "EOD");
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"[AnyProvider] Exception EOD: {ex}");
            }

            try
            {
                CreateFinancialSupportRequestDto? yahooDto = null;
                yahooDto = await _yahooProvider.GetBasicInfoByIsinAsync(isin);
                DebugLogger.Log($"[AnyProvider] YahooFinance: {(yahooDto != null ? "OK" : "NULL")}");
                if (yahooDto != null)
                    return (yahooDto, null, "YahooFinance");
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"[AnyProvider] Exception Yahoo: {ex}");
            }

            DebugLogger.Log($"[AnyProvider] Aucun provider trouvé pour ISIN {isin}");
            return (null, null, "None");
        }
    }
}