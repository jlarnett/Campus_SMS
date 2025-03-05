using Microsoft.EntityFrameworkCore.Migrations;
using System.Net.NetworkInformation;

#nullable disable

namespace Campus_SMS.Migrations
{
    public partial class AddSmsUserTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create the SmsUser table
            migrationBuilder.CreateTable(
                name: "SmsUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsFirstTime = table.Column<bool>(type: "bit", nullable: false),
                    OptStatus = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmsUsers", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the SmsUser table if rolling back
            migrationBuilder.DropTable(
                name: "SmsUsers");
        }
    }
}
