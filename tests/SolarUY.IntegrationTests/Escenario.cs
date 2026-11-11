using Microsoft.Extensions.DependencyInjection;
using SolarUY.Application.DTOs;
using SolarUY.Application.Services;
using SolarUY.Domain.Enums;

namespace SolarUY.IntegrationTests;

/// <summary>Atajos para armar parques, nodos y paneles en la base de un test, por los mismos servicios que usa el ABM.</summary>
public static class Escenario
{
    public static async Task<Guid> CrearParqueAsync(this IServiceProvider servicios, string nombre = "Parque Test")
    {
        using var scope = servicios.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ParqueService>().CrearAsync(
            new CrearParqueRequest(nombre, "Salto", 10, new DateTime(2026, 1, 1)));
    }

    public static async Task<Guid> CrearNodoAsync(this IServiceProvider servicios, Guid parqueId,
        string nombre, string? hardware)
    {
        using var scope = servicios.CreateScope();
        var sensor = hardware is null ? null : new SensorDispositivoRequest(hardware, "1.0.0", "http://sensor.test", true);
        return await scope.ServiceProvider.GetRequiredService<NodoService>().CrearAsync(
            new CrearNodoRequest(nombre, "Norte", parqueId, new InversorRequest("SMA", $"SN-{nombre}", 97.5, true), sensor));
    }

    public static async Task<Guid> CrearPanelAsync(this IServiceProvider servicios, Guid nodoId, string codigo,
        bool activo = true)
    {
        using var scope = servicios.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<PanelService>().CrearAsync(
            new CrearPanelRequest(codigo, "JAM72S30", "JA Solar", 300, EstadoSalud.OPERATIVO, activo,
                new DateTime(2026, 1, 1), nodoId));
    }
}
