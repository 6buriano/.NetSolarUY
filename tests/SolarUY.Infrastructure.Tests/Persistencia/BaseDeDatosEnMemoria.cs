using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SolarUY.Domain.Entities;
using SolarUY.Domain.Enums;
using SolarUY.Infrastructure.Persistence;

namespace SolarUY.Infrastructure.Tests.Persistencia;

/// <summary>
/// Base de datos SQLite en memoria con el modelo real de EF (<see cref="SolarUYDbContext"/> y todas sus
/// configuraciones). Vive mientras viva la conexión, así que cada <see cref="NuevoContexto"/> ve lo que
/// guardó el anterior: sirve para comprobar que un dato se escribió de verdad, sin la caché del contexto.
/// </summary>
internal sealed class BaseDeDatosEnMemoria : IDisposable
{
    private readonly SqliteConnection _conexion;

    public BaseDeDatosEnMemoria()
    {
        _conexion = new SqliteConnection("DataSource=:memory:");
        _conexion.Open();

        // SQLite no hace cumplir las claves foráneas (ni los borrados en cascada) hasta que se le pide.
        using (var comando = _conexion.CreateCommand())
        {
            comando.CommandText = "PRAGMA foreign_keys = ON;";
            comando.ExecuteNonQuery();
        }

        using var contexto = NuevoContexto();
        contexto.Database.EnsureCreated();
    }

    public SolarUYDbContext NuevoContexto()
        => new(new DbContextOptionsBuilder<SolarUYDbContext>().UseSqlite(_conexion).Options);

    /// <summary>Guarda entidades directamente, sin pasar por los repositorios, para preparar la prueba.</summary>
    public async Task GuardarAsync(params object[] entidades)
    {
        await using var contexto = NuevoContexto();
        contexto.AddRange(entidades);
        await contexto.SaveChangesAsync();
    }

    public void Dispose() => _conexion.Dispose();
}

/// <summary>Entidades válidas para armar los datos de las pruebas.</summary>
internal static class Datos
{
    public static readonly DateTime Instalacion = new(2026, 1, 15);

    public static ParqueSolar NuevoParque(string nombre = "Parque Norte")
        => new(nombre, "Salto", 12.5, Instalacion);

    public static Nodo NuevoNodo(Guid parqueId, string nombre = "Nodo A", string numeroSerie = "SN-001",
        string? hardware = null, bool sensorActivo = true)
    {
        var inversor = new Inversor("Huawei SUN2000", numeroSerie, 98.5, true);

        if (hardware is not null)
        {
            inversor.RegistrarSensor(new SensorDispositivo(hardware, "1.0.0", sensorActivo, "http://sensor-a:5100"));
        }

        return new Nodo(nombre, "Sector 1", parqueId, inversor);
    }

    public static PanelSolar NuevoPanel(Guid nodoId, string codigo = "P01", string modelo = "JAM72S30",
        string fabricante = "JA Solar", EstadoSalud estado = EstadoSalud.OPERATIVO)
        => new(codigo, modelo, fabricante, 550d, estado, Instalacion, nodoId);

    public static LecturaTelemetria NuevaLectura(Guid panelId, DateTime fechaHora, double potenciaW = 300d)
        => new(panelId, fechaHora, 8d, 40d, potenciaW, 35d);

    public static TareaMantenimiento NuevaTarea(Guid panelId, DateTime fechaProgramada,
        TipoMantenimiento tipo = TipoMantenimiento.LIMPIEZA, string descripcion = "Limpieza general")
        => new(panelId, tipo, fechaProgramada, descripcion, "Ana");

    /// <summary>Un parque con un nodo y un panel, ya guardados.</summary>
    public static async Task<(ParqueSolar Parque, Nodo Nodo, PanelSolar Panel)> SembrarAsync(
        BaseDeDatosEnMemoria baseDeDatos, string sufijo = "")
    {
        var parque = NuevoParque($"Parque{sufijo}");
        var nodo = NuevoNodo(parque.Id, $"Nodo{sufijo}", $"SN-{sufijo}001");
        var panel = NuevoPanel(nodo.Id);

        await baseDeDatos.GuardarAsync(parque, nodo, panel);
        return (parque, nodo, panel);
    }
}
