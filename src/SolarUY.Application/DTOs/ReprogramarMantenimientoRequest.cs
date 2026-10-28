namespace SolarUY.Application.DTOs;

/// <summary>
/// Datos para reprogramar una tarea de mantenimiento todavía no completada (cambia la fecha,
/// la descripción y/o quién la tiene asignada).
/// </summary>
public record ReprogramarMantenimientoRequest(
    Guid Id,
    DateTime FechaProgramada,
    string Descripcion,
    string? PersonalAsignado);
