using SolarUY.Domain.Exceptions;

namespace SolarUY.Domain.Entities;

/// <summary>
/// Parque solar: agrupación de nivel superior del ABM. Contiene los nodos.
/// </summary>
public class ParqueSolar
{
    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public string Ubicacion { get; private set; } = string.Empty;
    public double CapacidadInstaladaKW { get; private set; }
    public DateTime FechaInstalacion { get; private set; }
    public List<Nodo> Nodos { get; private set; } = [];

    /// <summary>Constructor sin parámetros que exige Entity Framework Core para materializar.</summary>
    private ParqueSolar()
    {
    }

    public ParqueSolar(string nombre, string ubicacion, double capacidadInstaladaKW, DateTime fechaInstalacion)
    {
        Id = Guid.NewGuid();
        EstablecerDatos(nombre, ubicacion, capacidadInstaladaKW, fechaInstalacion);
    }

    public void Actualizar(string nombre, string ubicacion, double capacidadInstaladaKW, DateTime fechaInstalacion)
        => EstablecerDatos(nombre, ubicacion, capacidadInstaladaKW, fechaInstalacion);

    public bool TieneNodos() => Nodos.Count > 0;

    private void EstablecerDatos(string nombre, string ubicacion, double capacidadInstaladaKW, DateTime fechaInstalacion)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ReglaDeNegocioException("El nombre del parque solar es obligatorio.");

        if (string.IsNullOrWhiteSpace(ubicacion))
            throw new ReglaDeNegocioException("La ubicación del parque solar es obligatoria.");

        if (capacidadInstaladaKW <= 0)
            throw new ReglaDeNegocioException("La capacidad instalada debe ser mayor a cero.");

        Nombre = nombre.Trim();
        Ubicacion = ubicacion.Trim();
        CapacidadInstaladaKW = capacidadInstaladaKW;
        FechaInstalacion = fechaInstalacion.Date;
    }
}
