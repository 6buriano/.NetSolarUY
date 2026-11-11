using SolarUY.Application.Consultas;
using SolarUY.Application.DTOs;
using SolarUY.Domain.Exceptions;

namespace SolarUY.Application.Services;

/// <summary>
/// Caso de uso de las consultas en lenguaje natural: valida la pregunta y se la pasa al agente.
/// Cada consulta es independiente: no guarda historial.
/// </summary>
public class ConsultaService
{
    /// <summary>
    /// Largo máximo de una pregunta, en caracteres. Es <c>const</c> porque también lo usan las
    /// validaciones del formulario, que necesitan una constante en tiempo de compilación.
    /// </summary>
    public const int LargoMaximoPregunta = 500;

    private readonly IMCPAgentService _agente;

    public ConsultaService(IMCPAgentService agente)
    {
        _agente = agente;
    }

    /// <summary>
    /// Recorta la pregunta, la valida y devuelve la respuesta del agente.
    /// </summary>
    /// <exception cref="ReglaDeNegocioException">
    /// Si la pregunta queda vacía o supera <see cref="LargoMaximoPregunta"/> caracteres.
    /// </exception>
    public async Task<ConsultaLenguajeNaturalResponse> ConsultarAsync(ConsultaLenguajeNaturalRequest request,
        CancellationToken cancellationToken = default)
    {
        var pregunta = (request.Pregunta ?? string.Empty).Trim();

        if (pregunta.Length == 0)
        {
            throw new ReglaDeNegocioException("Escribí una pregunta: no se puede consultar en blanco.");
        }

        if (pregunta.Length > LargoMaximoPregunta)
        {
            throw new ReglaDeNegocioException(
                $"La pregunta no puede superar los {LargoMaximoPregunta} caracteres.");
        }

        return await _agente.ConsultarAsync(pregunta, cancellationToken);
    }
}
