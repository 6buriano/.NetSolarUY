using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SolarUY.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Vincula los paneles con el hardware: agrega el código de panel y su estado de actividad,
    /// y la dirección del sensor ARM (a la que el colector le pide la telemetría).
    /// </summary>
    public partial class VinculacionConElSensor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "url_base",
                table: "sensores_dispositivos",
                type: "varchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "activo",
                table: "paneles",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "codigo_panel",
                table: "paneles",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            // Relleno de los datos que ya existían, antes de crear el índice único: si no, todos
            // los paneles quedarían con código vacío y el índice no se podría crear.
            // Los códigos se numeran por nodo en orden de instalación, que es el mismo criterio
            // que usa el simulador (P01, P02, ...).
            migrationBuilder.Sql(
                "CREATE TEMPORARY TABLE paneles_numerados AS " +
                "SELECT id, CONCAT('P', LPAD(ROW_NUMBER() OVER (PARTITION BY nodo_id ORDER BY fecha_instalacion, id), 2, '0')) AS codigo " +
                "FROM paneles;");

            migrationBuilder.Sql(
                "UPDATE paneles AS p " +
                "JOIN paneles_numerados AS n ON n.id = p.id " +
                "SET p.codigo_panel = n.codigo " +
                "WHERE p.codigo_panel = '';");

            migrationBuilder.Sql("DROP TEMPORARY TABLE paneles_numerados;");

            // Los paneles que ya existían estaban habilitados: el false por defecto de la columna
            // nueva no representa su estado real.
            migrationBuilder.Sql("UPDATE paneles SET activo = 1;");

            // Los sensores ya registrados apuntan al simulador por defecto.
            migrationBuilder.Sql("UPDATE sensores_dispositivos SET url_base = 'http://localhost:5100' WHERE url_base = '';");

            // El índice nuevo se crea antes de borrar el viejo: MySQL no permite dejar sin índice
            // a la clave foránea, y el compuesto empieza por nodo_id, así que le sirve igual.
            migrationBuilder.CreateIndex(
                name: "IX_paneles_nodo_id_codigo_panel",
                table: "paneles",
                columns: new[] { "nodo_id", "codigo_panel" },
                unique: true);

            migrationBuilder.DropIndex(
                name: "IX_paneles_nodo_id",
                table: "paneles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Mismo cuidado al revés: primero el índice que la clave foránea necesita.
            migrationBuilder.CreateIndex(
                name: "IX_paneles_nodo_id",
                table: "paneles",
                column: "nodo_id");

            migrationBuilder.DropIndex(
                name: "IX_paneles_nodo_id_codigo_panel",
                table: "paneles");

            migrationBuilder.DropColumn(
                name: "url_base",
                table: "sensores_dispositivos");

            migrationBuilder.DropColumn(
                name: "activo",
                table: "paneles");

            migrationBuilder.DropColumn(
                name: "codigo_panel",
                table: "paneles");
        }
    }
}
