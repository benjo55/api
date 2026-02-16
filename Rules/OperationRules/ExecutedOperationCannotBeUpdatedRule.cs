using api.Models;

namespace api.Rules.OperationRules
{
    /// <summary>
    /// Interdit toute modification d’une opération déjà exécutée.
    /// </summary>
    public class ExecutedOperationCannotBeUpdatedRule : IBusinessRule<Operation>
    {
        public string Name => nameof(ExecutedOperationCannotBeUpdatedRule);

        public string ErrorMessage =>
            "Une opération exécutée ne peut pas être modifiée.";

        public bool IsSatisfiedBy(Operation operation)
        {
            // Si l’opération est exécutée → rejet
            return operation.Status != OperationStatus.Executed;
        }
    }
}
