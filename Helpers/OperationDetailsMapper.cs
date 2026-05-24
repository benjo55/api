using api.Dtos.Operation;
using api.Models;

namespace api.Helpers
{
    /// <summary>
    /// Convertit entre les entités Model séparées (PaymentDetail, WithdrawalDetail,
    /// ArbitrageDetail) et le DTO polymorphe <see cref="OperationDetailsDto"/>.
    /// </summary>
    public static class OperationDetailsMapper
    {
        // =====================================================================
        //  MODEL → DTO
        // =====================================================================

        public static OperationDetailsDto? ToDto(Operation op)
        {
            if (op.PaymentDetail != null)
            {
                var pd = op.PaymentDetail;
                // Déduire le mode depuis PaymentMethod (legacy)
                var mode = pd.PaymentMethod?.ToLowerInvariant() switch
                {
                    "chèque" or "cheque" or "check" => "check",
                    "prélèvement" or "prelevement" or "sepa" => "sepa",
                    _ => "transfer"
                };

                return new PaymentDetailsDto
                {
                    Mode = mode,
                    SourceOfFunds = pd.SourceOfFunds,
                    Frequency = pd.Frequency,
                    StartDate = pd.StartDate,
                    ScheduleStatus = pd.ScheduleStatus?.ToString().ToLowerInvariant(),
                    ScheduleGroupId = pd.ScheduleGroupId,
                    SuspendedAt = pd.SuspendedAt,
                    StoppedAt = pd.StoppedAt,
                };
            }

            if (op.WithdrawalDetail != null)
            {
                var wd = op.WithdrawalDetail;
                return new WithdrawalDetailsDto
                {
                    Mode = wd.IsScheduled ? "scheduled" : "oneShot",
                    GrossAmount = wd.GrossAmount,
                    Frequency = wd.Frequency,
                };
            }

            if (op.ArbitrageDetail != null)
            {
                return new ArbitrageDetailsDto
                {
                    Mode = "manual",
                };
            }

            return null;
        }

        // =====================================================================
        //  DTO → MODEL
        // =====================================================================

        public static PaymentDetail? ToPaymentModel(OperationDetailsDto? dto)
        {
            if (dto is not PaymentDetailsDto pd) return null;

            var method = pd.Mode switch
            {
                "check" => "Chèque",
                "sepa" => "Prélèvement",
                _ => "Virement"
            };

            return new PaymentDetail
            {
                PaymentMethod = method,
                SourceOfFunds = pd.SourceOfFunds,
                Frequency = pd.Frequency,
                StartDate = pd.StartDate,
                ScheduleStatus = pd.ScheduleStatus?.ToLowerInvariant() switch
                {
                    "suspended" => OperationScheduleStatus.Suspended,
                    "stopped" => OperationScheduleStatus.Stopped,
                    _ => OperationScheduleStatus.Active,
                },
                ScheduleGroupId = pd.ScheduleGroupId,
                Amount = 0, // Amount est sur Operation.Amount
            };
        }

        public static WithdrawalDetail? ToWithdrawalModel(OperationDetailsDto? dto)
        {
            if (dto is not WithdrawalDetailsDto wd) return null;

            return new WithdrawalDetail
            {
                GrossAmount = wd.GrossAmount ?? 0,
                IsScheduled = wd.Mode == "scheduled",
                Frequency = wd.Frequency,
            };
        }

        public static ArbitrageDetail? ToArbitrageModel(OperationDetailsDto? dto)
        {
            if (dto is not ArbitrageDetailsDto) return null;

            // FromSupportId/ToSupportId/Percentage sont obsolètes — les allocations
            // portent désormais les supports. On garde des valeurs par défaut.
            return new ArbitrageDetail
            {
                FromSupportId = 0,
                ToSupportId = 0,
                Percentage = 0,
            };
        }
    }
}
