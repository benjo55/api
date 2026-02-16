using System.Threading.Tasks;
using api.Dtos.Compartment;
using api.Models;
using System.Collections.Generic;

namespace api.Interfaces
{
    public interface ICompartmentRepository
    {
        Task<List<Compartment>> GetByContractAsync(int contractId);
        Task<Compartment?> GetByIdAsync(int id);
        Task<Compartment> CreateAsync(Compartment model, CreateCompartmentRequestDto dto);
        Task<Compartment?> UpdateAsync(int id, UpdateCompartmentRequestDto dto);
        Task<bool> DeleteAsync(int id);
        Task<Compartment?> PatchLabelAsync(int id, string newLabel);
    }
}
