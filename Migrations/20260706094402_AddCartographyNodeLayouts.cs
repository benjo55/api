using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddCartographyNodeLayouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CartographyNodeLayouts",
                schema: "cmdb",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScopeType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ScopeKey = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ConfigurationItemId = table.Column<int>(type: "int", nullable: false),
                    PositionX = table.Column<double>(type: "float", nullable: false),
                    PositionY = table.Column<double>(type: "float", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartographyNodeLayouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CartographyNodeLayouts_ConfigurationItems_ConfigurationItemId",
                        column: x => x.ConfigurationItemId,
                        principalSchema: "cmdb",
                        principalTable: "ConfigurationItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CartographyNodeLayouts_ConfigurationItemId",
                schema: "cmdb",
                table: "CartographyNodeLayouts",
                column: "ConfigurationItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CartographyNodeLayouts_ScopeType_ScopeKey_UserName_ConfigurationItemId",
                schema: "cmdb",
                table: "CartographyNodeLayouts",
                columns: new[] { "ScopeType", "ScopeKey", "UserName", "ConfigurationItemId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CartographyNodeLayouts",
                schema: "cmdb");
        }
    }
}
