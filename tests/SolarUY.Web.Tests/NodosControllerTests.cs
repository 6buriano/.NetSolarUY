using Microsoft.AspNetCore.Mvc;
using SolarUY.Application.DTOs;
using SolarUY.Application.Sensors;
using SolarUY.Application.Services;
using SolarUY.Domain.Entities;
using SolarUY.Web.Controllers;
using SolarUY.Web.Models;

namespace SolarUY.Web.Tests;

/// <summary>
/// Pruebas de <see cref="NodosController"/>: listado, detalle, órdenes de volcado al sensor y el alta,
/// edición y baja, incluyendo los tres caminos cuando el sensor cargado no existe en el simulador.
/// </summary>
public class NodosControllerTests
{
    private const string ClaveExito = "MensajeExito";
    private const string ClaveError = "MensajeError";

    private sealed class Contexto
    {
        public required NodoRepositoryAbmFalso Nodos { get; init; }
        public required SensorClientFalso Sensor { get; init; }
        public required SensorDisponibilidadFalso Disponibilidad { get; init; }
        public required ParqueSolar Parque { get; init; }
        public required NodosController Controlador { get; init; }
    }

    private static Contexto Crear(NodoRepositoryAbmFalso? nodos = null, SensorClientFalso? sensor = null,
        SensorDisponibilidadFalso? disponibilidad = null, string? urlSimulador = null, ParqueSolar? parque = null)
    {
        var parqueActual = parque ?? FixtureWeb.NuevoParque();
        var repositorioNodos = nodos ?? new NodoRepositoryAbmFalso();
        var repositorioParques = new ParqueRepositoryAbmFalso(parqueActual);
        var cliente = sensor ?? new SensorClientFalso();
        var verificador = disponibilidad ?? new SensorDisponibilidadFalso();

        var controlador = new NodosController(
            new NodoService(repositorioNodos, repositorioParques),
            new ParqueService(repositorioParques),
            new PanelService(new PanelRepositoryAbmFalso(), repositorioNodos, new TelemetriaRepositoryDatosFalso()),
            verificador,
            cliente,
            FixtureWeb.Configuracion(urlSimulador))
        {
            TempData = FixtureWeb.NuevoTempData()
        };

        return new Contexto
        {
            Nodos = repositorioNodos,
            Sensor = cliente,
            Disponibilidad = verificador,
            Parque = parqueActual,
            Controlador = controlador
        };
    }

    private static NodoFormViewModel Formulario(Guid parqueId, string? hardware = null, Guid? id = null,
        string? accionArm = null) => new()
    {
        Id = id,
        Nombre = "Nodo Nuevo",
        UbicacionDentroParque = "Sector 9",
        ParqueSolarId = parqueId,
        InversorModelo = "Huawei SUN2000",
        InversorNumeroSerie = "SN-900",
        InversorEficienciaConversion = 98d,
        InversorVolcadoRedActivo = true,
        IdentificadorHardwareARM = hardware,
        SensorVersionFirmware = "1.2.0",
        SensorUrlBase = hardware is null ? null : "http://sensor-x:5100",
        SensorActivo = true,
        AccionArm = accionArm
    };

    // ---------------------------------------------------------------- Index y Detalle

    [Fact]
    public async Task Index_SinParque_MuestraTodosLosNodosYLosParques()
    {
        var parque = FixtureWeb.NuevoParque();
        var contexto = Crear(new NodoRepositoryAbmFalso(FixtureWeb.NuevoNodo(parque.Id),
            FixtureWeb.NuevoNodo(Guid.NewGuid(), "Otro", "SN-002")), parque: parque);

        var resultado = await contexto.Controlador.Index(null, CancellationToken.None);

        var modelo = Assert.IsType<NodosIndexViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal(2, modelo.Nodos.Count);
        Assert.Single(modelo.Parques);
        Assert.Null(modelo.ParqueSolarId);
    }

