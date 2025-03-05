using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Campus_SMS.Migrations
{
    /// <inheritdoc />
    public partial class OpenAiDocTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OpenAIUploadedDocs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DocumentID = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenAIUploadedDocs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OpenAIUploadedDocs");
        }
    }
}
