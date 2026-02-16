using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualBasic;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class SyncSchema : Migration
    {
        /// <inheritdoc />
        /// 

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var connectionString = "Data Source=BOOK-PBE\\SQLEXPRESS;Initial Catalog=Life;Integrated Security=True;Encrypt=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";

            if (!migrationBuilder.ColumnExists("Products", "CreatedDate", connectionString))
            {
                migrationBuilder.AddColumn<DateTime>(
                    name: "CreatedDate",
                    table: "Products",
                    type: "datetime2",
                    nullable: false,
                    defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
            }
            if (!migrationBuilder.ColumnExists("Products", "UpdatedDate", connectionString))
            {
                migrationBuilder.AddColumn<DateTime>(
                    name: "UpdatedDate",
                    table: "Products",
                    type: "datetime2",
                    nullable: true);
            }
            if (!migrationBuilder.ColumnExists("Persons", "CreatedDate", connectionString))
            {
                migrationBuilder.AddColumn<DateTime>(
                    name: "CreatedDate",
                    table: "Persons",
                    type: "datetime2",
                    nullable: false,
                    defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
            }

            if (!migrationBuilder.ColumnExists("Persons", "UpdatedDate", connectionString))
            {
                migrationBuilder.AddColumn<DateTime>(
                    name: "UpdatedDate",
                    table: "Persons",
                    type: "datetime2",
                    nullable: false,
                    defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
            }

            if (!migrationBuilder.ColumnExists("Contracts", "CreatedDate", connectionString))
            {
                migrationBuilder.AddColumn<DateTime>(
                    name: "CreatedDate",
                    table: "Contracts",
                    type: "datetime2",
                    nullable: false,
                    defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
            }
            if (!migrationBuilder.ColumnExists("Contracts", "UpdatedDate", connectionString))
            {
                migrationBuilder.AddColumn<DateTime>(
                    name: "UpdatedDate",
                    table: "Contracts",
                    type: "datetime2",
                    nullable: true);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "Contracts");
        }
    }
}
