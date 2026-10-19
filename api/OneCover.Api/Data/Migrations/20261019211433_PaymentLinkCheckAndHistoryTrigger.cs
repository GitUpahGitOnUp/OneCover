using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneCover.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class PaymentLinkCheckAndHistoryTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_ExactlyOneLink",
                table: "Payments",
                sql: "([EnrollmentId] IS NOT NULL AND [ClaimId] IS NULL) OR ([EnrollmentId] IS NULL AND [ClaimId] IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_ExactlyOneLink",
                table: "Payments");
        }
    }
}
