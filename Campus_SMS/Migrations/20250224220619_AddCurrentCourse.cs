using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Campus_SMS.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentCourse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentCourse",
                table: "SmsUsers",
                type: "nvarchar(100)",
                nullable: true,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentCourse",
                table: "SmsUsers");
        }
    }
}
