using Microsoft.EntityFrameworkCore;
using SolarUY.Domain.Entities;
using SolarUY.Domain.Enums;

namespace SolarUY.Infrastructure.Tests.Persistencia;

/// <summary>
/// Pruebas del modelo de EF (<see cref="SolarUY.Infrastructure.Persistence.SolarUYDbContext"/> y sus
/// configuraciones): tablas, restricciones de unicidad, conversión de enums y datos obligatorios.
/// Corren contra SQLite, que aplica las mismas claves foráneas e índices únicos que MySQL.
/// </summary>
public class ModeloDeDatosTests
{
    [Fact]
    public void ElModelo_MapeaCadaEntidadASuTabla()
    {
        using var db = new BaseDeDatosEnMemoria();
        using var contexto = db.NuevoContexto();

        var tablas = new Dictionary<Type, string>
        {
            [typeof(ParqueSolar)] = "parques_solares",
            [typeof(Nodo)] = "nodos",
            [typeof(Inversor)] = "inversores",
            [typeof(SensorDispositivo)] = "sensores_dispositivos",
            [typeof(PanelSolar)] = "paneles",
            [typeof(LecturaTelemetria)] = "lecturas_telemetria",
            [typeof(TareaMantenimiento)] = "mantenimientos"
        };

        foreach (var (tipo, tabla) in tablas)
        {
            Assert.Equal(tabla, contexto.Model.FindEntityType(tipo)!.GetTableName());
        }
    }

    [Fact]
    public async Task ElEstadoDelPanel_SeGuardaComoTexto()
    {
        using var db = new BaseDeDatosEnMemoria();
        var (_, nodo, _) = await Datos.SembrarAsync(db);
        await db.GuardarAsync(Datos.NuevoPanel(nodo.Id, "P02", estado: EstadoSalud.FALLA_DETECTADA));

        await using var contexto = db.NuevoContexto();
        var estados = await contexto.Database
            .SqlQueryRaw<string>("SELECT estado AS Value FROM paneles WHERE codigo_panel = 'P02'")
            .ToListAsync();

        Assert.Equal("FALLA_DETECTADA", Assert.Single(estados));
    }

    [Fact]
    public async Task ElTipoYElEstadoDeLaTarea_SeGuardanComoTexto()
    {
        using var db = new BaseDeDatosEnMemoria();
        var (_, _, panel) = await Datos.SembrarAsync(db);
        await db.GuardarAsync(Datos.NuevaTarea(panel.Id, new DateTime(2026, 11, 15), TipoMantenimiento.REEMPLAZO));

        await using var contexto = db.NuevoContexto();
        var tipos = await contexto.Database
            .SqlQueryRaw<string>("SELECT tipo AS Value FROM mantenimientos")
            .ToListAsync();
        var estados = await contexto.Database
            .SqlQueryRaw<string>("SELECT estado AS Value FROM mantenimientos")
            .ToListAsync();

        Assert.Equal("REEMPLAZO", Assert.Single(tipos));
        Assert.Equal("PENDIENTE", Assert.Single(estados));
    }

    [Fact]
    public async Task DosInversoresConElMismoNumeroDeSerie_NoSePuedenGuardar()
    {
        using var db = new BaseDeDatosEnMemoria();
        var parque = Datos.NuevoParque();
        await db.GuardarAsync(parque, Datos.NuevoNodo(parque.Id, "Uno", "SN-REPETIDO"));

        await Assert.ThrowsAsync<DbUpdateException>(
            () => db.GuardarAsync(Datos.NuevoNodo(parque.Id, "Dos", "SN-REPETIDO")));
    }

    [Fact]
    public async Task DosPanelesConElMismoCodigoEnUnNodo_NoSePuedenGuardar()
    {
        using var db = new BaseDeDatosEnMemoria();
        var (_, nodo, _) = await Datos.SembrarAsync(db);

        await Assert.ThrowsAsync<DbUpdateException>(() => db.GuardarAsync(Datos.NuevoPanel(nodo.Id, "P01")));
    }

    [Fact]
    public async Task ElMismoCodigoDePanelEnOtroNodo_SiSePuedeGuardar()
    {
        using var db = new BaseDeDatosEnMemoria();
        var (parque, _, _) = await Datos.SembrarAsync(db);
        var otroNodo = Datos.NuevoNodo(parque.Id, "Otro", "SN-OTRO");

        await db.GuardarAsync(otroNodo, Datos.NuevoPanel(otroNodo.Id, "P01"));

        await using var contexto = db.NuevoContexto();
        Assert.Equal(2, contexto.Paneles.Count(panel => panel.CodigoPanel == "P01"));
    }

    [Fact]
    public async Task UnPanelDeUnNodoInexistente_NoSePuedeGuardar()
    {
        using var db = new BaseDeDatosEnMemoria();

        await Assert.ThrowsAsync<DbUpdateException>(
            () => db.GuardarAsync(Datos.NuevoPanel(Guid.NewGuid(), "P01")));
    }

    [Fact]
    public async Task UnaLecturaDeUnPanelInexistente_NoSePuedeGuardar()
    {
        using var db = new BaseDeDatosEnMemoria();

        await Assert.ThrowsAsync<DbUpdateException>(
            () => db.GuardarAsync(Datos.NuevaLectura(Guid.NewGuid(), new DateTime(2026, 11, 10))));
    }

    [Fact]
    public async Task ElSensorDeUnNodo_SeRecuperaConSuInversor()
    {
        using var db = new BaseDeDatosEnMemoria();
        var parque = Datos.NuevoParque();
        var nodo = Datos.NuevoNodo(parque.Id, hardware: "ARM-001");
        await db.GuardarAsync(parque, nodo);

        await using var contexto = db.NuevoContexto();
        var sensor = await contexto.SensoresDispositivos.SingleAsync();

        Assert.Equal("ARM-001", sensor.IdentificadorHardwareARM);
        Assert.Equal(nodo.Inversor.Id, sensor.InversorId);
        Assert.Equal("http://sensor-a:5100", sensor.UrlBase);
        Assert.True(sensor.Activo);
    }

    [Fact]
    public async Task LasLecturas_ConservanElEstadoDetectado()
    {
        using var db = new BaseDeDatosEnMemoria();
        var parque = Datos.NuevoParque();
        var nodo = Datos.NuevoNodo(parque.Id);

        // El estado lo fija el dominio al evaluar las lecturas de un nodo, no el constructor.
        var apagado = Datos.NuevoPanel(nodo.Id, "P01");
        apagado.MarcarActividad(false);
        nodo.Paneles.Add(apagado);
        var lectura = Datos.NuevaLectura(apagado.Id, new DateTime(2026, 11, 10, 10, 0, 0), 0d);
        nodo.EvaluarLecturas([lectura]);

        await db.GuardarAsync(parque, nodo, lectura);

        await using var contexto = db.NuevoContexto();
        var guardada = await contexto.LecturasTelemetria.SingleAsync(l => l.Id == lectura.Id);
        Assert.Equal(EstadoSalud.DESCONECTADO, guardada.EstadoDetectado);
        Assert.Equal(EstadoSalud.DESCONECTADO, (await contexto.Paneles.SingleAsync()).Estado);
    }
}
