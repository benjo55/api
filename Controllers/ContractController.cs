using System;
using System.Linq;
using System.Threading.Tasks;
using api.Interfaces;
using api.Dtos.Contract;
using api.Mappers;
using Microsoft.AspNetCore.Mvc;
using api.Helpers;
using api.Dtos.Operation;

namespace api.Controllers
{
    [Route("api/contracts")]
    [ApiController]
    public class ContractController : ControllerBase
    {
        private readonly IContractRepository _contractRepository;
        private readonly IProductRepository _productRepository;
        private readonly IOperationEngineService _operationEngineService;
        private readonly IContractValuationService _valuationService;
        private readonly IOperationRepository _operationRepository;

        public ContractController(
            IContractRepository contractRepository,
            IProductRepository productRepository,
            IOperationEngineService operationEngineService,
            IOperationRepository operationRepository,
            IContractValuationService valuationService)
        {
            _contractRepository = contractRepository;
            _productRepository = productRepository;
            _operationEngineService = operationEngineService;
            _operationRepository = operationRepository;
            _valuationService = valuationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] QueryObject query)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var contracts = await _contractRepository.GetAllAsync(query);
            var contractDtos = contracts.Items.Select(s => s.ToContractDto()).ToList();

            return Ok(new
            {
                contracts.TotalCount,
                contracts.TotalPages,
                contracts.HasNextPage,
                contracts.CurrentPage,
                Items = contractDtos
            });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            var contract = await _contractRepository.GetByIdAsync(id);
            if (contract == null) return NotFound();
            return Ok(contract.ToContractDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateContractRequestDto createContractRequestDto)
        {
            // Création enrichie : ajout des relations
            var contractModel = createContractRequestDto.ToContractFromCreateDto();
            var createdContract = await _contractRepository.CreateAsync(contractModel, createContractRequestDto);

            // Vérifier si ProductId est défini
            if (createdContract.ProductId == 0)
            {
                return BadRequest("Le ProductId du contrat est requis.");
            }
            var productId = createdContract.ProductId ?? 0;

            // Mettre à jour le contractCount du produit
            var product = await _productRepository.GetByIdAsync(productId);
            if (product != null)
            {
                product.ContractCount = await _contractRepository.CountContractsByProductIdAsync(productId);
                var updateProductDto = product.ToUpdateProductRequestDto();
                await _productRepository.UpdateAsync(product.Id, updateProductDto);
            }

            // Retourne un ContractDto enrichi
            return CreatedAtAction(nameof(GetById), new { id = createdContract.Id }, createdContract.ToContractDto());
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateContractRequestDto updateContractDto)
        {
            var contractModel = await _contractRepository.UpdateAsync(id, updateContractDto);
            if (contractModel == null) return NotFound("Contrat non trouvé");
            return Ok(contractModel.ToContractDto());
        }

        [HttpPatch("{id}/beneficiaryClauseId")]
        public async Task<IActionResult> PatchBeneficiaryClauseId(int id, [FromBody] int clauseId)
        {
            var contract = await _contractRepository.PatchBeneficiaryClauseIdAsync(id, clauseId);
            if (contract == null)
                return NotFound();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            try
            {
                var contractModel = await _contractRepository.DeleteAsync(id);
                if (contractModel == null) return NotFound("Le contrat n'existe pas");
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine("⚠️ Erreur de suppression (FK) : " + ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 Erreur Serveur : " + ex.Message);
                return StatusCode(500, new { message = "Une erreur interne est survenue.", details = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPatch("{id:int}/locked")]
        public async Task<IActionResult> PatchLocked(int id, [FromBody] bool locked)
        {
            var updatedContract = await _contractRepository.PatchLockedAsync(id, locked);
            if (updatedContract == null)
                return NotFound();

            return Ok(updatedContract);
        }

        [HttpGet("{id:int}/available-supports")]
        public async Task<IActionResult> GetAvailableSupports([FromRoute] int id, [FromQuery] int? compartmentId)
        {
            // 🔹 On récupère les allocations du contrat (avec filtre optionnel)
            var supports = await _contractRepository.GetAvailableSupportsAsync(id, compartmentId);

            if (supports == null || !supports.Any())
                return Ok(Enumerable.Empty<object>());

            // 🔹 On construit un DTO propre
            var result = supports
                .Where(s => s.Support != null)
                .Select(s => new
                {
                    SupportId = s.SupportId,
                    Label = s.Support!.Label,
                    ISIN = s.Support.ISIN,

                    // Valorisation
                    VL = s.Support.LastValuationAmount,
                    DateVL = s.Support.LastValuationDate,

                    // 🎯 Compartiment EXACT de cette allocation
                    CompartmentId = s.CompartmentId,

                    // Quantité dans CE compartiment
                    CurrentShares = s.CurrentShares,
                    CurrentAmount = s.CurrentAmount
                })
                .OrderByDescending(s => s.CurrentAmount)
                .ToList();

            return Ok(result);
        }

        [HttpPost("{id}/recalculate-value")]
        public async Task<IActionResult> RecalculateValue(int id)
        {
            var result = await _contractRepository.RecalculateValueAsync(id, _valuationService, source: "ContractController - RecalculateValue (recalculate-value endpoint)");

            // Try to read a 'message' property if the returned object has one (anonymous object or DTO)
            var message = result?.GetType().GetProperty("message")?.GetValue(result) as string;
            if (!string.IsNullOrEmpty(message) && message.Contains("introuvable"))
                return NotFound(result);

            return Ok(result);
        }

        [HttpGet("{id}/holdings")]
        public async Task<IActionResult> GetHoldings(int id)
        {
            var holdings = await _contractRepository.GetHoldingsByContractAsync(id);
            if (holdings == null || !holdings.Any())
                return NotFound($"Aucun holding trouvé pour le contrat {id}");

            var result = holdings.Select(h =>
            {
                return new
                {
                    h.SupportId,
                    SupportLabel = h.SupportLabel,
                    ISIN = h.ISIN,

                    VL = Math.Round(h.Vl, 5),
                    TotalShares = Math.Round(h.TotalShares, 7),

                    // 💰 MONTANT RÉEL (Option A demandée)
                    CurrentValue = Math.Round(h.CurrentValue, 7),

                    // Valeur initiale (si toujours utile)
                    TotalInvested = Math.Round(h.TotalInvested, 7),

                    Pru = Math.Round(h.Pru, 7),

                    PerformancePercent = Math.Round(h.PerformancePercent, 4),

                    LastUpdated = h.LastUpdated
                };
            });

            return Ok(result);
        }

        // ContractsController.cs
        [HttpGet("{id}/operations")]
        public async Task<ActionResult<IEnumerable<OperationDto>>> GetOperationsByContract(int id)
        {
            var operations = await _operationRepository.GetByContractIdAsync(id);
            if (operations == null || !operations.Any())
                return NotFound();

            return Ok(operations);
        }

        [HttpPost("{id}/rebuild-holdings")]
        public async Task<IActionResult> RebuildHoldings(int id)
        {
            try
            {
                var result = await _contractRepository.RebuildHoldingsAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{id:int}/rebuild")]
        public async Task<IActionResult> Rebuild(int id)
        {
            var engine = HttpContext.RequestServices.GetRequiredService<IOperationEngineService>();

            await engine.RebuildContractAsync(id);

            var repo = HttpContext.RequestServices.GetRequiredService<IContractRepository>();
            var contract = await repo.GetByIdAsync(id);

            return Ok(new
            {
                message = "Rebuild terminé",
                contractId = id,
                contract = contract?.ToContractDto()
            });
        }
    }
}
