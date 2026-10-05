using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Shiftr.Data;

#nullable disable

namespace backend.Data.Migrations;

[DbContext(typeof(ShiftrDbContext))]
[Migration("20261003150000_AddEmployeeAccessRequests")]
public partial class AddEmployeeAccessRequests : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "EmployeeAccessRequests",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                PropertyId = table.Column<int>(type: "integer", nullable: false),
                IdentityUserId = table.Column<string>(type: "text", nullable: false),
                FirstName = table.Column<string>(type: "text", nullable: false),
                LastName = table.Column<string>(type: "text", nullable: false),
                Email = table.Column<string>(type: "text", nullable: false),
                PhoneNumber = table.Column<string>(type: "text", nullable: false),
                Role = table.Column<int>(type: "integer", nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EmployeeAccessRequests", request => request.Id);
                table.ForeignKey(
                    name: "FK_EmployeeAccessRequests_AspNetUsers_IdentityUserId",
                    column: request => request.IdentityUserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_EmployeeAccessRequests_Properties_PropertyId",
                    column: request => request.PropertyId,
                    principalTable: "Properties",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_EmployeeAccessRequests_IdentityUserId",
            table: "EmployeeAccessRequests",
            column: "IdentityUserId");

        migrationBuilder.CreateIndex(
            name: "IX_EmployeeAccessRequests_PropertyId_Status",
            table: "EmployeeAccessRequests",
            columns: new[] { "PropertyId", "Status" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "EmployeeAccessRequests");
    }
}