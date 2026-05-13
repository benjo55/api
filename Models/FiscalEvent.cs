using System.ComponentModel.DataAnnotations;
using api.Dtos.TaxProfile;

namespace api.Models
{
    /// <summary>
    /// Journal des événements fiscaux métier traités par le moteur.
    /// </summary>
    public class FiscalEvent
    {
        [Key]
        public int Id { get; set; }

        public int TaxComputationId { get; set; }

        public FiscalEventType EventType { get; set; }

        public DateTime EventDate { get; set; } = DateTime.UtcNow;

        public string Label { get; set; } = string.Empty;

        public TaxComputation? TaxComputation { get; set; }
    }
}
