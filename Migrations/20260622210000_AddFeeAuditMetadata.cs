using api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    [DbContext(typeof(ApplicationDBContext))]
    [Migration("20260622210000_AddFeeAuditMetadata")]
    public partial class AddFeeAuditMetadata : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AccumulatedBaseAmount",
                table: "ContractManagementFeeAccruals",
                type: "decimal(20,7)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "AccruedDays",
                table: "ContractManagementFeeAccruals",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "AccrualStartDate",
                table: "ContractManagementFeeAccruals",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(name: "AccrualEndDate", table: "ContractSupportFeeApplications", type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "AccrualStartDate", table: "ContractSupportFeeApplications", type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<int>(name: "AccruedDays", table: "ContractSupportFeeApplications", type: "int", nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "AppliedRate", table: "ContractSupportFeeApplications", type: "decimal(18,7)", nullable: true);
            migrationBuilder.AddColumn<int>(name: "FeeMode", table: "ContractSupportFeeApplications", type: "int", nullable: true);
            migrationBuilder.AddColumn<int>(name: "Frequency", table: "ContractSupportFeeApplications", type: "int", nullable: true);
            migrationBuilder.AddColumn<int>(name: "PostingMode", table: "ContractSupportFeeApplications", type: "int", nullable: true);
            migrationBuilder.AddColumn<int>(name: "ProrataMethod", table: "ContractSupportFeeApplications", type: "int", nullable: true);
            migrationBuilder.AddColumn<int>(name: "RateBase", table: "ContractSupportFeeApplications", type: "int", nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "AccumulatedBaseAmount", table: "ContractManagementFeeAccruals");
            migrationBuilder.DropColumn(name: "AccruedDays", table: "ContractManagementFeeAccruals");
            migrationBuilder.DropColumn(name: "AccrualStartDate", table: "ContractManagementFeeAccruals");

            migrationBuilder.DropColumn(name: "AccrualEndDate", table: "ContractSupportFeeApplications");
            migrationBuilder.DropColumn(name: "AccrualStartDate", table: "ContractSupportFeeApplications");
            migrationBuilder.DropColumn(name: "AccruedDays", table: "ContractSupportFeeApplications");
            migrationBuilder.DropColumn(name: "AppliedRate", table: "ContractSupportFeeApplications");
            migrationBuilder.DropColumn(name: "FeeMode", table: "ContractSupportFeeApplications");
            migrationBuilder.DropColumn(name: "Frequency", table: "ContractSupportFeeApplications");
            migrationBuilder.DropColumn(name: "PostingMode", table: "ContractSupportFeeApplications");
            migrationBuilder.DropColumn(name: "ProrataMethod", table: "ContractSupportFeeApplications");
            migrationBuilder.DropColumn(name: "RateBase", table: "ContractSupportFeeApplications");
        }
    }
}
