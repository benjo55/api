using Microsoft.EntityFrameworkCore.Migrations;
using System.Data.SqlClient;

public static class MigrationExtensions
{
    public static bool ColumnExists(this MigrationBuilder migrationBuilder, string tableName, string columnName, string connectionString)
    {
        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT 1 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = '{tableName}' AND COLUMN_NAME = '{columnName}'";
            var result = command.ExecuteScalar();
            return result != null;
        }
    }
}
