using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    public partial class AddEuroFundValueDateRules : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ValueDateRule",
                table: "EuroFundConfigurations",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "NextBusinessDay");

            migrationBuilder.AddColumn<int>(
                name: "ValueDateDelayDays",
                table: "EuroFundConfigurations",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "UX_EuroFundLots_SourceOperation_Contract_Fund",
                table: "EuroFundLots",
                columns: new[] { "SourceOperationId", "ContractId", "FinancialSupportId" },
                unique: true,
                filter: "[SourceOperationId] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_EuroFundLots_SourceOperation_Contract_Fund",
                table: "EuroFundLots");

            migrationBuilder.DropColumn(
                name: "ValueDateDelayDays",
                table: "EuroFundConfigurations");

            migrationBuilder.DropColumn(
                name: "ValueDateRule",
                table: "EuroFundConfigurations");
        }
    }
}