    [Fact]
    public async Task Index_ConParque_MuestraSoloLosNodosDeEseParque()
    {
        var parque = FixtureWeb.NuevoParque();
        var contexto = Crear(new NodoRepositoryAbmFalso(FixtureWeb.NuevoNodo(parque.Id),
            FixtureWeb.NuevoNodo(Guid.NewGuid(), "Otro", "SN-002")), parque: parque);

        var resultado = await contexto.Controlador.Index(parque.Id, CancellationToken.None);

        var modelo = Assert.IsType<NodosIndexViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal("Nodo A", Assert.Single(modelo.Nodos).Nombre);
        Assert.Equal(parque.Id, modelo.ParqueSolarId);
    }

    [Fact]
    public async Task Detalle_SiExiste_MuestraElNodoConSusPaneles()
    {
        var nodo = FixtureWeb.NuevoNodo();
        var contexto = Crear(new NodoRepositoryAbmFalso(nodo));

        var resultado = await contexto.Controlador.Detalle(nodo.Id, CancellationToken.None);

        var modelo = Assert.IsType<NodoDetalleViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal(nodo.Id, modelo.Nodo.Id);
        Assert.Empty(modelo.Paneles);
    }

    [Fact]
    public async Task Detalle_SiNoExiste_DevuelveNotFound()
    {
        var contexto = Crear();

        Assert.IsType<NotFoundResult>(await contexto.Controlador.Detalle(Guid.NewGuid(), CancellationToken.None));
    }

    // ---------------------------------------------------------------- Órdenes de volcado

