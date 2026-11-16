using Microsoft.AspNetCore.Mvc;
using SolarUY.Application.DTOs;
using SolarUY.Application.Services;
using SolarUY.Domain.Entities;
using SolarUY.Web.Controllers;
using SolarUY.Web.Models;

namespace SolarUY.Web.Tests;

/// <summary>
/// Pruebas de <see cref="ParquesController"/>: a qué pantalla va cada acción y qué aviso deja en TempData.
/// Se usan los servicios reales con repositorios falsos, así que también se ejercitan las reglas del dominio.
/// </summary>
public class ParquesControllerTests
{
    private const string ClaveExito = "MensajeExito";
    private const string ClaveError = "MensajeError";

    private static ParquesController CrearControlador(ParqueRepositoryAbmFalso parques, params Nodo[] nodos)
    {
        var nodoService = new NodoService(new NodoRepositoryAbmFalso(nodos), parques);

        return new ParquesController(new ParqueService(parques), nodoService)
        {
            TempData = FixtureWeb.NuevoTempData()
        };
    }

    private static ParqueFormViewModel FormularioValido(Guid? id = null) => new()
    {
        Id = id,
        Nombre = "Parque Este",
        Ubicacion = "Rivera",
        CapacidadInstaladaKW = 20d,
        FechaInstalacion = new DateTime(2026, 3, 10)
    };

    // ---------------------------------------------------------------- Index y Detalle

    [Fact]
    public async Task Index_DevuelveLaListaDeParques()
    {
        var controlador = CrearControlador(new ParqueRepositoryAbmFalso(FixtureWeb.NuevoParque(),
            FixtureWeb.NuevoParque("Parque Sur")));

        var resultado = await controlador.Index(CancellationToken.None);

        var modelo = Assert.IsAssignableFrom<IReadOnlyList<ParqueDto>>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal(2, modelo.Count);
    }

    [Fact]
    public async Task Detalle_SiExiste_MuestraElParqueConSusNodos()
    {
        var parque = FixtureWeb.NuevoParque();
        var nodo = FixtureWeb.NuevoNodo(parque.Id, "Nodo Central");
        var controlador = CrearControlador(new ParqueRepositoryAbmFalso(parque), nodo);

        var resultado = await controlador.Detalle(parque.Id, CancellationToken.None);

        var modelo = Assert.IsType<ParqueDetalleViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal(parque.Id, modelo.Parque.Id);
        Assert.Equal("Nodo Central", Assert.Single(modelo.Nodos).Nombre);
    }

    [Fact]
    public async Task Detalle_SiNoExiste_DevuelveNotFound()
    {
        var controlador = CrearControlador(new ParqueRepositoryAbmFalso());

        Assert.IsType<NotFoundResult>(await controlador.Detalle(Guid.NewGuid(), CancellationToken.None));
    }

    // ---------------------------------------------------------------- Crear

    [Fact]
    public void Crear_Get_MuestraUnFormularioVacio()
    {
        var controlador = CrearControlador(new ParqueRepositoryAbmFalso());

        var resultado = controlador.Crear();

        var modelo = Assert.IsType<ParqueFormViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Null(modelo.Id);
        Assert.Equal(string.Empty, modelo.Nombre);
    }

    [Fact]
    public async Task Crear_Post_ConModeloInvalido_VuelveALaVistaSinGuardar()
    {
        var parques = new ParqueRepositoryAbmFalso();
        var controlador = CrearControlador(parques);
        controlador.ModelState.AddModelError("Nombre", "obligatorio");
        var formulario = FormularioValido();

        var resultado = await controlador.Crear(formulario, CancellationToken.None);

        Assert.Same(formulario, Assert.IsType<ViewResult>(resultado).Model);
        Assert.Empty(parques.Agregados);
    }

    [Fact]
    public async Task Crear_Post_ConDatosValidos_GuardaYVuelveAlListado()
    {
        var parques = new ParqueRepositoryAbmFalso();
        var controlador = CrearControlador(parques);

        var resultado = await controlador.Crear(FormularioValido(), CancellationToken.None);

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal(nameof(ParquesController.Index), redireccion.ActionName);
        Assert.Equal("Parque Este", Assert.Single(parques.Agregados).Nombre);
        Assert.Equal("El parque 'Parque Este' se creó correctamente.", controlador.TempData[ClaveExito]);
    }

    [Fact]
    public async Task Crear_Post_SiElDominioRechazaLosDatos_MuestraElErrorEnElFormulario()
    {
        var parques = new ParqueRepositoryAbmFalso();
        var controlador = CrearControlador(parques);
        var formulario = FormularioValido();
        formulario.CapacidadInstaladaKW = 0d;

        var resultado = await controlador.Crear(formulario, CancellationToken.None);

        Assert.Same(formulario, Assert.IsType<ViewResult>(resultado).Model);
        Assert.False(controlador.ModelState.IsValid);
        Assert.Contains("capacidad", controlador.ModelState[string.Empty]!.Errors[0].ErrorMessage);
        Assert.Empty(parques.Agregados);
    }

    // ---------------------------------------------------------------- Editar

    [Fact]
    public async Task Editar_Get_CargaElFormularioConLosDatosDelParque()
    {
        var parque = FixtureWeb.NuevoParque();
        var controlador = CrearControlador(new ParqueRepositoryAbmFalso(parque));

        var resultado = await controlador.Editar(parque.Id, CancellationToken.None);

        var modelo = Assert.IsType<ParqueFormViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal(parque.Id, modelo.Id);
        Assert.Equal("Parque Norte", modelo.Nombre);
        Assert.Equal("Salto", modelo.Ubicacion);
        Assert.Equal(12.5d, modelo.CapacidadInstaladaKW, 6);
    }

