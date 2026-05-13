namespace api.Models
{
    public static class OperationTypeExtensions
    {
        public static bool IsWithdrawal(this OperationType type)
        {
            return type is OperationType.PartialWithdrawal
                       or OperationType.TotalWithdrawal
                       or OperationType.ScheduledWithdrawal
                       or OperationType.ManagementFee
                       or OperationType.OperationFee;
        }

        public static bool IsPayment(this OperationType type)
        {
            return type is OperationType.InitialPayment
                       or OperationType.FreePayment
                       or OperationType.ScheduledPayment;
        }
    }
}
