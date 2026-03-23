using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibApp.Migrations
{
    /// <inheritdoc />
    public partial class StateExampleBook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Condition",
                table: "ExampleBooks",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShelfCode",
                table: "ExampleBooks",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "ExampleBooks",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Condition",
                table: "ExampleBooks");

            migrationBuilder.DropColumn(
                name: "ShelfCode",
                table: "ExampleBooks");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ExampleBooks");
        }
    }
}
