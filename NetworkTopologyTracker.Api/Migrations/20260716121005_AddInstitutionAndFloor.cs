using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetworkTopologyTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddInstitutionAndFloor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rooms_Facilities_FacilityId",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "FloorInfo",
                table: "Rooms");

            migrationBuilder.RenameColumn(
                name: "FacilityId",
                table: "Rooms",
                newName: "FloorId");

            migrationBuilder.RenameIndex(
                name: "IX_Rooms_FacilityId",
                table: "Rooms",
                newName: "IX_Rooms_FloorId");

            migrationBuilder.AddColumn<string>(
                name: "FacilityType",
                table: "Facilities",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "InstitutionId",
                table: "Facilities",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Floors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    FacilityId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Floors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Floors_Facilities_FacilityId",
                        column: x => x.FacilityId,
                        principalTable: "Facilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Institutions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Institutions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Facilities_InstitutionId",
                table: "Facilities",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_Floors_FacilityId",
                table: "Floors",
                column: "FacilityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Facilities_Institutions_InstitutionId",
                table: "Facilities",
                column: "InstitutionId",
                principalTable: "Institutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Rooms_Floors_FloorId",
                table: "Rooms",
                column: "FloorId",
                principalTable: "Floors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Facilities_Institutions_InstitutionId",
                table: "Facilities");

            migrationBuilder.DropForeignKey(
                name: "FK_Rooms_Floors_FloorId",
                table: "Rooms");

            migrationBuilder.DropTable(
                name: "Floors");

            migrationBuilder.DropTable(
                name: "Institutions");

            migrationBuilder.DropIndex(
                name: "IX_Facilities_InstitutionId",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "FacilityType",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "InstitutionId",
                table: "Facilities");

            migrationBuilder.RenameColumn(
                name: "FloorId",
                table: "Rooms",
                newName: "FacilityId");

            migrationBuilder.RenameIndex(
                name: "IX_Rooms_FloorId",
                table: "Rooms",
                newName: "IX_Rooms_FacilityId");

            migrationBuilder.AddColumn<string>(
                name: "FloorInfo",
                table: "Rooms",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Rooms_Facilities_FacilityId",
                table: "Rooms",
                column: "FacilityId",
                principalTable: "Facilities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
