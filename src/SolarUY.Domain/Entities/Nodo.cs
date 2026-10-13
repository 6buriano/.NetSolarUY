using SolarUY.Domain.Enums;
using SolarUY.Domain.Exceptions;

namespace SolarUY.Domain.Entities;

/// <summary>
/// Nodo de un parque solar. Por regla de negocio tiene exactamente un inversor asignado.
/// También es el responsable de diagnosticar la salud de sus paneles, porque la caída de
/// potencia se mide contra los paneles hermanos del mismo nodo.
/// </summary>
public class Nodo
{
    /// <summary>Temperatura por encima de la cual el panel está en falla, sin importar el resto.</summary>
    public const double TemperaturaMaximaCelsius = 75d;

    /// <summary>Hasta esta caída respecto del promedio del nodo, el panel sigue operativo.</summary>
    public const double UmbralRendimientoBajo = 0.15d;

    /// <summary>Más de esta caída respecto del promedio del nodo ya es falla.</summary>
    public const double UmbralFallaDetectada = 0.40d;

    /// <summary>
    /// Si el rendimiento promedio de los otros paneles no llega a este valor (5 % de la nominal),
    /// hay muy poca luz y no se evalúa la caída, para no generar falsos positivos de noche.
    /// </summary>
    public const double RendimientoMinimoParaEvaluarCaida = 0.05d;

    /// <summary>
    /// Margen para comparar contra los umbrales. La spec los define inclusivos ("hasta 15 %",
    /// "más de 15 % y hasta 40 %"), y sin este margen el ruido del punto flotante decidiría
    /// en los valores exactos.
    /// </summary>
    private const double ToleranciaDeComparacion = 1e-9;

    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public string UbicacionDentroParque { get; private set; } = string.Empty;
    public Guid ParqueSolarId { get; private set; }
    public Inversor Inversor { get; private set; } = null!;
    public List<PanelSolar> Paneles { get; private set; } = [];

    /// <summary>Constructor sin parámetros que exige Entity Framework Core para materializar.</summary>
    private Nodo()
    {
    }

    /// <summary>
    /// Regla de negocio 1: un nodo no puede existir sin su inversor, por eso se exige al construir.
    /// </summary>
    public Nodo(string nombre, string ubicacionDentroParque, Guid parqueSolarId, Inversor inversor)
    {
        Id = Guid.NewGuid();
        Inversor = inversor ?? throw new ReglaDeNegocioException("Todo nodo debe tener exactamente un inversor asignado.");
        Inversor.AsignarANodo(Id);
        EstablecerDatos(nombre, ubicacionDentroParque, parqueSolarId);
    }

    public void Actualizar(string nombre, string ubicacionDentroParque, Guid parqueSolarId)
        => EstablecerDatos(nombre, ubicacionDentroParque, parqueSolarId);

    public bool TienePaneles() => Paneles.Count > 0;

