namespace SolarUY.Application.DTOs;

/// <summary>
/// Datos necesarios para dar de alta un nodo. El inversor viene incluido porque, por regla
/// de negocio, un nodo no puede existir sin exactamente un inversor asignado. El sensor
/// completa la dupla inversor + sensor ARM que representa a cada nodo, y puede venir en
/// <c>null</c>: un nodo se puede cargar antes de que su sensor esté conectado.
/// </summary>
public record CrearNodoRequest(
    string Nombre,
    string UbicacionDentroParque,
    Guid ParqueSolarId,
    InversorRequest Inversor,
    SensorDispositivoRequest? Sensor);
