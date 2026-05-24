using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledPaymentLifecycleGenerated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ScheduleGroupId",
                table: "PaymentDetails",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScheduleStatus",
                table: "PaymentDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "PaymentDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StoppedAt",
                table: "PaymentDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SuspendedAt",
                table: "PaymentDetails",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScheduleGroupId",
                table: "PaymentDetails");

            migrationBuilder.DropColumn(
                name: "ScheduleStatus",
                table: "PaymentDetails");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "PaymentDetails");

            migrationBuilder.DropColumn(
                name: "StoppedAt",
                table: "PaymentDetails");

            migrationBuilder.DropColumn(
                name: "SuspendedAt",
                table: "PaymentDetails");
        }
    }
}
