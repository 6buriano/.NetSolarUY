using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SolarUY.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MigracionInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "parques_solares",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    nombre = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ubicacion = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    capacidad_instalada_kw = table.Column<double>(type: "double", nullable: false),
                    fecha_instalacion = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parques_solares", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "nodos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    nombre = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ubicacion_dentro_parque = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    parque_solar_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nodos", x => x.id);
                    table.ForeignKey(
                        name: "FK_nodos_parques_solares_parque_solar_id",
                        column: x => x.parque_solar_id,
                        principalTable: "parques_solares",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "inversores",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    modelo = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    numero_serie = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    eficiencia_conversion = table.Column<double>(type: "double", nullable: false),
                    volcado_red_activo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    nodo_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inversores", x => x.id);
                    table.ForeignKey(
                        name: "FK_inversores_nodos_nodo_id",
                        column: x => x.nodo_id,
                        principalTable: "nodos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "paneles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    modelo = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fabricante = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    potencia_nominal_w = table.Column<double>(type: "double", nullable: false),
                    estado = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fecha_instalacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    nodo_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_paneles", x => x.id);
                    table.ForeignKey(
                        name: "FK_paneles_nodos_nodo_id",
                        column: x => x.nodo_id,
                        principalTable: "nodos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sensores_dispositivos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    identificador_hardware_arm = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    version_firmware = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    activo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    inversor_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sensores_dispositivos", x => x.id);
                    table.ForeignKey(
                        name: "FK_sensores_dispositivos_inversores_inversor_id",
                        column: x => x.inversor_id,
                        principalTable: "inversores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "lecturas_telemetria",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    panel_solar_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    fecha_hora = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    corriente_continua = table.Column<double>(type: "double", nullable: false),
                    voltaje = table.Column<double>(type: "double", nullable: false),
                    potencia_generada_watts = table.Column<double>(type: "double", nullable: false),
                    temperatura_celsius = table.Column<double>(type: "double", nullable: false),
                    estado_detectado = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lecturas_telemetria", x => x.id);
                    table.ForeignKey(
                        name: "FK_lecturas_telemetria_paneles_panel_solar_id",
                        column: x => x.panel_solar_id,
                        principalTable: "paneles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mantenimientos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    panel_solar_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    tipo = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fecha_programada = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    fecha_realizada = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    descripcion = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    estado = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    personal_asignado = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mantenimientos", x => x.id);
                    table.ForeignKey(
                        name: "FK_mantenimientos_paneles_panel_solar_id",
                        column: x => x.panel_solar_id,
                        principalTable: "paneles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_inversores_nodo_id",
                table: "inversores",
                column: "nodo_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inversores_numero_serie",
                table: "inversores",
                column: "numero_serie",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lecturas_telemetria_panel_solar_id_fecha_hora",
                table: "lecturas_telemetria",
                columns: new[] { "panel_solar_id", "fecha_hora" });

            migrationBuilder.CreateIndex(
                name: "IX_mantenimientos_panel_solar_id",
                table: "mantenimientos",
                column: "panel_solar_id");

            migrationBuilder.CreateIndex(
                name: "IX_nodos_parque_solar_id",
                table: "nodos",
                column: "parque_solar_id");

            migrationBuilder.CreateIndex(
                name: "IX_paneles_nodo_id",
                table: "paneles",
                column: "nodo_id");

            migrationBuilder.CreateIndex(
                name: "IX_parques_solares_nombre",
                table: "parques_solares",
                column: "nombre");

            migrationBuilder.CreateIndex(
                name: "IX_sensores_dispositivos_inversor_id",
                table: "sensores_dispositivos",
                column: "inversor_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lecturas_telemetria");

            migrationBuilder.DropTable(
                name: "mantenimientos");

            migrationBuilder.DropTable(
                name: "sensores_dispositivos");

            migrationBuilder.DropTable(
                name: "paneles");

            migrationBuilder.DropTable(
                name: "inversores");

            migrationBuilder.DropTable(
                name: "nodos");

            migrationBuilder.DropTable(
                name: "parques_solares");
        }
    }
}
