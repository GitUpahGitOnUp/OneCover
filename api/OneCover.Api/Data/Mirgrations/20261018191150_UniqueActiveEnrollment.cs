using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneCover.Api.Data.Mirgrations
{
    /// <inheritdoc />
    public partial class UniqueActiveEnrollment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Enrollments_UserId",
                table: "Enrollments");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_UserId_PlanId",
                table: "Enrollments",
                columns: new[] { "UserId", "PlanId" },
                unique: true,
                filter: "[Status] = 'Active'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Enrollments_UserId_PlanId",
                table: "Enrollments");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_UserId",
                table: "Enrollments",
                column: "UserId");
        }
    }
}
