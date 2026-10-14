namespace SolarUY.Application.DTOs;

/// <summary>
/// Datos necesarios para modificar un nodo, su inversor y su sensor ARM. El sensor puede venir en
/// <c>null</c>: significa que el nodo queda sin sensor (o sigue sin tenerlo).
/// </summary>
public record ActualizarNodoRequest(
    Guid Id,
    string Nombre,
    string UbicacionDentroParque,
    Guid ParqueSolarId,
    InversorRequest Inversor,
    SensorDispositivoRequest? Sensor);
