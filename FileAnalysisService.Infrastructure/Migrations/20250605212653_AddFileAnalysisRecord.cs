using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileAnalysisService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFileAnalysisRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "image_location",
                table: "file_analyses",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1024)",
                oldMaxLength: 1024);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "image_location",
                table: "file_analyses",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