    /// <summary>
    /// Busca un panel por el código con el que lo conoce el sensor. La comparación no distingue
    /// mayúsculas, igual que el simulador.
    /// </summary>
    public PanelSolar? BuscarPanelPorCodigo(string codigoPanel)
        => Paneles.FirstOrDefault(panel =>
            string.Equals(panel.CodigoPanel, codigoPanel, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Diagnostica las lecturas de todos los paneles del nodo y devuelve el estado que le
    /// corresponde a cada una. El sensor sólo mide; esto vive en el dominio porque la caída de
    /// potencia se mide contra el promedio de los otros paneles activos del mismo nodo.
    /// Reglas (spec del simulador, sección 9):
    /// <list type="bullet">
    ///   <item>Panel inactivo: <see cref="EstadoSalud.DESCONECTADO"/>.</item>
    ///   <item>Temperatura mayor a 75 °C: <see cref="EstadoSalud.FALLA_DETECTADA"/>.</item>
    ///   <item>Sin referencia (no hay otros paneles activos con lectura) o poca luz: no se evalúa la caída.</item>
    ///   <item>Caída de hasta 15 %: <see cref="EstadoSalud.OPERATIVO"/>.</item>
    ///   <item>Caída de más de 15 % y hasta 40 %: <see cref="EstadoSalud.RENDIMIENTO_BAJO"/>.</item>
    ///   <item>Caída de más de 40 %: <see cref="EstadoSalud.FALLA_DETECTADA"/>.</item>
    /// </list>
    /// </summary>
    /// <param name="lecturas">Las lecturas de los paneles de este nodo, sin diagnosticar.</param>
    /// <returns>Las mismas lecturas, ya con su <see cref="LecturaTelemetria.EstadoDetectado"/>.</returns>
    public IReadOnlyList<LecturaTelemetria> EvaluarLecturas(IReadOnlyList<LecturaTelemetria> lecturas)
    {
        ArgumentNullException.ThrowIfNull(lecturas);

        var idsDelNodo = Paneles.Select(panel => panel.Id).ToHashSet();
        var ajenas = lecturas.Where(lectura => !idsDelNodo.Contains(lectura.PanelSolarId)).ToList();
        if (ajenas.Count > 0)
        {
            throw new ReglaDeNegocioException(
                "Hay lecturas que no corresponden a paneles de este nodo y no se pueden evaluar.");
        }

        var lecturaPorPanel = lecturas.ToDictionary(lectura => lectura.PanelSolarId);

        // Rendimiento relativo (potencia generada / potencia nominal) de cada panel activo con lectura.
        // Normalizar permite comparar paneles de distinta potencia nominal.
        var rendimientos = Paneles
            .Where(panel => panel.Activo && lecturaPorPanel.ContainsKey(panel.Id))
            .ToDictionary(panel => panel.Id, panel => lecturaPorPanel[panel.Id].PotenciaGeneradaWatts / panel.PotenciaNominalW);

        foreach (var panel in Paneles)
        {
            if (!lecturaPorPanel.TryGetValue(panel.Id, out var lectura))
            {
                // Sin lectura no hay nada que diagnosticar: se deja el estado como estaba.
                continue;
            }

            var estado = DiagnosticarPanel(panel, lectura, rendimientos);
            lectura.EstablecerEstadoDetectado(estado);
            panel.CambiarEstado(estado);
        }

        return lecturas;
    }

    private static EstadoSalud DiagnosticarPanel(PanelSolar panel, LecturaTelemetria lectura,
        IReadOnlyDictionary<Guid, double> rendimientos)
    {
        if (!panel.Activo)
        {
            return EstadoSalud.DESCONECTADO;
        }

        if (lectura.TemperaturaCelsius > TemperaturaMaximaCelsius)
        {
            return EstadoSalud.FALLA_DETECTADA;
        }

        var rendimientoDeLosOtros = rendimientos
            .Where(rendimiento => rendimiento.Key != panel.Id)
            .Select(rendimiento => rendimiento.Value)
            .ToList();

        // Menos de dos paneles activos con lectura: no hay referencia contra la cual comparar.
        if (rendimientoDeLosOtros.Count == 0)
        {
            return EstadoSalud.OPERATIVO;
        }

        var promedioOtros = rendimientoDeLosOtros.Average();

        // Noche o muy poca luz: comparar acá generaría falsos positivos.
        if (promedioOtros < RendimientoMinimoParaEvaluarCaida)
        {
            return EstadoSalud.OPERATIVO;
        }

        if (!rendimientos.TryGetValue(panel.Id, out var rendimientoDelPanel))
        {
            return EstadoSalud.OPERATIVO;
        }

        var caida = 1 - (rendimientoDelPanel / promedioOtros);

        if (caida <= UmbralRendimientoBajo + ToleranciaDeComparacion)
        {
            return EstadoSalud.OPERATIVO;
        }

        return caida <= UmbralFallaDetectada + ToleranciaDeComparacion
            ? EstadoSalud.RENDIMIENTO_BAJO
            : EstadoSalud.FALLA_DETECTADA;
    }

    private void EstablecerDatos(string nombre, string ubicacionDentroParque, Guid parqueSolarId)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ReglaDeNegocioException("El nombre del nodo es obligatorio.");

        if (string.IsNullOrWhiteSpace(ubicacionDentroParque))
            throw new ReglaDeNegocioException("La ubicación dentro del parque es obligatoria.");

        if (parqueSolarId == Guid.Empty)
            throw new ReglaDeNegocioException("El nodo debe pertenecer a un parque solar.");

        Nombre = nombre.Trim();
        UbicacionDentroParque = ubicacionDentroParque.Trim();
        ParqueSolarId = parqueSolarId;
    }
}
