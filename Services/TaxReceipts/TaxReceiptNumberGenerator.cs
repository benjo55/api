using System.Threading;
using api.Data;
using api.Interfaces;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace api.Services.TaxReceipts
{
    public sealed class TaxReceiptNumberGenerator : ITaxReceiptNumberGenerator
    {
        private static long _fallbackSequence;
        private readonly ApplicationDBContext _db;
        private readonly IConfiguration _configuration;

        public TaxReceiptNumberGenerator(ApplicationDBContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        public async Task<string> GenerateAsync(CancellationToken cancellationToken = default)
        {
            var sequenceValue = await NextSequenceValueAsync(cancellationToken);
            var prefix = _configuration["TaxReceipts:NumberPrefix"] ?? string.Empty;
            var year = DateTime.UtcNow.Year;
            return string.IsNullOrWhiteSpace(prefix)
                ? $"{year}-{sequenceValue:000000}"
                : $"{prefix}-{year}-{sequenceValue:000000}";
        }

        private async Task<long> NextSequenceValueAsync(CancellationToken cancellationToken)
        {
            if (string.Equals(_db.Database.ProviderName, "Microsoft.EntityFrameworkCore.SqlServer", StringComparison.Ordinal))
            {
                var connection = _db.Database.GetDbConnection();
                var closeConnection = connection.State == ConnectionState.Closed;

                if (closeConnection)
                {
                    await _db.Database.OpenConnectionAsync(cancellationToken);
                }

                try
                {
                    await using var command = connection.CreateCommand();
                    command.CommandText = "SELECT NEXT VALUE FOR dbo.TaxReceiptNumberSequence";
                    var currentTransaction = _db.Database.CurrentTransaction;
                    if (currentTransaction is not null)
                    {
                        command.Transaction = currentTransaction.GetDbTransaction();
                    }

                    var value = await command.ExecuteScalarAsync(cancellationToken);
                    return Convert.ToInt64(value);
                }
                finally
                {
                    if (closeConnection)
                    {
                        await _db.Database.CloseConnectionAsync();
                    }
                }
            }

            return Interlocked.Increment(ref _fallbackSequence);
        }
    }
}
