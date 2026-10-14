using SolarUY.Application.Sensors;
using SolarUY.Domain.Entities;

namespace SolarUY.Application.Diagnostics;

/// <summary>
/// Compara lo cargado en el sistema con lo que emula el simulador y devuelve los desajustes.
/// Es lógica pura: recibe los nodos y el inventario ya leídos, así se puede probar sin base de
/// datos ni red.
/// </summary>
public static class ComparadorVinculacion
{
    public static InformeVinculacion Comparar(IReadOnlyList<Nodo> nodos, IReadOnlyList<InventarioSensorDto> inventario)
    {
        ArgumentNullException.ThrowIfNull(nodos);
        ArgumentNullException.ThrowIfNull(inventario);

        var hallazgos = new List<HallazgoVinculacion>();

        var sensoresDelSimulador = new Dictionary<string, InventarioSensorDto>(StringComparer.OrdinalIgnoreCase);
        foreach (var sensor in inventario)
        {
            sensoresDelSimulador[sensor.IdentificadorHardwareARM] = sensor;
        }

        var usados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var panelesEnElSistema = 0;

        foreach (var nodo in nodos)
        {
            var sensor = nodo.Inversor?.Sensor;

            if (sensor is null)
            {
                hallazgos.Add(new HallazgoVinculacion(SeveridadHallazgo.Error, nodo.Nombre, null,
                    "El nodo no tiene sensor ARM cargado, así que el colector no tiene a quién pedirle telemetría."));
                continue;
            }

            var hardware = sensor.IdentificadorHardwareARM;
            usados.Add(hardware);
            panelesEnElSistema += nodo.Paneles.Count;

            if (!sensor.Activo)
            {
                hallazgos.Add(new HallazgoVinculacion(SeveridadHallazgo.Advertencia, nodo.Nombre, hardware,
                    "El sensor está desactivado en el sistema: el colector no lo consulta."));
            }

            if (!sensoresDelSimulador.TryGetValue(hardware, out var sensorSimulado))
            {
                hallazgos.Add(new HallazgoVinculacion(SeveridadHallazgo.Error, nodo.Nombre, hardware,
                    $"El simulador no emula un sensor '{hardware}'. Los que existen: {Listar(sensoresDelSimulador.Keys)}."));
                continue;
            }

            foreach (var panel in nodo.Paneles)
            {
                var panelSimulado = sensorSimulado.Paneles.FirstOrDefault(candidato =>
                    string.Equals(candidato.CodigoPanel, panel.CodigoPanel, StringComparison.OrdinalIgnoreCase));

                if (panelSimulado is null)
                {
                    hallazgos.Add(new HallazgoVinculacion(SeveridadHallazgo.Error, nodo.Nombre, hardware,
                        $"El panel '{panel.CodigoPanel}' no existe en el sensor, así que nunca va a recibir lecturas. " +
                        $"El sensor informa: {Listar(sensorSimulado.Paneles.Select(p => p.CodigoPanel))}."));
                    continue;
                }

                if (Math.Abs(panel.PotenciaNominalW - panelSimulado.PotenciaNominalW) > 0.01)
                {
                    hallazgos.Add(new HallazgoVinculacion(SeveridadHallazgo.Advertencia, nodo.Nombre, hardware,
                        $"El panel '{panel.CodigoPanel}' está cargado con {panel.PotenciaNominalW:0.##} W y el sensor " +
                        $"genera hasta {panelSimulado.PotenciaNominalW:0.##} W: la comparación de caída entre paneles " +
                        "del mismo nodo queda distorsionada."));
                }
            }

            foreach (var panelSimulado in sensorSimulado.Paneles)
            {
                var cargadoEnElSistema = nodo.Paneles.Any(panel =>
                    string.Equals(panel.CodigoPanel, panelSimulado.CodigoPanel, StringComparison.OrdinalIgnoreCase));

                if (!cargadoEnElSistema)
                {
                    hallazgos.Add(new HallazgoVinculacion(SeveridadHallazgo.Advertencia, nodo.Nombre, hardware,
                        $"El sensor informa el panel '{panelSimulado.CodigoPanel}' y el sistema no lo tiene cargado: " +
                        "esas lecturas se descartan."));
                }
            }
        }

        // Sensores que el simulador emula y ningún nodo usa: son identificadores libres para crear nodos.
        var libres = inventario
            .Where(sensor => !usados.Contains(sensor.IdentificadorHardwareARM))
            .Select(sensor => sensor.IdentificadorHardwareARM)
            .OrderBy(hardware => hardware, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new InformeVinculacion(
            SimuladorConsultado: true,
            ErrorConsulta: null,
            NodosRevisados: nodos.Count,
            PanelesRevisados: panelesEnElSistema,
            SensoresEnElSimulador: inventario.Count,
            Hallazgos: hallazgos,
            SensoresLibres: libres);
    }

    private static string Listar(IEnumerable<string> valores)
    {
        var lista = valores.OrderBy(valor => valor, StringComparer.OrdinalIgnoreCase).ToList();
        return lista.Count == 0 ? "ninguno" : string.Join(", ", lista);
    }
}
