using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Caupo.Migrations
{
    /// <inheritdoc />
    public partial class IdNarudzbe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "idNarudzbe",
                table: "NarudzbeStavke",  // Provjeri da li je to ispravan naziv tablice
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "idNarudzbe",
                table: "NarudzbeStavke");
        }
    }
}
