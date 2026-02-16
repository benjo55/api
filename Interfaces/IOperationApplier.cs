using System.Threading;
using System.Threading.Tasks;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Interfaces
{
    /// <summary>
    /// Applique les effets financiers d'une opération EXECUTÉE
    /// sur l'état réel du contrat (parts, montants investis, holdings).
    ///
    /// ⚠️ IMPORTANT :
    /// - Ne doit JAMAIS être appelé pour une opération Pending
    /// - Ne gère PAS la transition de statut
    /// - N'a AUCUNE logique UX
    /// </summary>
    public interface IOperationApplier
    {


        /// <summary>
        /// Applique l'opération sur les holdings du contrat.
        /// Cette méthode suppose que :
        /// - operation.Status == Executed
        /// - les allocations sont déjà persistées
        /// </summary>
        Task ApplyAsync(
            Operation operation,
            DbContext context,
            CancellationToken cancellationToken = default
        );
    }
}
