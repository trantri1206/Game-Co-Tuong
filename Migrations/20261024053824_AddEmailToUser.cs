using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameCoTuong.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Users",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            // Gán email mặc định theo username cho các bản ghi đã có để tránh vi phạm Unique Index
            migrationBuilder.Sql("UPDATE [Users] SET [Email] = [Username] + '@example.com' WHERE [Email] = '' OR [Email] IS NULL;");

            migrationBuilder.CreateIndex(
                name: "UQ_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_Users_Email",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Users");
        }
    }
}
