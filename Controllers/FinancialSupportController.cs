using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using api.Helpers;
using api.Dtos.FinancialSupport;
using api.Interfaces;
using api.Services;

namespace api.Controllers
{
    [Route("api/financialSupports")]
    [ApiController]
    public class FinancialSupportController : ControllerBase
    {
        private readonly FinancialSupportImportService _importService;
        private readonly IFinancialSupportRepository _financialSupportRepository;

        private readonly ISupportHistoricalDataRepository _historyRepo;
        private readonly IConfiguration _config;

        public FinancialSupportController(
            FinancialSupportImportService importService,
            IFinancialSupportRepository financialSupportRepository,
            ISupportHistoricalDataRepository historyRepo,
            IConfiguration config)
        {
            _importService = importService;
            _financialSupportRepository = financialSupportRepository;
            _historyRepo = historyRepo;
            _config = config;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] QueryObject query)
        {
            DebugLogger.Log($">>> Controller GetAll appelé avec QueryObject={query}");
            var result = await _financialSupportRepository.GetAllAsync(query);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            var dto = await _financialSupportRepository.GetByIdAsync(id); // <-- Correction ici
            if (dto == null) return NotFound();
            return Ok(dto);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateFinancialSupportRequestDto createDto)
        {
            // Nettoyage ISIN pour comparer de façon robuste
            var isin = createDto.ISIN?.Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(isin))
            {
                return BadRequest("ISIN requis");
            }

            // Vérification unicité ISIN
            var exists = await _financialSupportRepository.AnyByIsinAsync(isin);
            if (exists)
            {
                // Retourne une erreur REST standard : 409 Conflict
                return Conflict($"Un support avec l’ISIN {isin} existe déjà.");
            }

            var dto = await _financialSupportRepository.CreateAsync(createDto);
            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }


        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateFinancialSupportRequestDto updateDto)
        {
            var dto = await _financialSupportRepository.UpdateAsync(id, updateDto);
            if (dto == null) return NotFound();
            return Ok(dto);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            var dto = await _financialSupportRepository.DeleteAsync(id);
            if (dto == null) return NotFound();
            return Ok(dto);
        }

        [HttpPost("import/yahoo/{ticker}")]
        public async Task<IActionResult> ImportFromYahoo(string ticker)
        {
            await _importService.ImportFromYahooAsync(ticker);
            return Ok("Import Yahoo terminé.");
        }

        [HttpGet("searchByIsinWithHistory")]
        public async Task<IActionResult> SearchByIsin([FromQuery] string isin)
        {
            if (string.IsNullOrWhiteSpace(isin))
                return BadRequest("ISIN requis");

            // Appel à un nouveau provider que l'on crée juste après
            var result = await _importService.SearchSupportByIsinFromEodAsync(isin);

            if (result == null)
                return NotFound("Aucun support trouvé pour cet ISIN");

            return Ok(result);
        }

        [HttpGet("{id:int}/history")]
        public async Task<IActionResult> GetHistoricalData([FromRoute] int id)
        {
            var history = await _historyRepo.GetBySupportIdAsync(id);

            if (history == null || !history.Any())
                return NotFound("Aucune donnée historique trouvée pour ce support.");

            return Ok(history.Select(h => new
            {
                date = h.Date,
                open = h.Open,
                high = h.High,
                low = h.Low,
                close = h.Close,
                volume = h.Volume
            }));
        }

        [HttpGet("import/eod")]
        public async Task<IActionResult> ImportFromEod([FromQuery] string ticker)
        {
            var result = await _importService.ImportFromEodAsync(ticker);
            return Ok(result);
        }

        [HttpPost("{id:int}/import/eod")]
        public async Task<IActionResult> ImportHistoryFromEod([FromRoute] int id, [FromQuery] string isin)
        {
            if (string.IsNullOrWhiteSpace(isin))
                return BadRequest("ISIN requis");

            try
            {
                await _importService.ImportFromEodByIsinAsync(id, isin);
                return Ok("Import historique EOD terminé.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Erreur : {ex.Message}");
            }
        }

        [HttpGet("searchByIsinCombined")]
        public async Task<IActionResult> SearchByIsinCombined([FromQuery] string isin)
        {
            DebugLogger.Log($">>> Controller SearchByIsinCombined appelé avec ISIN={isin}");
            if (string.IsNullOrWhiteSpace(isin))
                return BadRequest("ISIN requis");

            var (dto, ticker) = await _importService.ImportFromYahooThenEodAsync(isin);

            if (dto == null)
                return NotFound("Aucun support trouvé pour cet ISIN");

            return Ok(new { dto, ticker });
        }

        [HttpGet("searchByIsinFromTwelve")]
        public async Task<IActionResult> SearchByIsinFromTwelve([FromQuery] string isin)
        {
            DebugLogger.Log($">>> Controller SearchByIsinFromTwelve appelé avec ISIN={isin}");
            DebugLogger.Log("Clé API utilisée : " + _config["TwelveData:ApiKey"]);
            try
            {
                var (dto, symbol) = await _importService.ImportFromTwelveAsync(isin);

                if (dto == null)
                    return NotFound("Aucune donnée Twelve Data pour cet ISIN");

                return Ok(new { dto, symbol });
            }
            catch (Exception ex)
            {
                // LOG DANS FICHIER À LA MAIN, pour être sûr que ça s’écrit !
                System.IO.File.AppendAllText(@"C:\Temp\debug-api.log", $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {ex.ToString()} {Environment.NewLine}");

                // Et tu retournes aussi le détail dans la réponse HTTP pour debug court
                return StatusCode(500, ex.ToString());
            }
        }

        [HttpGet("searchByIsinChained")]
        public async Task<IActionResult> SearchByIsinChained([FromQuery] string isin)
        {
            if (string.IsNullOrWhiteSpace(isin))
                return BadRequest("ISIN requis");

            var (dto, symbol, source) = await _importService.ImportFromAnyProviderAsync(isin);

            if (dto == null)
                return NotFound("Aucun support trouvé via TwelveData, EOD, ou YahooFinance");

            return Ok(new { dto, symbol, source });
        }

        // Ajoute ceci dans ta classe controller
        [HttpGet("typeahead")]
        public async Task<IActionResult> Typeahead([FromQuery] string search)
        {
            if (string.IsNullOrWhiteSpace(search) || search.Length < 2)
                return Ok(new List<object>()); // évite les requêtes inutiles

            // Recherche sur ISIN, Label ou Code (ajuste selon ton modèle)
            var results = await _financialSupportRepository.TypeaheadAsync(search ?? "");

            // On retourne uniquement les champs utiles pour le typeahead
            return Ok(new { items = results });
        }


    }
}
