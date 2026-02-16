using api.Models;

namespace api.Rules.OperationRules
{
    /// <summary>
    /// Interdit la suppression d’une opération déjà exécutée.
    /// </summary>
    public class ExecutedOperationCannotBeDeletedRule : IBusinessRule<Operation>
    {
        public string Name => nameof(ExecutedOperationCannotBeDeletedRule);

        public string ErrorMessage =>
            "Une opération exécutée ne peut pas être supprimée.";

        public bool IsSatisfiedBy(Operation operation)
        {
            return operation.Status != OperationStatus.Executed;
        }
    }
}
