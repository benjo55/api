using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Dtos.Notary;
using api.Helpers;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace api.Repository
{
    public class NotaryRepository : INotaryRepository
    {
        private readonly ApplicationDBContext _context;
        public NotaryRepository(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<Notary> CreateAsync(Notary notaryModel)
        {
            await _context.Notaries.AddAsync(notaryModel);
            await _context.SaveChangesAsync();
            return notaryModel;
        }

        public async Task<Notary?> DeleteAsync(int id)
        {
            var notaryModel = await _context.Notaries.FirstOrDefaultAsync(n => n.Id == id);
            if (notaryModel == null) return null;
            _context.Notaries.Remove(notaryModel);
            await _context.SaveChangesAsync();
            return notaryModel;
        }

        public async Task<List<Notary>> GetAllAsync(QueryObject query)
        {
            return await _context.Notaries.ToListAsync();
        }
        public async Task<Notary?> GetByIdAsync(int id)
        {
            return await _context.Notaries.FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task<bool> NotaryExists(int id)
        {
            return await _context.Notaries.AnyAsync(n => n.Id == id);
        }

        public async Task<Notary?> UpdateAsync(int id, UpdateNotaryRequestDto NotaryDto)
        {
            var existingNotary = await _context.Notaries.FirstOrDefaultAsync(n => n.Id == id);
            if (existingNotary == null) return null;

            existingNotary.RaisonSociale = NotaryDto.RaisonSociale;
            existingNotary.Adresse1 = NotaryDto.Adresse1;
            existingNotary.Adresse2 = NotaryDto.RaisonSociale;
            existingNotary.CodePostal = NotaryDto.CodePostal;
            existingNotary.Ville = NotaryDto.Ville;
            existingNotary.Telephone = NotaryDto.Telephone;


            await _context.SaveChangesAsync();
            return existingNotary;
        }
    }
}