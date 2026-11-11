using System.Reflection;
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using SolarUY.Application.Services;
using SolarUY.Collector;
using SolarUY.Domain.Entities;
using SolarUY.Infrastructure;
using SolarUY.Web.Models;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using RefAssembly = System.Reflection.Assembly;

namespace SolarUY.CodeQuality.Tests;

/// <summary>
/// Tests de arquitectura: vigilan las reglas de `.agents/rules/1_clean_architecture.md` y
/// `3_convenciones_nomenclatura.md`. No prueban comportamiento, prueban la forma del código.
/// </summary>
/// <remarks>
/// Las reglas de dependencia entre capas se escriben con ArchUnitNET, que compara los tipos de los
/// cinco ensamblados cargados. La prohibición de EF Core y ASP.NET dentro del dominio se comprueba
/// además sobre las referencias del ensamblado: es la forma más directa de verlo y no depende de
/// que esas librerías estén cargadas en el modelo.
/// </remarks>
public class ArquitecturaTests
{
    private static readonly RefAssembly EnsambladoDominio = typeof(ParqueSolar).Assembly;
    private static readonly RefAssembly EnsambladoAplicacion = typeof(ParqueService).Assembly;
    private static readonly RefAssembly EnsambladoInfraestructura = typeof(DependencyInjection).Assembly;
    private static readonly RefAssembly EnsambladoWeb = typeof(NodoFormViewModel).Assembly;
    private static readonly RefAssembly EnsambladoCollector = typeof(ColectorTelemetriaWorker).Assembly;

    private static readonly Architecture Arquitectura = new ArchLoader()
        .LoadAssemblies(EnsambladoDominio, EnsambladoAplicacion, EnsambladoInfraestructura, EnsambladoWeb,
            EnsambladoCollector)
        .Build();

    // ---------- Dependencias entre capas ----------

    [Fact]
    public void ElDominioNoDependeDeNingunaOtraCapa()
    {
        IArchRule regla = Types().That().ResideInAssembly(EnsambladoDominio)
            .Should().NotDependOnAny(Types().That().ResideInAssembly(EnsambladoAplicacion))
            .AndShould().NotDependOnAny(Types().That().ResideInAssembly(EnsambladoInfraestructura))
            .AndShould().NotDependOnAny(Types().That().ResideInAssembly(EnsambladoWeb))
            .AndShould().NotDependOnAny(Types().That().ResideInAssembly(EnsambladoCollector))
            .Because("el dominio es el centro: no conoce a nadie");

        regla.Check(Arquitectura);
    }

    [Fact]
    public void LaAplicacionSoloDependeDelDominio()
    {
        IArchRule regla = Types().That().ResideInAssembly(EnsambladoAplicacion)
            .Should().NotDependOnAny(Types().That().ResideInAssembly(EnsambladoInfraestructura))
            .AndShould().NotDependOnAny(Types().That().ResideInAssembly(EnsambladoWeb))
            .AndShould().NotDependOnAny(Types().That().ResideInAssembly(EnsambladoCollector))
            .Because("los casos de uso no conocen la base ni las pantallas: piden interfaces");

        regla.Check(Arquitectura);
    }

    [Fact]
    public void LaInfraestructuraNoDependeDeLasPantallasNiDelColector()
    {
        IArchRule regla = Types().That().ResideInAssembly(EnsambladoInfraestructura)
            .Should().NotDependOnAny(Types().That().ResideInAssembly(EnsambladoWeb))
            .AndShould().NotDependOnAny(Types().That().ResideInAssembly(EnsambladoCollector))
            .Because("los detalles técnicos de adentro no conocen a los de afuera");

        regla.Check(Arquitectura);
    }

    [Fact]
    public void LaWebYElColectorNoSeConocenEntreSi()
    {
        Types().That().ResideInAssembly(EnsambladoWeb)
            .Should().NotDependOnAny(Types().That().ResideInAssembly(EnsambladoCollector))
            .Because("son los dos puntos de arranque, no se usan entre ellos")
            .Check(Arquitectura);

        Types().That().ResideInAssembly(EnsambladoCollector)
            .Should().NotDependOnAny(Types().That().ResideInAssembly(EnsambladoWeb))
            .Check(Arquitectura);
    }

    [Fact]
    public void SoloElProgramaDeLaWebUsaInfrastructureDirectamente()
    {
        // La excepción es Program.cs, donde se conectan las interfaces con sus implementaciones.
        Types().That().ResideInAssembly(EnsambladoWeb)
            .And().DoNotHaveNameStartingWith("Program")
            .Should().NotDependOnAny(Types().That().ResideInAssembly(EnsambladoInfraestructura))
            .Because("los controladores y las vistas usan Application, nunca Infrastructure")
            .Check(Arquitectura);
    }

    // ---------- Lo que el dominio no puede referenciar ----------

    [Fact]
    public void ElEnsambladoDelDominioNoReferenciaEntityFrameworkNiAspNet()
    {
        var referencias = EnsambladoDominio.GetReferencedAssemblies()
            .Select(referencia => referencia.Name ?? string.Empty)
            .ToList();

        Assert.DoesNotContain(referencias,
            nombre => nombre.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
        Assert.DoesNotContain(referencias,
            nombre => nombre.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

    // ---------- Convenciones de nombres y ubicación ----------

    [Fact]
    public void LasInterfacesDelDominioEmpiezanConI()
        => Interfaces().That().ResideInAssembly(EnsambladoDominio)
            .Should().HaveNameStartingWith("I")
            .Because("es la convención de nombres del proyecto (regla 3)")
            .Check(Arquitectura);

    [Fact]
    public void LosRepositoriosSeDeclaranEnDomainRepositories()
        => Interfaces().That().HaveNameEndingWith("Repository")
            .Should().ResideInNamespace("SolarUY.Domain.Repositories")
            .Because("los contratos de persistencia viven en el dominio")
            .Check(Arquitectura);

    [Fact]
    public void LosServiciosDeAplicacionVivenEnApplicationServices()
        => Classes().That().ResideInAssembly(EnsambladoAplicacion).And().HaveNameEndingWith("Service")
            .Should().ResideInNamespace("SolarUY.Application.Services")
            .Because("un caso de uso por servicio, todos juntos en la misma carpeta")
            .Check(Arquitectura);

    [Fact]
    public void LosControladoresVivenEnWebControllers()
        => Classes().That().ResideInAssembly(EnsambladoWeb).And().HaveNameEndingWith("Controller")
            .Should().ResideInNamespace("SolarUY.Web.Controllers")
            .Because("un controlador por recurso, en la carpeta Controllers")
            .Check(Arquitectura);

    [Fact]
    public void LasEntidadesDelDominioSonPublicas()
        => Classes().That().ResideInNamespace("SolarUY.Domain.Entities")
            .Should().BePublic()
            .Because("las entidades las usan todas las capas de afuera")
            .Check(Arquitectura);
}
