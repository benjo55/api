using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AllowSameSupportCompartmentByFlowOnOperationAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_OSA_Operation_Support_Compartment",
                table: "OperationSupportAllocations");

            migrationBuilder.CreateIndex(
                name: "UX_OSA_Operation_Support_Compartment_Flow",
                table: "OperationSupportAllocations",
                columns: new[] { "OperationId", "SupportId", "CompartmentId", "Flow" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_OSA_Operation_Support_Compartment_Flow",
                table: "OperationSupportAllocations");

            migrationBuilder.CreateIndex(
                name: "UX_OSA_Operation_Support_Compartment",
                table: "OperationSupportAllocations",
                columns: new[] { "OperationId", "SupportId", "CompartmentId" },
                unique: true);
        }
    }
}
