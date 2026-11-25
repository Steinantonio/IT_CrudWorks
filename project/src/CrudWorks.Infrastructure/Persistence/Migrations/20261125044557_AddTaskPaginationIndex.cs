using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrudWorks.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskPaginationIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tasks_UserId",
                table: "Tasks");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_UserId_CreatedAtUtc_Id",
                table: "Tasks",
                columns: new[] { "UserId", "CreatedAtUtc", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tasks_UserId_CreatedAtUtc_Id",
                table: "Tasks");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_UserId",
                table: "Tasks",
                column: "UserId");
        }
    }
}
