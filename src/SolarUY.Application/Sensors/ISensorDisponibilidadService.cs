namespace SolarUY.Application.Sensors;

/// <summary>Resultado de preguntarle al hardware si un sensor existe.</summary>
/// <param name="VerificacionPosible">
/// Si este entorno puede verificar. En producción es <c>false</c>: no hay inventario que consultar,
/// porque un sensor real simplemente es el que es.
/// </param>
/// <param name="SePudoConsultar">
/// Si la consulta se llegó a hacer y respondió. Puede fallar aunque la verificación sea posible,
/// por ejemplo si el simulador está apagado.
/// </param>
/// <param name="Existe">Si el identificador existe en el hardware.</param>
/// <param name="IdentificadoresExistentes">Los que sí existen, para poder mostrárselos al usuario.</param>
public record DisponibilidadSensor(
    bool VerificacionPosible,
    bool SePudoConsultar,
    bool Existe,
    IReadOnlyList<string> IdentificadoresExistentes);

/// <summary>Resultado de pedirle al hardware que empiece a emular un sensor nuevo.</summary>
public record ResultadoAltaSensor(
    bool Creado,
    string? Error,
    IReadOnlyList<string> CodigosDePanel);

/// <summary>
/// Pregunta si un sensor existe en el hardware, y permite crearlo cuando ese hardware es el
/// simulador.
/// </summary>
/// <remarks>
/// Existe para que el ABM de nodos pueda avisar "ese identificador no existe" sin depender él mismo
/// de la API de administración del mock. En producción se registra la implementación nula, que no
/// verifica nada: el nodo se crea igual que siempre.
/// </remarks>
public interface ISensorDisponibilidadService
{
    Task<DisponibilidadSensor> VerificarAsync(string identificadorHardwareARM,
        CancellationToken cancellationToken = default);

    Task<ResultadoAltaSensor> CrearAsync(string identificadorHardwareARM,
        CancellationToken cancellationToken = default);
}
