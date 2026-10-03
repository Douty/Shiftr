using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Shiftr.Data;

#nullable disable

namespace backend.Data.Migrations
{
    [DbContext(typeof(ShiftrDbContext))]
    [Migration("20261002200000_AddResidentUnitNumber")]
    public partial class AddResidentUnitNumber : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UnitNumber",
                table: "Residents",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "UnitNumber", table: "Residents");
        }
    }
}