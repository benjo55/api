using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace api.Data
{
    public sealed class SqlServerSessionOptionsInterceptor : DbConnectionInterceptor
    {
        private const string RequiredSetOptions = """
            SET QUOTED_IDENTIFIER ON;
            SET ANSI_NULLS ON;
            SET ANSI_PADDING ON;
            SET ANSI_WARNINGS ON;
            SET ARITHABORT ON;
            SET CONCAT_NULL_YIELDS_NULL ON;
            SET NUMERIC_ROUNDABORT OFF;
            """;

        public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        {
            ApplySessionOptions(connection);
        }

        public override async Task ConnectionOpenedAsync(
            DbConnection connection,
            ConnectionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            await ApplySessionOptionsAsync(connection, cancellationToken);
        }

        private static void ApplySessionOptions(DbConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = RequiredSetOptions;
            command.ExecuteNonQuery();
        }

        private static async Task ApplySessionOptionsAsync(DbConnection connection, CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = RequiredSetOptions;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
