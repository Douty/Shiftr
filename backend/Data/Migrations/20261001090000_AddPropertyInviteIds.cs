using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Shiftr.Data;

#nullable disable

namespace backend.Data.Migrations
{
    [DbContext(typeof(ShiftrDbContext))]
    [Migration("20261001090000_AddPropertyInviteIds")]
    public partial class AddPropertyInviteIds : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmployeeInviteId",
                table: "Properties",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResidentInviteId",
                table: "Properties",
                type: "text",
                nullable: true);

            migrationBuilder.Sql("UPDATE \"Properties\" SET \"EmployeeInviteId\" = gen_random_uuid()::text, \"ResidentInviteId\" = gen_random_uuid()::text");

            migrationBuilder.AlterColumn<string>(
                name: "EmployeeInviteId",
                table: "Properties",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ResidentInviteId",
                table: "Properties",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Properties_EmployeeInviteId",
                table: "Properties",
                column: "EmployeeInviteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Properties_ResidentInviteId",
                table: "Properties",
                column: "ResidentInviteId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Properties_EmployeeInviteId",
                table: "Properties");

            migrationBuilder.DropIndex(
                name: "IX_Properties_ResidentInviteId",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "EmployeeInviteId",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "ResidentInviteId",
                table: "Properties");
        }
    }
}