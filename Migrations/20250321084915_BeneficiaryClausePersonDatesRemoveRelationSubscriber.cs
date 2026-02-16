using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class BeneficiaryClausePersonDatesRemoveRelationSubscriber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RelationWithSubscriber",
                table: "BeneficiaryClauses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RelationWithSubscriber",
                table: "BeneficiaryClauses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
