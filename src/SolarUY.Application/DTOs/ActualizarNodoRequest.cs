namespace SolarUY.Application.DTOs;

/// <summary>Datos necesarios para modificar un nodo, su inversor y su sensor ARM.</summary>
public record ActualizarNodoRequest(
    Guid Id,
    string Nombre,
    string UbicacionDentroParque,
    Guid ParqueSolarId,
    InversorRequest Inversor,
    SensorDispositivoRequest Sensor);
