namespace SolarUY.Application.DTOs;

/// <summary>Aviso de que un panel acaba de pasar a FALLA_DETECTADA.</summary>
public record AlertaFallaDto(
    Guid PanelId,
    string CodigoPanel,
    string NodoNombre,
    Guid ParqueId);