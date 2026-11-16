using SolarUY.Application.Consultas;
using SolarUY.Application.DTOs;
using SolarUY.Application.Services;
using SolarUY.Domain.Exceptions;

namespace SolarUY.Application.Tests.Services;

/// <summary>
/// Pruebas de <see cref="ConsultaService"/>: valida y recorta la pregunta antes de pasársela al agente
/// y devuelve su respuesta tal cual. El agente es falso: no hay red ni proveedor de IA.
/// </summary>
public class ConsultaServiceTests
{
    [Fact]
    public async Task ConsultarAsync_RecortaLaPreguntaYDevuelveLaRespuestaDelAgente()
    {
        var agente = new AgenteFalso(new ConsultaLenguajeNaturalResponse(true, "Hay 3 paneles en falla."));
        var servicio = new ConsultaService(agente);

        var respuesta = await servicio.ConsultarAsync(new ConsultaLenguajeNaturalRequest("  ¿Cuántos paneles fallan?  "));

        Assert.True(respuesta.Exito);
        Assert.Equal("Hay 3 paneles en falla.", respuesta.Texto);
        Assert.Equal("¿Cuántos paneles fallan?", agente.UltimaPregunta);
        Assert.Equal(1, agente.Llamadas);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public async Task ConsultarAsync_ConPreguntaEnBlanco_LanzaExcepcionYNoLlamaAlAgente(string pregunta)
    {
        var agente = new AgenteFalso();
        var servicio = new ConsultaService(agente);

        var error = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => servicio.ConsultarAsync(new ConsultaLenguajeNaturalRequest(pregunta)));

        Assert.Equal("Escribí una pregunta: no se puede consultar en blanco.", error.Message);
        Assert.Equal(0, agente.Llamadas);
    }

    [Fact]
    public async Task ConsultarAsync_ConPreguntaNula_SeTrataComoVacia()
    {
        var agente = new AgenteFalso();
        var servicio = new ConsultaService(agente);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => servicio.ConsultarAsync(new ConsultaLenguajeNaturalRequest(null!)));

        Assert.Equal(0, agente.Llamadas);
    }

    [Fact]
    public async Task ConsultarAsync_ConPreguntaDeLargoMaximo_SeAceptaYConUnCaracterMasSeRechaza()
    {
        var agente = new AgenteFalso();
        var servicio = new ConsultaService(agente);

        await servicio.ConsultarAsync(new ConsultaLenguajeNaturalRequest(new string('a', ConsultaService.LargoMaximoPregunta)));
        Assert.Equal(1, agente.Llamadas);

        var error = await Assert.ThrowsAsync<ReglaDeNegocioException>(() => servicio.ConsultarAsync(
            new ConsultaLenguajeNaturalRequest(new string('a', ConsultaService.LargoMaximoPregunta + 1))));

        Assert.Contains($"{ConsultaService.LargoMaximoPregunta}", error.Message);
        Assert.Equal(1, agente.Llamadas);
    }

    [Fact]
    public async Task ConsultarAsync_CuentaElLargoDespuesDeRecortar()
    {
        var agente = new AgenteFalso();
        var servicio = new ConsultaService(agente);
        var pregunta = "  " + new string('a', ConsultaService.LargoMaximoPregunta) + "  ";

        await servicio.ConsultarAsync(new ConsultaLenguajeNaturalRequest(pregunta));

        Assert.Equal(1, agente.Llamadas);
    }

    [Fact]
    public async Task ConsultarAsync_SiElAgenteFalla_DevuelveSuMensajeSinLanzar()
    {
        var agente = new AgenteFalso(new ConsultaLenguajeNaturalResponse(false, "No se pudo responder ahora."));
        var servicio = new ConsultaService(agente);

        var respuesta = await servicio.ConsultarAsync(new ConsultaLenguajeNaturalRequest("¿Cómo está el parque?"));

        Assert.False(respuesta.Exito);
        Assert.Equal("No se pudo responder ahora.", respuesta.Texto);
    }

    private sealed class AgenteFalso(ConsultaLenguajeNaturalResponse? respuesta = null) : IMcpAgentService
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
