using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Campus_SMS.Migrations
{
    /// <inheritdoc />
    public partial class connectingSmsToCourse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CourseId",
                table: "SmsInteractions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SmsInteractions_CourseId",
                table: "SmsInteractions",
                column: "CourseId");

            migrationBuilder.AddForeignKey(
                name: "FK_SmsInteractions_Courses_CourseId",
                table: "SmsInteractions",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SmsInteractions_Courses_CourseId",
                table: "SmsInteractions");

            migrationBuilder.DropIndex(
                name: "IX_SmsInteractions_CourseId",
                table: "SmsInteractions");

            migrationBuilder.DropColumn(
                name: "CourseId",
                table: "SmsInteractions");
        }
    }
}
