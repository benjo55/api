using api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    [DbContext(typeof(ApplicationDBContext))]
    [Migration("20260622090000_AddOperationRequestedExecutedAmounts")]
    public partial class AddOperationRequestedExecutedAmounts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RequestedAmount",
                table: "Operations",
                type: "decimal(20,7)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExecutedAmount",
                table: "Operations",
                type: "decimal(20,7)",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE Operations SET RequestedAmount = Amount, " +
                "ExecutedAmount = CASE WHEN Status = 1 THEN Amount ELSE NULL END;");

            migrationBuilder.Sql(@"
                UPDATE o
                SET ExecutedAmount = x.ExecutedAmount,
                    Amount = x.ExecutedAmount
                FROM Operations o
                CROSS APPLY (
                    SELECT SUM(a.Amount) AS ExecutedAmount
                    FROM OperationSupportAllocations a
                    WHERE a.OperationId = o.Id AND a.Flow = 0
                ) x
                WHERE o.Status = 1
                  AND o.Type IN (11, 12)
                  AND x.ExecutedAmount IS NOT NULL;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ExecutedAmount", table: "Operations");
            migrationBuilder.DropColumn(name: "RequestedAmount", table: "Operations");
        }
    }
}
