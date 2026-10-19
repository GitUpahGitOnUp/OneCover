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

            // the append-only history trigger, carried by the migration so a fresh or rebuilt database always gets it
            // (same SQL as sql/ClaimStatusHistory_AppendOnly.sql, which stays as readable documentation).
            // CREATE OR ALTER = safe even where the trigger was already created by hand in SSMS
            migrationBuilder.Sql("""
                CREATE OR ALTER TRIGGER dbo.TR_ClaimStatusHistories_AppendOnly
                ON dbo.ClaimStatusHistories
                INSTEAD OF UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 50001, 'Claim status history is append-only: rows cannot be updated or deleted.', 1;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS dbo.TR_ClaimStatusHistories_AppendOnly;");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_ExactlyOneLink",
                table: "Payments");
        }
    }
}
