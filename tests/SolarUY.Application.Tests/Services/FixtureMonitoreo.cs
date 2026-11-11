using SolarUY.Domain.Entities;
using SolarUY.Domain.Enums;

namespace SolarUY.Application.Tests.Services;

/// <summary>
/// Arma el grafo mínimo parque → nodo → panel para las pruebas del monitoreo (req012). Las colecciones
/// del dominio son listas con setter privado, así que se llenan agregando.
/// </summary>
internal static class FixtureMonitoreo
{
    private static readonly DateTime FechaInstalacion = new(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);

    public static ParqueSolar Parque(string nombre = "Parque Norte")
        => new(nombre, "Ruta 5 km 20", 100, FechaInstalacion);

    public static Nodo Nodo(ParqueSolar parque, string nombre = "Nodo A")
    {
        var nodo = new Nodo(nombre, "Sector A", parque.Id,
            new Inversor("SUN2000-5KTL", $"SN-{Guid.NewGuid():N}"[..12], 0.97, false));

        parque.Nodos.Add(nodo);
        return nodo;
    }

    public static PanelSolar Panel(Nodo nodo, string codigo, EstadoSalud estado = EstadoSalud.OPERATIVO,
        bool activo = true)
    {
        var panel = new PanelSolar(codigo, "CS6P-300", "Canadian Solar", 300, estado, FechaInstalacion,
            nodo.Id, activo);

        nodo.Paneles.Add(panel);
        return panel;
    }

    public static LecturaTelemetria Lectura(PanelSolar panel, DateTime fechaHora, double potenciaW,
        double temperaturaCelsius = 45)
        => new(panel.Id, fechaHora, 8, 36, potenciaW, temperaturaCelsius);
}
