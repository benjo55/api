using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleGroupIdForManualWithdrawalArbitrage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ScheduleGroupId",
                table: "WithdrawalDetails",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScheduleGroupId",
                table: "ArbitrageDetails",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScheduleGroupId",
                table: "WithdrawalDetails");

            migrationBuilder.DropColumn(
                name: "ScheduleGroupId",
                table: "ArbitrageDetails");
        }
    }
}
