using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddDonationPurpose : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Purpose",
                table: "Donations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE Donations
SET Purpose = NULLIF(LTRIM(RTRIM(OtherFormDescription)), ''),
    OtherFormDescription = NULL,
    DonationForm = 'ManualGiftDeclaration'
WHERE UserId IS NOT NULL
  AND DonationForm = 'Other'
  AND Purpose IS NULL
  AND OtherFormDescription IS NOT NULL
  AND LTRIM(RTRIM(OtherFormDescription)) <> '';
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Purpose",
                table: "Donations");
        }
    }
}
