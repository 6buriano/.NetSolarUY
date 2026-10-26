# Especificación Técnica: Verificación de vinculación

> Documentado después de implementar, a partir del código de `main`.

## 1. Application (`SolarUY.Application`)
* **`ISimuladorAdminClient.ObtenerInventarioAsync(urlSimulador)`** → lista de `InventarioSensorDto` (sensores con sus paneles y potencias), o `null` si no responde.
* **`VerificacionVinculacionService`** (depende de `INodoRepository` e `ISimuladorAdminClient`):
  * `VerificarAsync(urlSimulador)`: trae todos los nodos y el inventario. Si el inventario es `null` → `InformeVinculacion.SinSimulador(...)`. Si no → `ComparadorVinculacion.Comparar(nodos, inventario)`.
* **`ComparadorVinculacion`** (`Diagnostics/`, clase `static`): función **pura**, sin base ni HTTP; recibe los nodos y el inventario y arma el informe. Por ser pura es fácil de testear.
* **`InformeVinculacion`**: `Errores`, `Advertencias`, sensores libres, `TodoVinculado` y la fábrica `SinSimulador(...)`.

## 2. Infrastructure (`SolarUY.Infrastructure`)
* Cliente HTTP de `ISimuladorAdminClient` (mismo timeout corto que el de los sensores).

## 3. Web (`SolarUY.Web`)
* **`DiagnosticoController.Vinculacion`** (`[Authorize(Roles = "Administrador")]`): si `!IsDevelopment()` → `NotFound()`. La URL del simulador sale de `Simulador:UrlBase`.
* Vista `Views/Diagnostico/Vinculacion.cshtml`.

## 4. Trazabilidad

| Criterio | Dónde se cumple |
|---|---|
| 1 | `DiagnosticoController` (`IsDevelopment`) |
| 2 | `[Authorize]` (req002) |
| 3, 4, 5 | `ComparadorVinculacion.Comparar` |
| 6 | `InformeVinculacion.TodoVinculado` |
| 7 | `VerificacionVinculacionService` + `InformeVinculacion.SinSimulador` |

## 5. Preguntas abiertas
* `ComparadorVinculacion` es pura pero **no tiene tests**: conviene agregarlos (un caso por cada error y advertencia).
