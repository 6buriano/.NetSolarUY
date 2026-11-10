using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SolarUY.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IndiceFechaHoraLecturas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_lecturas_telemetria_fecha_hora",
                table: "lecturas_telemetria",
                column: "fecha_hora");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_lecturas_telemetria_fecha_hora",
                table: "lecturas_telemetria");
        }
    }
}
