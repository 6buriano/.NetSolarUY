namespace SolarUY.IntegrationTests;

/// <summary>
/// Como <c>[Fact]</c>, pero se saltea con un aviso claro cuando no hay un MySQL contra el cual correr
/// (variable de entorno <see cref="EntornoMySql.Variable"/>). Así <c>dotnet test</c> no se rompe en
/// una máquina sin Docker; el CI sí define la variable, así que ahí corren siempre.
/// </summary>
public sealed class FactMySqlAttribute : FactAttribute
{
    public FactMySqlAttribute()
    {
        if (!EntornoMySql.Disponible)
        {
            Skip = $"Falta la variable de entorno {EntornoMySql.Variable} (cadena de conexión a un MySQL " +
                   "con permisos para crear bases). Ver tests/SolarUY.IntegrationTests/README.md.";
        }
    }
}
