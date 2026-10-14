namespace SolarUY.Application.Diagnostics;

/// <summary>Qué tan grave es un desajuste entre el sistema y el hardware simulado.</summary>
public enum SeveridadHallazgo
{
    /// <summary>Impide recibir telemetría de ese nodo o panel.</summary>
    Error,

    /// <summary>Conviene corregirlo, pero la recolección sigue funcionando.</summary>
    Advertencia
}

/// <summary>Un desajuste entre lo cargado en el sistema y lo que emula el simulador.</summary>
public record HallazgoVinculacion(
    SeveridadHallazgo Severidad,
    string Nodo,
    string? Sensor,
    string Detalle);

/// <summary>Resultado de comparar el sistema contra el inventario del simulador.</summary>
public record InformeVinculacion(
    bool SimuladorConsultado,
    string? ErrorConsulta,
    int NodosRevisados,
    int PanelesRevisados,
    int SensoresEnElSimulador,
    IReadOnlyList<HallazgoVinculacion> Hallazgos,
    IReadOnlyList<string> SensoresLibres)
{
    /// <summary>Todo lo cargado tiene su contraparte en el simulador.</summary>
    public bool TodoVinculado => SimuladorConsultado && Hallazgos.Count == 0;

    public int Errores => Hallazgos.Count(hallazgo => hallazgo.Severidad == SeveridadHallazgo.Error);

    public int Advertencias => Hallazgos.Count(hallazgo => hallazgo.Severidad == SeveridadHallazgo.Advertencia);

    /// <summary>Informe para cuando no se pudo consultar el simulador: no se pudo verificar nada.</summary>
    public static InformeVinculacion SinSimulador(string error, int nodosRevisados)
        => new(false, error, nodosRevisados, 0, 0, [], []);
}
