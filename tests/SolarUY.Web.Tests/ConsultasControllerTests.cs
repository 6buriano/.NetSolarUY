using Microsoft.AspNetCore.Mvc;
using SolarUY.Application.Consultas;
using SolarUY.Application.DTOs;
using SolarUY.Application.Services;
using SolarUY.Web.Controllers;
using SolarUY.Web.Models;

namespace SolarUY.Web.Tests;

/// <summary>
/// Pruebas de <see cref="ConsultasController"/>: qué le muestra al usuario según lo que conteste el
/// agente. Se usa el <see cref="ConsultaService"/> real con un agente falso.
/// </summary>
public class ConsultasControllerTests
{
    private static ConsultasController CrearControlador(AgenteFalso agente)
        => new(new ConsultaService(agente));

    [Fact]
    public void Index_Get_DevuelveElFormularioVacio()
    {
        var controlador = CrearControlador(new AgenteFalso());

        var resultado = controlador.Index();

        var vista = Assert.IsType<ViewResult>(resultado);
        var modelo = Assert.IsType<ConsultaViewModel>(vista.Model);
        Assert.Equal(string.Empty, modelo.Pregunta);
        Assert.Null(modelo.Respuesta);
    }

    [Fact]
    public async Task Index_Post_ConModeloInvalido_DevuelveLaVistaSinLlamarAlAgente()
    {
        var agente = new AgenteFalso();
        var controlador = CrearControlador(agente);
        controlador.ModelState.AddModelError(nameof(ConsultaViewModel.Pregunta), "Escribí una pregunta.");
        var modelo = new ConsultaViewModel { Pregunta = "texto" };

        var resultado = await controlador.Index(modelo, CancellationToken.None);

        var vista = Assert.IsType<ViewResult>(resultado);
        Assert.Same(modelo, vista.Model);
        Assert.Equal(0, agente.Llamadas);
    }

    [Fact]
    public async Task Index_Post_ConRespuestaExitosa_MuestraLaRespuestaYLimpiaLaPregunta()
    {
        var agente = new AgenteFalso(new ConsultaLenguajeNaturalResponse(true, "Hoy se generaron 120 kWh."));
        var controlador = CrearControlador(agente);
        var modelo = new ConsultaViewModel { Pregunta = "  ¿Cuánto se generó hoy?  " };

        var resultado = await controlador.Index(modelo, CancellationToken.None);

        var vista = Assert.IsType<ViewResult>(resultado);
        var devuelto = Assert.IsType<ConsultaViewModel>(vista.Model);
        Assert.True(devuelto.Exito);
        Assert.Equal("Hoy se generaron 120 kWh.", devuelto.Respuesta);
        Assert.Equal("¿Cuánto se generó hoy?", devuelto.PreguntaEnviada);
        Assert.Equal(string.Empty, devuelto.Pregunta);
        Assert.Equal("¿Cuánto se generó hoy?", agente.UltimaPregunta);
    }

    [Fact]
    public async Task Index_Post_SiElAgenteFalla_ConservaLaPreguntaParaReintentar()
    {
        var agente = new AgenteFalso(new ConsultaLenguajeNaturalResponse(false, "No pude responder ahora."));
        var controlador = CrearControlador(agente);
        var modelo = new ConsultaViewModel { Pregunta = "¿Hay paneles en falla?" };

        var resultado = await controlador.Index(modelo, CancellationToken.None);

        var vista = Assert.IsType<ViewResult>(resultado);
        var devuelto = Assert.IsType<ConsultaViewModel>(vista.Model);
        Assert.False(devuelto.Exito);
        Assert.Equal("No pude responder ahora.", devuelto.Respuesta);
        Assert.Equal("¿Hay paneles en falla?", devuelto.Pregunta);
    }

    [Fact]
    public async Task Index_Post_ConPreguntaEnBlanco_AgregaElErrorAlCampoSinLlamarAlAgente()
    {
        var agente = new AgenteFalso();
        var controlador = CrearControlador(agente);
        var modelo = new ConsultaViewModel { Pregunta = "   " };

        var resultado = await controlador.Index(modelo, CancellationToken.None);

        Assert.IsType<ViewResult>(resultado);
        Assert.True(controlador.ModelState.ContainsKey(nameof(ConsultaViewModel.Pregunta)));
        Assert.Equal(0, agente.Llamadas);
    }

    private sealed class AgenteFalso(ConsultaLenguajeNaturalResponse? respuesta = null) : IMCPAgentService
    {
        public int Llamadas { get; private set; }
        public string? UltimaPregunta { get; private set; }

        public Task<ConsultaLenguajeNaturalResponse> ConsultarAsync(string pregunta,
            CancellationToken cancellationToken = default)
        {
            Llamadas++;
            UltimaPregunta = pregunta;
            return Task.FromResult(respuesta ?? new ConsultaLenguajeNaturalResponse(true, "ok"));
        }
    }
}
