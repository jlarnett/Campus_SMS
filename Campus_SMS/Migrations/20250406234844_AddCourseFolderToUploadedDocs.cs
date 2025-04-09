using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Campus_SMS.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseFolderToUploadedDocs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CourseFolder",
                table: "OpenAIUploadedDocs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CourseFolder",
                table: "OpenAIUploadedDocs");
        }
    }
}
