using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibApp.Migrations
{
    /// <inheritdoc />
    public partial class IsDeletedInExBook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ExampleBooks",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ExampleBooks");
        }
    }
}
