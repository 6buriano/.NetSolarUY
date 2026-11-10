using Microsoft.Extensions.Logging;
using SolarUY.Application.Consultas;
using SolarUY.Application.DTOs;

namespace SolarUY.Infrastructure.Consultas;

/// <summary>
/// Implementación que se registra cuando falta configuración del agente. No lanza al arrancar: avisa
/// en el log qué claves faltan (nunca sus valores) y devuelve el mismo mensaje de falla que un
/// proveedor caído, para que la web arranque y el usuario reciba un texto entendible.
/// </summary>
public sealed class AgenteNoConfigurado : IMCPAgentService
{
    private readonly ILogger<AgenteNoConfigurado> _logger;
    private readonly string _faltantes;

    public AgenteNoConfigurado(ILogger<AgenteNoConfigurado> logger, string faltantes)
    {
        _logger = logger;
        _faltantes = faltantes;

        _logger.LogWarning(
            "El agente de consultas no está configurado: falta {Faltantes}. Las consultas en lenguaje " +
            "natural van a devolver el mensaje de falla hasta que se complete la configuración.",
            _faltantes);
    }

    public Task<ConsultaLenguajeNaturalResponse> ConsultarAsync(string pregunta,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            new ConsultaLenguajeNaturalResponse(false, GeminiMcpAgentService.MensajeDeFalla));
    }
}
