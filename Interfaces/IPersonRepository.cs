using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Person;
using api.Dtos.Generic;
using api.Helpers;
using api.Models;

namespace api.Interfaces
{
    public interface IPersonRepository
    {
        Task<PagedResult<Person>> GetAllAsync(QueryObject query);
        Task<PagedResult<PersonListDto>> GetListLightAsync(QueryObject query);
        Task<Person?> GetByIdAsync(int id);
        Task<Person> CreateAsync(Person personModel);
        Task<Person?> UpdateAsync(int id, UpdatePersonRequestDto personDto);
        Task<Person?> DeleteAsync(int id);
        Task<bool> PersonExists(int id);
        Task<List<PersonTypeaheadDto>> GetTypeaheadAsync(string search);
        Task<bool> IsPersonBeneficiary(int personId);
        Task<bool> HasContracts(int personId);
        Task<Person?> PatchLockedAsync(int id, bool locked);
    }
}