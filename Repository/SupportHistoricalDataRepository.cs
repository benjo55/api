using api.Interfaces;
using api.Models;
using api.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Repository
{
    public class SupportHistoricalDataRepository : ISupportHistoricalDataRepository
    {
        private readonly ApplicationDBContext _context;

        public SupportHistoricalDataRepository(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<List<SupportHistoricalData>> GetBySupportIdAsync(int supportId)
        {
            var result = await _context.SupportHistoricalDatas
                .Where(h => h.FinancialSupportId == supportId)
                .OrderBy(h => h.Date)
                .ToListAsync();

            Console.WriteLine($"[DEBUG] GetBySupportIdAsync : {result.Count} historiques lus pour SupportId={supportId}");
            return result;
        }

        public async Task InsertRangeAsync(List<SupportHistoricalData> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                Console.WriteLine("[DEBUG] InsertRangeAsync : aucune entrée à insérer.");
                return;
            }

            _context.SupportHistoricalDatas.AddRange(entries);
            var n = await _context.SaveChangesAsync();
            Console.WriteLine($"[DEBUG] InsertRangeAsync : {entries.Count} entrées insérées, {n} SaveChanges.");
        }

        public async Task DeleteBySupportIdAsync(int supportId)
        {
            var toDelete = await _context.SupportHistoricalDatas
                .Where(h => h.FinancialSupportId == supportId)
                .ToListAsync();

            if (toDelete.Count == 0)
            {
                Console.WriteLine($"[DEBUG] DeleteBySupportIdAsync : aucune entrée à supprimer pour SupportId={supportId}");
                return;
            }

            _context.SupportHistoricalDatas.RemoveRange(toDelete);
            var n = await _context.SaveChangesAsync();
            Console.WriteLine($"[DEBUG] DeleteBySupportIdAsync : {toDelete.Count} entrées supprimées, {n} SaveChanges.");
        }
    }
}
