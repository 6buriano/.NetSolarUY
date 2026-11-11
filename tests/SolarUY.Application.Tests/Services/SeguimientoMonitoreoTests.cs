using SolarUY.Application.DTOs;
using SolarUY.Application.Services;
using SolarUY.Domain.Entities;
using SolarUY.Domain.Enums;

namespace SolarUY.Application.Tests.Services;

/// <summary>
/// Pruebas de <see cref="SeguimientoMonitoreo.Comparar"/>, que tiene memoria entre vueltas: se le pasa
/// la foto de cada ronda del worker y devuelve solo lo nuevo.
/// </summary>
public class SeguimientoMonitoreoTests
{
    private static readonly DateTime Ahora = new(2026, 11, 11, 15, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void PrimeraVuelta_NoMandaLecturasNiFallas()
    {
        var (parque, nodo) = CrearParqueConNodo();
        var operativo = FixtureMonitoreo.Panel(nodo, "P01");
        var enFalla = FixtureMonitoreo.Panel(nodo, "P02", EstadoSalud.FALLA_DETECTADA);
        var lecturas = new[]
        {
            FixtureMonitoreo.Lectura(operativo, Ahora, 100),
            FixtureMonitoreo.Lectura(enFalla, Ahora, 0, 80)
        };

        var cambios = new SeguimientoMonitoreo().Comparar(Instantanea(parque, lecturas), lecturas);

        Assert.Empty(cambios.LecturasNuevas);
        Assert.Empty(cambios.Fallas);
    }

    [Fact]
    public void SegundaVuelta_MandaSoloLaLecturaNueva()
    {
        var (parque, nodo) = CrearParqueConNodo();
        var panel = FixtureMonitoreo.Panel(nodo, "P01");
        var primera = new[] { FixtureMonitoreo.Lectura(panel, Ahora, 100) };
        var seguimiento = new SeguimientoMonitoreo();

        seguimiento.Comparar(Instantanea(parque, primera), primera);

        var segunda = new[]
        {
            primera[0],
            FixtureMonitoreo.Lectura(panel, Ahora.AddSeconds(5), 120)
        };
        var cambios = seguimiento.Comparar(Instantanea(parque, segunda), segunda);

        var lecturaNueva = Assert.Single(cambios.LecturasNuevas);
        Assert.Equal(120, lecturaNueva.PotenciaGeneradaWatts);
        Assert.Equal(Ahora.AddSeconds(5), lecturaNueva.FechaHora);
    }

    [Fact]
    public void NoRepiteUnaLecturaQueYaHabiaMandado()
    {
        var (parque, nodo) = CrearParqueConNodo();
        var panel = FixtureMonitoreo.Panel(nodo, "P01");
        var primera = new[] { FixtureMonitoreo.Lectura(panel, Ahora, 100) };
        var segunda = new[] { FixtureMonitoreo.Lectura(panel, Ahora.AddSeconds(5), 120) };
        var seguimiento = new SeguimientoMonitoreo();

        seguimiento.Comparar(Instantanea(parque, primera), primera);
        seguimiento.Comparar(Instantanea(parque, segunda), segunda);

        // La ventana vuelve a traer las dos: ninguna es nueva.
        var tercera = new[] { primera[0], segunda[0] };
        var cambios = seguimiento.Comparar(Instantanea(parque, tercera), tercera);

        Assert.Empty(cambios.LecturasNuevas);
    }

    [Fact]
    public void PanelQuePasaAFalla_GeneraLaAlerta()
    {
        var (parque, nodo) = CrearParqueConNodo();
        var panel = FixtureMonitoreo.Panel(nodo, "P01");
        var seguimiento = new SeguimientoMonitoreo();

        var antes = new[] { FixtureMonitoreo.Lectura(panel, Ahora, 100) };
        seguimiento.Comparar(Instantanea(parque, antes), antes);

        // El dominio lo diagnostica en falla por sobretemperatura.
        var enFalla = new[] { FixtureMonitoreo.Lectura(panel, Ahora.AddSeconds(5), 0, 80) };
        nodo.EvaluarLecturas(enFalla);

        var cambios = seguimiento.Comparar(Instantanea(parque, enFalla), enFalla);

        Assert.Equal(EstadoSalud.FALLA_DETECTADA, panel.Estado);
        var alerta = Assert.Single(cambios.Fallas);
        Assert.Equal(panel.Id, alerta.PanelId);
        Assert.Equal("P01", alerta.CodigoPanel);
        Assert.Equal("Nodo A", alerta.NodoNombre);
        Assert.Equal(parque.Id, alerta.ParqueId);
    }

    [Fact]
    public void PanelQueYaEstabaEnFalla_NoGeneraAlerta()
    {
        var (parque, nodo) = CrearParqueConNodo();
        var panel = FixtureMonitoreo.Panel(nodo, "P01", EstadoSalud.FALLA_DETECTADA);
        var lecturas = new[] { FixtureMonitoreo.Lectura(panel, Ahora, 0, 80) };
        var seguimiento = new SeguimientoMonitoreo();

        // Primera vuelta: se registra como visto.
        seguimiento.Comparar(Instantanea(parque, lecturas), lecturas);

        var cambios = seguimiento.Comparar(Instantanea(parque, lecturas), lecturas);

        Assert.Empty(cambios.Fallas);
    }

    [Fact]
    public void MientrasSigueEnFalla_NoRepiteElAviso()
    {
        var (parque, nodo) = CrearParqueConNodo();
        var panel = FixtureMonitoreo.Panel(nodo, "P01");
        var seguimiento = new SeguimientoMonitoreo();

        var operativo = new[] { FixtureMonitoreo.Lectura(panel, Ahora, 100) };
        seguimiento.Comparar(Instantanea(parque, operativo), operativo);

        var enFalla = new[] { FixtureMonitoreo.Lectura(panel, Ahora.AddSeconds(5), 0, 80) };
        nodo.EvaluarLecturas(enFalla);
        Assert.Single(seguimiento.Comparar(Instantanea(parque, enFalla), enFalla).Fallas);

        // Siguiente ronda: sigue en falla, no vuelve a avisar.
        var sigueEnFalla = new[] { FixtureMonitoreo.Lectura(panel, Ahora.AddSeconds(10), 0, 82) };
        nodo.EvaluarLecturas(sigueEnFalla);
        Assert.Empty(seguimiento.Comparar(Instantanea(parque, sigueEnFalla), sigueEnFalla).Fallas);
    }

    [Fact]
    public void SiSeRecuperaYVuelveAFallar_AvisaDeNuevo()
    {
        var (parque, nodo) = CrearParqueConNodo();
        var panel = FixtureMonitoreo.Panel(nodo, "P01");
        var seguimiento = new SeguimientoMonitoreo();

        var operativo = new[] { FixtureMonitoreo.Lectura(panel, Ahora, 100) };
        seguimiento.Comparar(Instantanea(parque, operativo), operativo);

        // Pasa a falla: avisa.
        var enFalla = new[] { FixtureMonitoreo.Lectura(panel, Ahora.AddSeconds(5), 0, 80) };
        nodo.EvaluarLecturas(enFalla);
        Assert.Single(seguimiento.Comparar(Instantanea(parque, enFalla), enFalla).Fallas);

        // Se recupera (temperatura normal): no avisa de nada.
        var recuperado = new[] { FixtureMonitoreo.Lectura(panel, Ahora.AddSeconds(10), 90, 45) };
        nodo.EvaluarLecturas(recuperado);
        Assert.Equal(EstadoSalud.OPERATIVO, panel.Estado);
        Assert.Empty(seguimiento.Comparar(Instantanea(parque, recuperado), recuperado).Fallas);

        // Vuelve a fallar: avisa otra vez.
        var fallaDeNuevo = new[] { FixtureMonitoreo.Lectura(panel, Ahora.AddSeconds(15), 0, 85) };
        nodo.EvaluarLecturas(fallaDeNuevo);
        Assert.Single(seguimiento.Comparar(Instantanea(parque, fallaDeNuevo), fallaDeNuevo).Fallas);
    }

    private static (ParqueSolar Parque, Nodo Nodo) CrearParqueConNodo()
    {
        var parque = FixtureMonitoreo.Parque();
        return (parque, FixtureMonitoreo.Nodo(parque));
    }

    private static InstantaneaMonitoreoDto Instantanea(ParqueSolar parque, IReadOnlyList<LecturaTelemetria> lecturas)
        => MonitoreoService.ArmarInstantanea([parque], lecturas, Ahora);
}