    [Fact]
    public async Task Editar_Get_SiNoExiste_DevuelveNotFound()
    {
        var controlador = CrearControlador(new ParqueRepositoryAbmFalso());

        Assert.IsType<NotFoundResult>(await controlador.Editar(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task Editar_Post_SiElIdDeLaRutaNoCoincideConElDelFormulario_DevuelveNotFound()
    {
        var parque = FixtureWeb.NuevoParque();
        var controlador = CrearControlador(new ParqueRepositoryAbmFalso(parque));

        var resultado = await controlador.Editar(parque.Id, FormularioValido(Guid.NewGuid()), CancellationToken.None);

        Assert.IsType<NotFoundResult>(resultado);
    }

    [Fact]
    public async Task Editar_Post_ConModeloInvalido_VuelveALaVistaSinGuardar()
    {
        var parque = FixtureWeb.NuevoParque();
        var parques = new ParqueRepositoryAbmFalso(parque);
        var controlador = CrearControlador(parques);
        controlador.ModelState.AddModelError("Nombre", "obligatorio");
        var formulario = FormularioValido(parque.Id);

        var resultado = await controlador.Editar(parque.Id, formulario, CancellationToken.None);

        Assert.Same(formulario, Assert.IsType<ViewResult>(resultado).Model);
        Assert.Empty(parques.Actualizados);
    }

    [Fact]
    public async Task Editar_Post_ConDatosValidos_ActualizaYVuelveAlListado()
    {
        var parque = FixtureWeb.NuevoParque();
        var parques = new ParqueRepositoryAbmFalso(parque);
        var controlador = CrearControlador(parques);

        var resultado = await controlador.Editar(parque.Id, FormularioValido(parque.Id), CancellationToken.None);

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal(nameof(ParquesController.Index), redireccion.ActionName);
        Assert.Equal("Parque Este", parque.Nombre);
        Assert.Single(parques.Actualizados);
        Assert.Equal("El parque 'Parque Este' se actualizó correctamente.", controlador.TempData[ClaveExito]);
    }

    [Fact]
    public async Task Editar_Post_SiElParqueYaNoExiste_DevuelveNotFound()
    {
        var controlador = CrearControlador(new ParqueRepositoryAbmFalso());
        var id = Guid.NewGuid();

        var resultado = await controlador.Editar(id, FormularioValido(id), CancellationToken.None);

        Assert.IsType<NotFoundResult>(resultado);
    }

    [Fact]
    public async Task Editar_Post_SiElDominioRechazaLosDatos_MuestraElErrorEnElFormulario()
    {
        var parque = FixtureWeb.NuevoParque();
        var controlador = CrearControlador(new ParqueRepositoryAbmFalso(parque));
        var formulario = FormularioValido(parque.Id);
        formulario.Nombre = "   ";

        var resultado = await controlador.Editar(parque.Id, formulario, CancellationToken.None);

        Assert.Same(formulario, Assert.IsType<ViewResult>(resultado).Model);
        Assert.False(controlador.ModelState.IsValid);
    }

    // ---------------------------------------------------------------- Eliminar

    [Fact]
    public async Task Eliminar_Get_PideConfirmacionConElParque()
    {
        var parque = FixtureWeb.NuevoParque();
        var controlador = CrearControlador(new ParqueRepositoryAbmFalso(parque));

        var resultado = await controlador.Eliminar(parque.Id, CancellationToken.None);

        Assert.Equal(parque.Id, Assert.IsType<ParqueDto>(Assert.IsType<ViewResult>(resultado).Model).Id);
    }

    [Fact]
    public async Task Eliminar_Get_SiNoExiste_DevuelveNotFound()
    {
        var controlador = CrearControlador(new ParqueRepositoryAbmFalso());

        Assert.IsType<NotFoundResult>(await controlador.Eliminar(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task EliminarConfirmado_SiNoTieneNodos_LoEliminaYVuelveAlListado()
    {
        var parque = FixtureWeb.NuevoParque();
        var parques = new ParqueRepositoryAbmFalso(parque);
        var controlador = CrearControlador(parques);

        var resultado = await controlador.EliminarConfirmado(parque.Id, CancellationToken.None);

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal(nameof(ParquesController.Index), redireccion.ActionName);
        Assert.Same(parque, Assert.Single(parques.Eliminados));
        Assert.Equal("El parque se eliminó correctamente.", controlador.TempData[ClaveExito]);
    }

    [Fact]
    public async Task EliminarConfirmado_SiNoExiste_DevuelveNotFound()
    {
        var controlador = CrearControlador(new ParqueRepositoryAbmFalso());

        Assert.IsType<NotFoundResult>(await controlador.EliminarConfirmado(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task EliminarConfirmado_SiTieneNodos_AvisaYVuelveAlDetalleSinEliminar()
    {
        var parque = FixtureWeb.NuevoParque();
        parque.Nodos.Add(FixtureWeb.NuevoNodo(parque.Id));
        var parques = new ParqueRepositoryAbmFalso(parque);
        var controlador = CrearControlador(parques);

        var resultado = await controlador.EliminarConfirmado(parque.Id, CancellationToken.None);

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal(nameof(ParquesController.Detalle), redireccion.ActionName);
        Assert.Equal(parque.Id, redireccion.RouteValues!["id"]);
        Assert.Contains("nodos asociados", (string)controlador.TempData[ClaveError]!);
        Assert.Empty(parques.Eliminados);
    }
}
