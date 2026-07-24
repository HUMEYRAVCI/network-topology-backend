using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetworkTopologyTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBrandColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Brand",
                table: "Devices");

            migrationBuilder.AlterColumn<string>(
                name: "Model",
                table: "Devices",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Model",
                table: "Devices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Brand",
                table: "Devices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
