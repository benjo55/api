using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicUserRegistrationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(254)",
                maxLength: 254,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Users",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedEmail",
                table: "Users",
                type: "nvarchar(254)",
                maxLength: 254,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedUsername",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE [Users]
                SET
                    [NormalizedUsername] = UPPER(LTRIM(RTRIM([Username]))),
                    [NormalizedEmail] = UPPER(LTRIM(RTRIM([Email]))),
                    [CreatedDate] = SYSUTCDATETIME()
                WHERE [NormalizedUsername] = '' OR [NormalizedEmail] = '';
                """);

            migrationBuilder.Sql("""
                WITH DuplicateEmails AS
                (
                    SELECT
                        [Id],
                        ROW_NUMBER() OVER (PARTITION BY [NormalizedEmail] ORDER BY [Id]) AS RowNumber
                    FROM [Users]
                    WHERE [NormalizedEmail] <> ''
                )
                UPDATE [Users]
                SET [NormalizedEmail] = CONCAT([Users].[NormalizedEmail], '#DUPLICATE-', [Users].[Id])
                FROM [Users]
                INNER JOIN DuplicateEmails ON [Users].[Id] = DuplicateEmails.[Id]
                WHERE DuplicateEmails.RowNumber > 1;
                """);

            migrationBuilder.Sql("""
                WITH DuplicateUserNames AS
                (
                    SELECT
                        [Id],
                        ROW_NUMBER() OVER (PARTITION BY [NormalizedUsername] ORDER BY [Id]) AS RowNumber
                    FROM [Users]
                    WHERE [NormalizedUsername] <> ''
                )
                UPDATE [Users]
                SET [NormalizedUsername] = CONCAT([Users].[NormalizedUsername], '#DUPLICATE-', [Users].[Id])
                FROM [Users]
                INNER JOIN DuplicateUserNames ON [Users].[Id] = DuplicateUserNames.[Id]
                WHERE DuplicateUserNames.RowNumber > 1;
                """);

            migrationBuilder.CreateIndex(
                name: "UX_Users_NormalizedEmail",
                table: "Users",
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Users_NormalizedUsername",
                table: "Users",
                column: "NormalizedUsername",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Users_NormalizedEmail",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "UX_Users_NormalizedUsername",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NormalizedEmail",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NormalizedUsername",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(254)",
                oldMaxLength: 254);
        }
    }
}
