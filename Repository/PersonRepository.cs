using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Dtos.Person;
using api.Dtos.Generic;
using api.Helpers;
using api.Interfaces;
using api.Models;
using api.Services;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace api.Repository
{
    public class PersonRepository : IPersonRepository
    {
        private readonly ApplicationDBContext _context;
        private readonly EntityHistoryService _entityHistoryService;  // Service d'historisation
        private readonly IMapper _mapper; // AutoMapper pour éviter d'affecter manuellement les champs
        private readonly BusinessRuleValidator _validator;

        public PersonRepository(ApplicationDBContext context, EntityHistoryService entityHistoryService, IMapper mapper, BusinessRuleValidator validator)
        {
            _context = context;
            _entityHistoryService = entityHistoryService;
            _mapper = mapper;
            _validator = validator;
            _mapper = mapper;
        }

        public async Task<Person> CreateAsync(Person personModel)
        {
            _validator.Validate(personModel);
            await _context.Persons.AddAsync(personModel);
            await _context.SaveChangesAsync();
            return personModel;
        }

        public async Task<Person?> DeleteAsync(int id)
        {
            try
            {
                var personModel = await _context.Persons.FirstOrDefaultAsync(p => p.Id == id);
                if (personModel == null) return null;
                _validator.Validate(personModel);
                _context.Persons.Remove(personModel);
                await _context.SaveChangesAsync();
                return personModel;
            }
            catch (DbUpdateException ex) when ((ex.InnerException as SqlException)?.Number == 547)
            {
                throw new InvalidOperationException("Impossible de supprimer cette personne car elle est titulaire d'au moins un contrat", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Une erreur inattendue s'est produite lors de la suppression.", ex);
            }

        }

        public async Task<PagedResult<Person>> GetAllAsync(QueryObject query)
        {
            var persons = _context.Persons
                .AsNoTracking()
                .AsSplitQuery() // 🔥 optimisation clé
                .Include(p => p.Contracts)
                .AsQueryable();

            // Filtres dynamiques

            // ✅ Filtrage par rôle
            if (!string.IsNullOrEmpty(query.Role))
                persons = persons.Where(p => p.Role == query.Role);

            // ✅ Filtrage par statut
            if (!string.IsNullOrEmpty(query.Status))
                persons = persons.Where(p => p.Status == query.Status);

            if (!string.IsNullOrWhiteSpace(query.FirstName))
            {
                persons = persons.Where(p => p.FirstName.Contains(query.FirstName));
            }
            if (!string.IsNullOrWhiteSpace(query.LastName))
            {
                persons = persons.Where(p => p.LastName.Contains(query.LastName));
            }
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                persons = persons.Where(p =>
                    p.FirstName.Contains(query.Search) ||
                    p.LastName.Contains(query.Search) ||
                    p.BirthCountry.Contains(query.Search) ||
                    p.Sex.Contains(query.Search));
            }

            persons = query.SortBy switch
            {
                "FirstName" => query.IsDescending ? persons.OrderByDescending(p => p.FirstName) : persons.OrderBy(p => p.FirstName),
                "LastName" => query.IsDescending ? persons.OrderByDescending(p => p.LastName) : persons.OrderBy(p => p.LastName),
                "UpdatedDate" => query.IsDescending ? persons.OrderByDescending(p => p.UpdatedDate) : persons.OrderBy(p => p.UpdatedDate),
                _ => persons.OrderByDescending(p => p.CreatedDate) // Default sorting
            };

            // Pagination
            var totalCount = await persons.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);
            var skipNumber = (query.PageNumber - 1) * query.PageSize;
            var pagedPersons = await persons.Skip(skipNumber).Take(query.PageSize).ToListAsync();
            var hasNextPage = query.PageNumber < totalPages;

            return new PagedResult<Person>
            {
                Items = pagedPersons,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = hasNextPage
            };
        }

        public async Task<PagedResult<PersonListDto>> GetListLightAsync(QueryObject query)
        {
            var persons = _context.Persons
                .AsNoTracking()
                .AsQueryable();

            // 🔍 Filtres avant projection
            if (!string.IsNullOrEmpty(query.Role))
                persons = persons.Where(p => p.Role == query.Role);

            if (!string.IsNullOrEmpty(query.Status))
                persons = persons.Where(p => p.Status == query.Status);

            if (!string.IsNullOrWhiteSpace(query.FirstName))
                persons = persons.Where(p => p.FirstName.Contains(query.FirstName));

            if (!string.IsNullOrWhiteSpace(query.LastName))
                persons = persons.Where(p => p.LastName.Contains(query.LastName));

            if (!string.IsNullOrWhiteSpace(query.Search))
                persons = persons.Where(p =>
                    p.FirstName.Contains(query.Search) ||
                    p.LastName.Contains(query.Search) ||
                    p.Email1.Contains(query.Search));

            // 🧠 Projection
            var projection = persons.Select(p => new PersonListDto
            {
                Id = p.Id,
                FullName = p.FirstName + " " + p.LastName,
                Email1 = p.Email1,
                PhoneNumber = p.PhoneNumber,
                Role = p.Role,
                Status = p.Status,
                UpdatedDate = p.UpdatedDate,
                ContractCount = p.Contracts.Count()
            });

            // Tri
            projection = query.SortBy switch
            {
                "FullName" => query.IsDescending ? projection.OrderByDescending(p => p.FullName) : projection.OrderBy(p => p.FullName),
                "UpdatedDate" => query.IsDescending ? projection.OrderByDescending(p => p.UpdatedDate) : projection.OrderBy(p => p.UpdatedDate),
                _ => projection.OrderByDescending(p => p.UpdatedDate)
            };

            var totalCount = await projection.CountAsync();
            var items = await projection
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PagedResult<PersonListDto>
            {
                Items = items,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize),
                HasNextPage = query.PageNumber * query.PageSize < totalCount,
                CurrentPage = query.PageNumber
            };
        }

        public async Task<Person?> GetByIdAsync(int id)
        {
            return await _context.Persons
            .Include(p => p.Contracts)
                .ThenInclude(c => c.BeneficiaryClause)
            .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Person?> UpdateAsync(int id, UpdatePersonRequestDto updatePersonDto)
        {
            var existingPerson = await _context.Persons.FirstOrDefaultAsync(p => p.Id == id);
            if (existingPerson == null) return null;

            // Sauvegarde de l'état initial pour l'historique
            var originalPerson = new Person();
            _mapper.Map(existingPerson, originalPerson);

            // Mise à jour automatique des champs avec AutoMapper
            _mapper.Map(updatePersonDto, existingPerson);
            existingPerson.UpdatedDate = DateTime.UtcNow; // Met à jour la date de modification

            // Historisation des changements
            await _entityHistoryService.TrackChangesAsync(originalPerson, existingPerson, "Admin");  // Remplace "Admin" par l'utilisateur actuel

            await _context.SaveChangesAsync();
            return existingPerson;
        }

        public async Task<List<PersonTypeaheadDto>> GetTypeaheadAsync(string search)
        {
            if (string.IsNullOrWhiteSpace(search) || search.Length < 2)
                return new List<PersonTypeaheadDto>();

            return await _context.Persons
                .Where(p => p.FirstName.Contains(search) || p.LastName.Contains(search))
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .Select(p => new PersonTypeaheadDto
                {
                    Id = p.Id,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    BirthDate = p.BirthDate,
                    BirthCity = p.BirthCity,
                })
                .Take(50)
                .ToListAsync();
        }

        public async Task<bool> PersonExists(int id)
        {
            return await _context.Persons.AnyAsync(p => p.Id == id);
        }

        public async Task<bool> IsPersonBeneficiary(int personId)
        {
            return await _context.BeneficiaryClausePersons
                .AnyAsync(bcp => bcp.PersonId == personId);
        }

        public async Task<bool> HasContracts(int personId)
        {
            return await _context.Contracts.AnyAsync(c => c.PersonId == personId);
        }

        public async Task<Person?> PatchLockedAsync(int id, bool locked)
        {
            var person = await _context.Persons.FirstOrDefaultAsync(p => p.Id == id);
            if (person == null)
                return null;

            var original = new Person();
            _mapper.Map(person, original);

            person.Locked = locked;
            person.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Historisation
            await _entityHistoryService.TrackChangesAsync(original, person, "Admin");

            return person;
        }
    }
}