    [Fact]
    public async Task ActivarVolcado_SiElNodoNoExiste_DevuelveNotFound()
    {
        var contexto = Crear();

        Assert.IsType<NotFoundResult>(await contexto.Controlador.ActivarVolcado(Guid.NewGuid(), CancellationToken.None));
        Assert.IsType<NotFoundResult>(
            await contexto.Controlador.DesactivarVolcado(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task ActivarVolcado_SiElNodoNoTieneSensor_AvisaYNoMandaOrden()
    {
        var nodo = FixtureWeb.NuevoNodo();
        var contexto = Crear(new NodoRepositoryAbmFalso(nodo));

        var resultado = await contexto.Controlador.ActivarVolcado(nodo.Id, CancellationToken.None);

        Assert.Equal(nameof(NodosController.Detalle), Assert.IsType<RedirectToActionResult>(resultado).ActionName);
        Assert.Contains("no tiene sensor ARM", (string)contexto.Controlador.TempData[ClaveError]!);
        Assert.Empty(contexto.Sensor.Ordenes);
    }

    [Fact]
    public async Task ActivarVolcado_MandaLaOrdenDeActivarAlSensor()
    {
        var nodo = FixtureWeb.NuevoNodo(hardware: "ARM-001");
        var contexto = Crear(new NodoRepositoryAbmFalso(nodo));

        await contexto.Controlador.ActivarVolcado(nodo.Id, CancellationToken.None);

        Assert.Equal("activar-volcado http://sensor-a:5100 ARM-001", Assert.Single(contexto.Sensor.Ordenes));
        Assert.Contains("orden de activar el volcado", (string)contexto.Controlador.TempData[ClaveExito]!);
    }

    [Fact]
    public async Task DesactivarVolcado_MandaLaOrdenDeDesactivarAlSensor()
    {
        var nodo = FixtureWeb.NuevoNodo(hardware: "ARM-001");
        var contexto = Crear(new NodoRepositoryAbmFalso(nodo));

        await contexto.Controlador.DesactivarVolcado(nodo.Id, CancellationToken.None);

        Assert.Equal("desactivar-volcado http://sensor-a:5100 ARM-001", Assert.Single(contexto.Sensor.Ordenes));
        Assert.Contains("orden de desactivar el volcado", (string)contexto.Controlador.TempData[ClaveExito]!);
    }

    [Theory]
    [InlineData(true, ResultadoOrdenSensor.YaAplicado, ClaveExito, "ya estaba activo")]
    [InlineData(false, ResultadoOrdenSensor.YaAplicado, ClaveExito, "ya estaba inactivo")]
    [InlineData(true, ResultadoOrdenSensor.NoEncontrado, ClaveError, "no reconoce este sensor")]
    [InlineData(false, ResultadoOrdenSensor.SinRespuesta, ClaveError, "El sensor no respondió")]
    public async Task OrdenDeVolcado_DejaElAvisoSegunLaRespuestaDelSensor(bool activar,
        ResultadoOrdenSensor respuesta, string clave, string textoEsperado)
    {
        var nodo = FixtureWeb.NuevoNodo(hardware: "ARM-001");
        var contexto = Crear(new NodoRepositoryAbmFalso(nodo), new SensorClientFalso(respuesta));

        var resultado = activar
            ? await contexto.Controlador.ActivarVolcado(nodo.Id, CancellationToken.None)
            : await contexto.Controlador.DesactivarVolcado(nodo.Id, CancellationToken.None);

        Assert.Equal(nameof(NodosController.Detalle), Assert.IsType<RedirectToActionResult>(resultado).ActionName);
        Assert.Contains(textoEsperado, (string)contexto.Controlador.TempData[clave]!);
    }

    // ---------------------------------------------------------------- Crear (GET)

    [Fact]
    public async Task Crear_Get_PrecargaElParqueYLaDireccionDelSimulador()
    {
        var contexto = Crear(urlSimulador: "http://simulador:5100");

        var resultado = await contexto.Controlador.Crear(contexto.Parque.Id, CancellationToken.None);

        var modelo = Assert.IsType<NodoFormViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal(contexto.Parque.Id, modelo.ParqueSolarId);
        Assert.Equal("http://simulador:5100", modelo.SensorUrlBase);
        Assert.Single(modelo.Parques);
    }

    [Fact]
    public async Task Crear_Get_SinSimuladorConfigurado_DejaLaDireccionVacia()
    {
        var contexto = Crear();

        var resultado = await contexto.Controlador.Crear((Guid?)null, CancellationToken.None);

        var modelo = Assert.IsType<NodoFormViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal(Guid.Empty, modelo.ParqueSolarId);
        Assert.Equal(string.Empty, modelo.SensorUrlBase);
    }

    // ---------------------------------------------------------------- Crear (POST)

    [Fact]
    public async Task Crear_Post_ConModeloInvalido_VuelveALaVistaSinGuardar()
    {
        var contexto = Crear();
        contexto.Controlador.ModelState.AddModelError("Nombre", "obligatorio");
        var formulario = Formulario(contexto.Parque.Id);

        var resultado = await contexto.Controlador.Crear(formulario, CancellationToken.None);

        Assert.Same(formulario, Assert.IsType<ViewResult>(resultado).Model);
        Assert.Single(formulario.Parques);
        Assert.Empty(contexto.Nodos.Agregados);
    }

    [Fact]
    public async Task Crear_Post_SinSensor_GuardaElNodoSinConsultarAlSimulador()
    {
        var contexto = Crear();

        var resultado = await contexto.Controlador.Crear(Formulario(contexto.Parque.Id), CancellationToken.None);

        var nodo = Assert.Single(contexto.Nodos.Agregados);
        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal(nameof(NodosController.Detalle), redireccion.ActionName);
        Assert.Equal(nodo.Id, redireccion.RouteValues!["id"]);
        Assert.Null(nodo.Inversor.Sensor);
        Assert.Empty(contexto.Disponibilidad.Verificados);
        Assert.Equal("El nodo 'Nodo Nuevo' se creó correctamente.", contexto.Controlador.TempData[ClaveExito]);
    }

    [Fact]
    public async Task Crear_Post_ConSensorQueExiste_GuardaElNodoConSuSensor()
    {
        var contexto = Crear();

        await contexto.Controlador.Crear(Formulario(contexto.Parque.Id, "ARM-009"), CancellationToken.None);

        var nodo = Assert.Single(contexto.Nodos.Agregados);
        Assert.Equal("ARM-009", nodo.Inversor.Sensor!.IdentificadorHardwareARM);
        Assert.Equal("ARM-009", Assert.Single(contexto.Disponibilidad.Verificados));
        Assert.Equal("El nodo 'Nodo Nuevo' se creó correctamente.", contexto.Controlador.TempData[ClaveExito]);
    }

    [Fact]
    public async Task Crear_Post_SiNoSePudoConsultarElSimulador_CreaElNodoYAgregaUnaNota()
    {
        var contexto = Crear(disponibilidad: new SensorDisponibilidadFalso
        {
            Disponibilidad = new DisponibilidadSensor(true, false, false, [])
        });

        await contexto.Controlador.Crear(Formulario(contexto.Parque.Id, "ARM-009"), CancellationToken.None);

        Assert.Single(contexto.Nodos.Agregados);
        Assert.Contains("No se pudo consultar el simulador", (string)contexto.Controlador.TempData[ClaveExito]!);
    }

    [Fact]
    public async Task Crear_Post_SiNoHayVerificacionPosible_CreaElNodoSinNota()
    {
        var contexto = Crear(disponibilidad: new SensorDisponibilidadFalso
        {
            Disponibilidad = new DisponibilidadSensor(false, false, false, [])
        });

        await contexto.Controlador.Crear(Formulario(contexto.Parque.Id, "ARM-009"), CancellationToken.None);

        Assert.Single(contexto.Nodos.Agregados);
        Assert.Equal("El nodo 'Nodo Nuevo' se creó correctamente.", contexto.Controlador.TempData[ClaveExito]);
    }

    [Fact]
    public async Task Crear_Post_SiElSensorNoExisteYNoHayEleccion_MuestraElAvisoConLosExistentes()
    {
        var contexto = Crear(disponibilidad: new SensorDisponibilidadFalso
        {
            Disponibilidad = new DisponibilidadSensor(true, true, false, ["ARM-001", "ARM-002"])
        });
        var formulario = Formulario(contexto.Parque.Id, " ARM-009 ");

        var resultado = await contexto.Controlador.Crear(formulario, CancellationToken.None);

        Assert.Same(formulario, Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal("El sensor 'ARM-009' no existe en el simulador. Los que existen: ARM-001, ARM-002.",
            formulario.AvisoSensorInexistente);
        Assert.Empty(contexto.Nodos.Agregados);
        Assert.Empty(contexto.Disponibilidad.Creados);
    }

    [Fact]
    public async Task Crear_Post_SiElSensorNoExisteYElSimuladorEstaVacio_DiceQueNoHayNinguno()
    {
        var contexto = Crear(disponibilidad: new SensorDisponibilidadFalso
        {
            Disponibilidad = new DisponibilidadSensor(true, true, false, [])
        });
        var formulario = Formulario(contexto.Parque.Id, "ARM-009");

        await contexto.Controlador.Crear(formulario, CancellationToken.None);

        Assert.Equal("El sensor 'ARM-009' no existe en el simulador. Los que existen: ninguno.",
            formulario.AvisoSensorInexistente);
    }

    [Fact]
    public async Task Crear_Post_SiEligeCrearEnHardware_DaDeAltaElSensorYCreaElNodoConLaNota()
    {
        var contexto = Crear(disponibilidad: new SensorDisponibilidadFalso
        {
            Disponibilidad = new DisponibilidadSensor(true, true, false, []),
            Alta = new ResultadoAltaSensor(true, null, ["P01", "P02"])
        });

        var resultado = await contexto.Controlador.Crear(
            Formulario(contexto.Parque.Id, "ARM-009", accionArm: NodoFormViewModel.AccionesArm.CrearEnHardware),
            CancellationToken.None);

        Assert.Equal(nameof(NodosController.Detalle), Assert.IsType<RedirectToActionResult>(resultado).ActionName);
        Assert.Equal("ARM-009", Assert.Single(contexto.Disponibilidad.Creados));
        Assert.Equal("ARM-009", Assert.Single(contexto.Nodos.Agregados).Inversor.Sensor!.IdentificadorHardwareARM);
        var aviso = (string)contexto.Controlador.TempData[ClaveExito]!;
        Assert.Contains("empezó a emular el sensor 'ARM-009'", aviso);
        Assert.Contains("P01, P02", aviso);
    }

    [Fact]
    public async Task Crear_Post_SiElAltaNoInformaPaneles_NoMencionaPaneles()
    {
        var contexto = Crear(disponibilidad: new SensorDisponibilidadFalso
        {
            Disponibilidad = new DisponibilidadSensor(true, true, false, [])
        });

        await contexto.Controlador.Crear(
            Formulario(contexto.Parque.Id, "ARM-009", accionArm: NodoFormViewModel.AccionesArm.CrearEnHardware),
            CancellationToken.None);

        Assert.DoesNotContain("paneles", (string)contexto.Controlador.TempData[ClaveExito]!);
    }

    [Fact]
    public async Task Crear_Post_SiFallaElAltaEnElHardware_MuestraElErrorYNoCreaNada()
    {
        var contexto = Crear(disponibilidad: new SensorDisponibilidadFalso
        {
            Disponibilidad = new DisponibilidadSensor(true, true, false, []),
            Alta = new ResultadoAltaSensor(false, "El simulador rechazó el alta.", [])
        });
        var formulario = Formulario(contexto.Parque.Id, "ARM-009",
            accionArm: NodoFormViewModel.AccionesArm.CrearEnHardware);

        var resultado = await contexto.Controlador.Crear(formulario, CancellationToken.None);

        Assert.Same(formulario, Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal("El simulador rechazó el alta.",
            contexto.Controlador.ModelState[string.Empty]!.Errors[0].ErrorMessage);
        Assert.Empty(contexto.Nodos.Agregados);
    }

    [Fact]
    public async Task Crear_Post_SiFallaElAltaSinMensaje_UsaElMensajeGenerico()
    {
        var contexto = Crear(disponibilidad: new SensorDisponibilidadFalso
        {
            Disponibilidad = new DisponibilidadSensor(true, true, false, []),
            Alta = new ResultadoAltaSensor(false, null, [])
        });

        await contexto.Controlador.Crear(
            Formulario(contexto.Parque.Id, "ARM-009", accionArm: NodoFormViewModel.AccionesArm.CrearEnHardware),
            CancellationToken.None);

        Assert.Equal("No se pudo crear el sensor.",
            contexto.Controlador.ModelState[string.Empty]!.Errors[0].ErrorMessage);
    }

    [Fact]
    public async Task Crear_Post_SiEligeSinSensor_CreaElNodoSinSensorYLoAvisa()
    {
        var contexto = Crear(disponibilidad: new SensorDisponibilidadFalso
        {
            Disponibilidad = new DisponibilidadSensor(true, true, false, [])
        });

        await contexto.Controlador.Crear(
            Formulario(contexto.Parque.Id, "ARM-009", accionArm: NodoFormViewModel.AccionesArm.SinSensor),
            CancellationToken.None);

        Assert.Null(Assert.Single(contexto.Nodos.Agregados).Inversor.Sensor);
        Assert.Contains("se creó sin sensor ARM: 'ARM-009' no existe", (string)contexto.Controlador.TempData[ClaveExito]!);
    }

    [Fact]
    public async Task Crear_Post_SiElParqueNoExiste_MuestraElErrorDelDominio()
    {
        var contexto = Crear();
        var formulario = Formulario(Guid.NewGuid());

        var resultado = await contexto.Controlador.Crear(formulario, CancellationToken.None);

        Assert.Same(formulario, Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal("El parque solar seleccionado no existe.",
            contexto.Controlador.ModelState[string.Empty]!.Errors[0].ErrorMessage);
        Assert.Empty(contexto.Nodos.Agregados);
    }

    // ---------------------------------------------------------------- Editar

    [Fact]
    public async Task Editar_Get_CargaElFormularioConElSensorDelNodo()
    {
        var nodo = FixtureWeb.NuevoNodo(hardware: "ARM-001");
        var contexto = Crear(new NodoRepositoryAbmFalso(nodo));

        var resultado = await contexto.Controlador.Editar(nodo.Id, CancellationToken.None);

        var modelo = Assert.IsType<NodoFormViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal(nodo.Id, modelo.Id);
        Assert.Equal("Nodo A", modelo.Nombre);
        Assert.Equal("SN-001", modelo.InversorNumeroSerie);
        Assert.Equal("ARM-001", modelo.IdentificadorHardwareARM);
        Assert.Equal("http://sensor-a:5100", modelo.SensorUrlBase);
        Assert.True(modelo.SensorActivo);
        Assert.Single(modelo.Parques);
    }

    [Fact]
    public async Task Editar_Get_SinSensor_UsaLosValoresPorDefecto()
    {
        var nodo = FixtureWeb.NuevoNodo();
        var contexto = Crear(new NodoRepositoryAbmFalso(nodo), urlSimulador: "http://simulador:5100");

        var resultado = await contexto.Controlador.Editar(nodo.Id, CancellationToken.None);

        var modelo = Assert.IsType<NodoFormViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal(string.Empty, modelo.IdentificadorHardwareARM);
        Assert.Equal(string.Empty, modelo.SensorVersionFirmware);
        Assert.Equal("http://simulador:5100", modelo.SensorUrlBase);
        Assert.True(modelo.SensorActivo);
    }

    [Fact]
    public async Task Editar_Get_SiNoExiste_DevuelveNotFound()
    {
        var contexto = Crear();

        Assert.IsType<NotFoundResult>(await contexto.Controlador.Editar(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task Editar_Post_SiElIdNoCoincide_DevuelveNotFound()
    {
        var nodo = FixtureWeb.NuevoNodo();
        var contexto = Crear(new NodoRepositoryAbmFalso(nodo));

        var resultado = await contexto.Controlador.Editar(nodo.Id, Formulario(contexto.Parque.Id, id: Guid.NewGuid()),
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(resultado);
    }

    [Fact]
    public async Task Editar_Post_ConModeloInvalido_VuelveALaVistaSinGuardar()
    {
        var nodo = FixtureWeb.NuevoNodo();
        var contexto = Crear(new NodoRepositoryAbmFalso(nodo));
        contexto.Controlador.ModelState.AddModelError("Nombre", "obligatorio");
        var formulario = Formulario(contexto.Parque.Id, id: nodo.Id);

        var resultado = await contexto.Controlador.Editar(nodo.Id, formulario, CancellationToken.None);

        Assert.Same(formulario, Assert.IsType<ViewResult>(resultado).Model);
        Assert.Single(formulario.Parques);
        Assert.Empty(contexto.Nodos.Actualizados);
    }

    [Fact]
    public async Task Editar_Post_ConDatosValidos_ActualizaElNodoYVuelveAlDetalle()
    {
        var nodo = FixtureWeb.NuevoNodo();
        var contexto = Crear(new NodoRepositoryAbmFalso(nodo));

        var resultado = await contexto.Controlador.Editar(nodo.Id,
            Formulario(contexto.Parque.Id, "ARM-009", nodo.Id), CancellationToken.None);

        Assert.Equal(nameof(NodosController.Detalle), Assert.IsType<RedirectToActionResult>(resultado).ActionName);
        Assert.Equal("Nodo Nuevo", nodo.Nombre);
        Assert.Equal("ARM-009", nodo.Inversor.Sensor!.IdentificadorHardwareARM);
        Assert.Single(contexto.Nodos.Actualizados);
        Assert.Equal("El nodo 'Nodo Nuevo' se actualizó correctamente.", contexto.Controlador.TempData[ClaveExito]);
    }

    [Fact]
    public async Task Editar_Post_SiElNodoYaNoExiste_DevuelveNotFound()
    {
        var contexto = Crear();
        var id = Guid.NewGuid();

        var resultado = await contexto.Controlador.Editar(id, Formulario(contexto.Parque.Id, id: id),
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(resultado);
    }

    [Fact]
    public async Task Editar_Post_SiElNumeroDeSerieYaExiste_MuestraElErrorDelDominio()
    {
        var nodo = FixtureWeb.NuevoNodo();
        var contexto = Crear(new NodoRepositoryAbmFalso(nodo) { NumeroSerieOcupado = true });
        var formulario = Formulario(contexto.Parque.Id, id: nodo.Id);

        var resultado = await contexto.Controlador.Editar(nodo.Id, formulario, CancellationToken.None);

        Assert.Same(formulario, Assert.IsType<ViewResult>(resultado).Model);
        Assert.Contains("'SN-900'", contexto.Controlador.ModelState[string.Empty]!.Errors[0].ErrorMessage);
        Assert.Empty(contexto.Nodos.Actualizados);
    }

    // ---------------------------------------------------------------- Eliminar

    [Fact]
    public async Task Eliminar_Get_PideConfirmacionConElNodo()
    {
        var nodo = FixtureWeb.NuevoNodo();
        var contexto = Crear(new NodoRepositoryAbmFalso(nodo));

        var resultado = await contexto.Controlador.Eliminar(nodo.Id, CancellationToken.None);

        Assert.Equal(nodo.Id, Assert.IsType<NodoDto>(Assert.IsType<ViewResult>(resultado).Model).Id);
    }

    [Fact]
    public async Task Eliminar_Get_SiNoExiste_DevuelveNotFound()
    {
        var contexto = Crear();

        Assert.IsType<NotFoundResult>(await contexto.Controlador.Eliminar(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task EliminarConfirmado_SinPaneles_LoEliminaYVuelveAlListado()
    {
        var nodo = FixtureWeb.NuevoNodo();
        var contexto = Crear(new NodoRepositoryAbmFalso(nodo));

        var resultado = await contexto.Controlador.EliminarConfirmado(nodo.Id, CancellationToken.None);

        Assert.Equal(nameof(NodosController.Index), Assert.IsType<RedirectToActionResult>(resultado).ActionName);
        Assert.Same(nodo, Assert.Single(contexto.Nodos.Eliminados));
        Assert.Equal("El nodo se eliminó correctamente.", contexto.Controlador.TempData[ClaveExito]);
    }

    [Fact]
    public async Task EliminarConfirmado_SiNoExiste_DevuelveNotFound()
    {
        var contexto = Crear();

        Assert.IsType<NotFoundResult>(
            await contexto.Controlador.EliminarConfirmado(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task EliminarConfirmado_ConPaneles_AvisaYNoLoElimina()
    {
        var nodo = FixtureWeb.NuevoNodo();
        nodo.Paneles.Add(FixtureWeb.NuevoPanel(nodo.Id));
        var contexto = Crear(new NodoRepositoryAbmFalso(nodo));

        var resultado = await contexto.Controlador.EliminarConfirmado(nodo.Id, CancellationToken.None);

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal(nameof(NodosController.Detalle), redireccion.ActionName);
        Assert.Equal(nodo.Id, redireccion.RouteValues!["id"]);
        Assert.Contains("todavía tiene paneles", (string)contexto.Controlador.TempData[ClaveError]!);
        Assert.Empty(contexto.Nodos.Eliminados);
    }

    // ---------------------------------------------------------------- Validación del formulario

    [Fact]
    public void Validate_SinIdentificador_NoPideDireccion()
    {
        var formulario = Formulario(Guid.NewGuid());

        Assert.Empty(formulario.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(formulario)));
    }

    [Fact]
    public void Validate_ConIdentificadorSinDireccion_PideLaDireccion()
    {
        var formulario = Formulario(Guid.NewGuid(), "ARM-001");
        formulario.SensorUrlBase = "  ";

        var errores = formulario.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(formulario))
            .ToList();

        var error = Assert.Single(errores);
        Assert.Contains("la dirección es obligatoria", error.ErrorMessage);
        Assert.Equal(nameof(NodoFormViewModel.SensorUrlBase), Assert.Single(error.MemberNames));
    }

    [Fact]
    public void Validate_ConIdentificadorYDireccion_EsValido()
    {
        var formulario = Formulario(Guid.NewGuid(), "ARM-001");

        Assert.Empty(formulario.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(formulario)));
    }
}
