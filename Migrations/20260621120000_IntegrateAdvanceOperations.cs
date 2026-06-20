using api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    [DbContext(typeof(ApplicationDBContext))]
    [Migration("20260621120000_IntegrateAdvanceOperations")]
    public partial class IntegrateAdvanceOperations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Advances",
                type: "rowversion",
                rowVersion: true,
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "AdvanceId",
                table: "AdvanceDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "AdvanceDetails",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransactionType",
                table: "AdvanceDetails",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OperationId",
                table: "AdvanceTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceDetails_AdvanceId",
                table: "AdvanceDetails",
                column: "AdvanceId");

            migrationBuilder.CreateIndex(
                name: "UX_AdvanceTransactions_OperationId",
                table: "AdvanceTransactions",
                column: "OperationId",
                unique: true,
                filter: "[OperationId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_AdvanceDetails_Advances_AdvanceId",
                table: "AdvanceDetails",
                column: "AdvanceId",
                principalTable: "Advances",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AdvanceTransactions_Operations_OperationId",
                table: "AdvanceTransactions",
                column: "OperationId",
                principalTable: "Operations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdvanceDetails_Advances_AdvanceId",
                table: "AdvanceDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_AdvanceTransactions_Operations_OperationId",
                table: "AdvanceTransactions");

            migrationBuilder.DropIndex(
                name: "IX_AdvanceDetails_AdvanceId",
                table: "AdvanceDetails");

            migrationBuilder.DropIndex(
                name: "UX_AdvanceTransactions_OperationId",
                table: "AdvanceTransactions");

            migrationBuilder.DropColumn(
                name: "AdvanceId",
                table: "AdvanceDetails");

            migrationBuilder.DropColumn(
                name: "Comment",
                table: "AdvanceDetails");

            migrationBuilder.DropColumn(
                name: "TransactionType",
                table: "AdvanceDetails");

            migrationBuilder.DropColumn(
                name: "OperationId",
                table: "AdvanceTransactions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Advances");
        }
    }
}
