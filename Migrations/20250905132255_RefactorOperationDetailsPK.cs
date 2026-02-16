using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class RefactorOperationDetailsPK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_WithdrawalDetails",
                table: "WithdrawalDetails");

            migrationBuilder.DropIndex(
                name: "IX_WithdrawalDetails_OperationId",
                table: "WithdrawalDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PaymentDetails",
                table: "PaymentDetails");

            migrationBuilder.DropIndex(
                name: "IX_PaymentDetails_OperationId",
                table: "PaymentDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ArbitrageDetails",
                table: "ArbitrageDetails");

            migrationBuilder.DropIndex(
                name: "IX_ArbitrageDetails_OperationId",
                table: "ArbitrageDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AdvanceDetails",
                table: "AdvanceDetails");

            migrationBuilder.DropIndex(
                name: "IX_AdvanceDetails_OperationId",
                table: "AdvanceDetails");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "WithdrawalDetails");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "PaymentDetails");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ArbitrageDetails");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "AdvanceDetails");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WithdrawalDetails",
                table: "WithdrawalDetails",
                column: "OperationId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PaymentDetails",
                table: "PaymentDetails",
                column: "OperationId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ArbitrageDetails",
                table: "ArbitrageDetails",
                column: "OperationId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AdvanceDetails",
                table: "AdvanceDetails",
                column: "OperationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_WithdrawalDetails",
                table: "WithdrawalDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PaymentDetails",
                table: "PaymentDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ArbitrageDetails",
                table: "ArbitrageDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AdvanceDetails",
                table: "AdvanceDetails");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "WithdrawalDetails",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "PaymentDetails",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "ArbitrageDetails",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "AdvanceDetails",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WithdrawalDetails",
                table: "WithdrawalDetails",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PaymentDetails",
                table: "PaymentDetails",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ArbitrageDetails",
                table: "ArbitrageDetails",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AdvanceDetails",
                table: "AdvanceDetails",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalDetails_OperationId",
                table: "WithdrawalDetails",
                column: "OperationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentDetails_OperationId",
                table: "PaymentDetails",
                column: "OperationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArbitrageDetails_OperationId",
                table: "ArbitrageDetails",
                column: "OperationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceDetails_OperationId",
                table: "AdvanceDetails",
                column: "OperationId",
                unique: true);
        }
    }
}
