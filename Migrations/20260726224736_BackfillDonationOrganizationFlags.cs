using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class BackfillDonationOrganizationFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE [BeneficiaryOrganizations]
                SET
                    [IsDonationEnabled] = 1,
                    [IsEligibleForTaxReceipt] = 1,
                    [UpdatedAt] = SYSUTCDATETIME()
                WHERE [IsActive] = 1
                  AND ([IsDonationEnabled] = 0 OR [IsEligibleForTaxReceipt] = 0);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data backfill only. Do not disable donations automatically on rollback.
        }
    }
}
