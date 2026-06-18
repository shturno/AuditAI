using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuditAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPasswordHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "password_hash",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                ALTER TABLE users
                ALTER COLUMN password_hash DROP DEFAULT;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "password_hash",
                table: "users");
        }
    }
}
