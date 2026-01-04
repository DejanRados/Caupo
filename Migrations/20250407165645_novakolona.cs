using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Caupo.Migrations
{
    /// <inheritdoc />
    public partial class NovaKolona : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Konobar",
                table: "tblNarudzbeStavke",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Konobar",
                table: "tblNarudzbeStavke");
        }
    }
}
