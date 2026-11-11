using SolarUY.Domain.Repositories;

namespace SolarUY.Application.Services;

/// <summary>
/// Política de retención de la telemetría. El colector guarda una lectura por panel cada pocos
/// segundos (unas 260.000 filas por día con tres nodos), así que sin purga la tabla crece sin
/// límite hasta llenar el disco del servidor.
/// </summary>
/// <remarks>
/// La fecha límite se calcula con <see cref="TimeProvider"/> para poder probar la política sin
/// depender del reloj de la máquina.
/// </remarks>
public class RetencionTelemetriaService
{
    /// <summary>Días de lecturas que se conservan cuando no se configura otra cosa.</summary>
    public const int DiasPorDefecto = 90;

    private readonly ITelemetriaRepository _telemetriaRepository;
    private readonly TimeProvider _reloj;

    public RetencionTelemetriaService(ITelemetriaRepository telemetriaRepository, TimeProvider reloj)
    {
        _telemetriaRepository = telemetriaRepository;
        _reloj = reloj;
    }

    /// <summary>
    /// Fecha (UTC) anterior a la cual ya no se conservan lecturas: el momento actual menos
    /// <paramref name="dias"/> días.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Si <paramref name="dias"/> no es mayor a cero: retener cero días borraría todo, incluida la
    /// lectura que acaba de guardar el colector.
    /// </exception>
    public DateTime CalcularLimite(int dias)
    {
        if (dias <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dias), dias,
                "Los días de retención tienen que ser mayores a cero.");
        }

        return _reloj.GetUtcNow().UtcDateTime.AddDays(-dias);
    }

    /// <summary>
    /// Borra las lecturas anteriores al límite y devuelve cuántas borró. Cero significa que no
    /// había nada viejo.
    /// </summary>
    public async Task<int> PurgarAsync(int dias, CancellationToken cancellationToken = default)
    {
        var limite = CalcularLimite(dias);
        return await _telemetriaRepository.EliminarAnterioresAsync(limite, cancellationToken);
    }
}
