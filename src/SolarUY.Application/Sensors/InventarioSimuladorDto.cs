namespace SolarUY.Application.Sensors;

/// <summary>
/// Un panel del inventario del simulador (sección 4 de su spec).
/// </summary>
public record InventarioPanelDto(
    string CodigoPanel,
    double PotenciaNominalW,
    bool Activo,
    string? Falla,
    bool FallaForzada);

/// <summary>
/// Un sensor del inventario del simulador, con los paneles que emula.
/// </summary>
public record InventarioSensorDto(
    string IdentificadorHardwareARM,
    bool VolcadoRedActivo,
    IReadOnlyList<InventarioPanelDto> Paneles);
