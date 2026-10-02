using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Shiftr.Data;

#nullable disable

namespace backend.Data.Migrations
{
    [DbContext(typeof(ShiftrDbContext))]
    [Migration("20261002190000_AddResidentPreferences")]
    public partial class AddResidentPreferences : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AllowedGuests",
                table: "Residents",
                type: "text",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<bool>(
                name: "CallToNotify",
                table: "Residents",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "AllowedGuests", table: "Residents");
            migrationBuilder.DropColumn(name: "CallToNotify", table: "Residents");
        }
    }
}