namespace SolarUY.Application.Sensors;

/// <summary>Resultado de mandarle una orden al sensor (spec del simulador, sección 3).</summary>
public enum ResultadoOrdenSensor
{
    /// <summary>200: la orden se aplicó y el sensor devolvió el estado resultante.</summary>
    Ok,

    /// <summary>
    /// 404: no existe el sensor con ese identificador de hardware, o no existe ese panel
    /// dentro del sensor. El contrato usa el mismo código para los dos casos.
    /// </summary>
    NoEncontrado,

    /// <summary>409: el estado pedido ya estaba aplicado.</summary>
    YaAplicado,

    /// <summary>El sensor no respondió, tardó demasiado o devolvió algo inesperado.</summary>
    SinRespuesta
}
