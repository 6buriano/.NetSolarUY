using SolarUY.Application.DTOs;
using SolarUY.Application.Mappings;
using SolarUY.Domain.Entities;
using SolarUY.Domain.Enums;

namespace SolarUY.Application.Services;

/// <summary>Lo que cambió entre una foto y la anterior: lecturas nuevas y paneles que pasaron a falla.</summary>
public record CambiosMonitoreo(
    IReadOnlyList<LecturaTelemetriaDto> LecturasNuevas,
    IReadOnlyList<AlertaFallaDto> Fallas);

/// <summary>
/// Recuerda qué se envió en la vuelta anterior del monitoreo (req012) para mandar solo lo nuevo.
/// Vive una sola instancia por réplica (Singleton). No guarda datos de usuario: si nginx pasa a la
/// otra réplica, esa arma su propio seguimiento desde cero.
/// </summary>
public class SeguimientoMonitoreo
{
    private readonly object _candado = new();
    private readonly Dictionary<Guid, DateTime> _ultimaLecturaEnviada = new();
    private Dictionary<Guid, EstadoSalud>? _estadoAnterior;

    public CambiosMonitoreo Comparar(InstantaneaMonitoreoDto instantanea, IReadOnlyList<LecturaTelemetria> lecturas)
    {
        lock (_candado)
        {
            // En la primera vuelta no hay con qué comparar: se registra todo como "ya visto" y no se
            // manda nada, para no avisar de fallas viejas ni repetir lecturas que la pantalla ya tiene.
            var esPrimeraVuelta = _estadoAnterior is null;

            var lecturasNuevas = new List<LecturaTelemetriaDto>();
            foreach (var lectura in lecturas.OrderBy(lectura => lectura.FechaHora))
            {
                var yaEnviada = _ultimaLecturaEnviada.TryGetValue(lectura.PanelSolarId, out var ultima)
                    && lectura.FechaHora <= ultima;
                if (yaEnviada)
                {
                    continue;
                }

                _ultimaLecturaEnviada[lectura.PanelSolarId] = lectura.FechaHora;
                if (!esPrimeraVuelta)
                {
                    lecturasNuevas.Add(lectura.ToDto());
                }
            }

            var fallas = new List<AlertaFallaDto>();
            var estadoActual = new Dictionary<Guid, EstadoSalud>();
            foreach (var parque in instantanea.Parques)
            {
                foreach (var nodo in parque.Nodos)
                {
                    foreach (var panel in nodo.Paneles)
                    {
                        estadoActual[panel.PanelId] = panel.Estado;

                        var estabaEnFalla = _estadoAnterior is not null
                            && _estadoAnterior.TryGetValue(panel.PanelId, out var antes)
                            && antes == EstadoSalud.FALLA_DETECTADA;

                        if (!esPrimeraVuelta && panel.Estado == EstadoSalud.FALLA_DETECTADA && !estabaEnFalla)
                        {
                            fallas.Add(new AlertaFallaDto(panel.PanelId, panel.CodigoPanel, nodo.Nombre, parque.ParqueId));
                        }
                    }
                }
            }

            _estadoAnterior = estadoActual;
            return new CambiosMonitoreo(lecturasNuevas, fallas);
        }
    }
}