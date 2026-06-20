using api.Dtos.Advance;
using api.Models;

namespace api.Mappers
{
    public static class AdvanceMapper
    {
        public static AdvanceDto ToDto(this Advance advance)
        {
            return new AdvanceDto
            {
                Id = advance.Id,
                ContractId = advance.ContractId,
                AdvanceNumber = advance.AdvanceNumber,
                RequestDate = advance.RequestDate,
                ApprovalDate = advance.ApprovalDate,
                DisbursementDate = advance.DisbursementDate,
                MaturityDate = advance.MaturityDate,
                RequestedAmount = advance.RequestedAmount,
                ApprovedAmount = advance.ApprovedAmount,
                OutstandingCapital = advance.OutstandingCapital,
                InterestRate = advance.InterestRate,
                DurationMonths = advance.DurationMonths,
                Reason = advance.Reason,
                Status = advance.Status,
                Locked = advance.Locked,
                CreatedDate = advance.CreatedDate,
                UpdatedDate = advance.UpdatedDate,
                Transactions = advance.Transactions?
                    .OrderByDescending(t => t.OperationDate)
                    .Select(t => t.ToDto())
                    .ToList() ?? new()
            };
        }

        public static AdvanceTransactionDto ToDto(this AdvanceTransaction transaction)
        {
            return new AdvanceTransactionDto
            {
                Id = transaction.Id,
                AdvanceId = transaction.AdvanceId,
                OperationId = transaction.OperationId,
                OperationDate = transaction.OperationDate,
                Type = transaction.Type,
                Amount = transaction.Amount,
                Comment = transaction.Comment,
                CreatedDate = transaction.CreatedDate,
                UpdatedDate = transaction.UpdatedDate
            };
        }
    }
}
