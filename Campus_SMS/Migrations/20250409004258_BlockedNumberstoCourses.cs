using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Campus_SMS.Migrations
{
    /// <inheritdoc />
    public partial class BlockedNumberstoCourses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BlockedNumbers",
                table: "Courses",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlockedNumbers",
                table: "Courses");
        }
    }
}
