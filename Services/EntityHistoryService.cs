using api.Data;  // Assurez-vous que ce namespace correspond à l'emplacement de ApplicationDbContext
using api.Interfaces;
using api.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace api.Services
{
    public class EntityHistoryService
    {
        private readonly IEntityHistoryRepository _historyRepository;
        private readonly ApplicationDBContext _dbContext; // Correction : Injection de DbContext

        public EntityHistoryService(IEntityHistoryRepository historyRepository, ApplicationDBContext dbContext)
        {
            _historyRepository = historyRepository;
            _dbContext = dbContext; // Initialisation de _dbContext
        }

        public async Task TrackChangesAsync<T>(T originalEntity, T updatedEntity, string modifiedBy) where T : class
        {
            var entityName = typeof(T).Name;
            var entry = _dbContext.Entry(updatedEntity);
            var idProperty = entry.Property("Id");
            if (idProperty == null || idProperty.CurrentValue == null)
                throw new InvalidOperationException($"L'entité {entityName} ne possède pas de champ 'Id' valide.");

            var excludedFields = new HashSet<string> { "UpdatedDate", "CreatedDate" };
            var historyEntries = new List<EntityHistory>();

            foreach (var property in entry.Properties)
            {
                var propInfo = typeof(T).GetProperty(property.Metadata.Name);
                // Ignore navigation properties/collections except string
                if (propInfo == null)
                    continue;
                if (propInfo.PropertyType.IsClass && propInfo.PropertyType != typeof(string))
                    continue;
                if (excludedFields.Contains(property.Metadata.Name))
                    continue;

                var originalValue = propInfo.GetValue(originalEntity)?.ToString();
                var currentValue = property.CurrentValue?.ToString();

                if (originalValue != currentValue)
                {
                    historyEntries.Add(new EntityHistory
                    {
                        EntityName = entityName,
                        EntityId = Convert.ToInt32(idProperty.CurrentValue),
                        PropertyName = property.Metadata.Name,
                        OldValue = originalValue,
                        NewValue = currentValue,
                        ModifiedBy = modifiedBy,
                    });
                }
            }

            if (historyEntries.Any())
            {
                await _dbContext.EntityHistories.AddRangeAsync(historyEntries);
                await _dbContext.SaveChangesAsync();
            }
        }
        public async Task TrackEventAsync(string entityName, int entityId, string propertyName, string? oldValue, string? newValue, string modifiedBy)
        {
            var history = new EntityHistory
            {
                EntityName = entityName,
                EntityId = entityId,
                PropertyName = propertyName,
                OldValue = oldValue,
                NewValue = newValue,
                ModifiedAt = DateTime.UtcNow,
                ModifiedBy = modifiedBy
            };
            await _dbContext.EntityHistories.AddAsync(history);
            await _dbContext.SaveChangesAsync();
        }


    }
}
