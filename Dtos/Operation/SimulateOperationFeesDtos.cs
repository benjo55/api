using System.ComponentModel.DataAnnotations;
using api.Models.Enum;
using api.Models;
using System.Linq;

namespace api.Dtos.Operation
{
    public class AllocationForSimulationDto
    {
        [Required]
        public int SupportId { get; set; }

        public int? CompartmentId { get; set; }

        [Required]
        public OperationFlow Flow { get; set; }

        public decimal? Amount { get; set; }
        public decimal? Percentage { get; set; }
    }

    public class SimulateOperationFeesRequest
    {
        [Required]
        public int ContractId { get; set; }

        [Required]
        public OperationType OperationType { get; set; }

        [Required]
        public DateTime OperationDate { get; set; }

        [Required]
        public List<AllocationForSimulationDto> Allocations { get; set; } = new List<AllocationForSimulationDto>();
    }

    public class SimulatedFeeResult
    {
        public int SupportId { get; set; }
        public int? CompartmentId { get; set; }
        public decimal BaseAmount { get; set; }
        public decimal FeeAmount { get; set; }
        public string Mode { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public decimal FixedAmount { get; set; }
        public string ApplyOn { get; set; } = string.Empty;
    }
}
