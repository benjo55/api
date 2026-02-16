using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Notary;
using api.Helpers;
using api.Models;

namespace api.Interfaces
{
    public interface INotaryRepository
    {
        Task<List<Notary>> GetAllAsync(QueryObject query);
        Task<Notary?> GetByIdAsync(int id);
        Task<Notary> CreateAsync(Notary notaryModel);
        Task<Notary?> UpdateAsync(int id, UpdateNotaryRequestDto notaryDto);
        Task<Notary?> DeleteAsync(int id);
        Task<bool> NotaryExists(int id);
    }
}