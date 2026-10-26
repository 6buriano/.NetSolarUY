# Especificación Técnica: ABM de Nodos

> Documentado después de implementar, a partir del código de `main`.

## 1. Domain (`SolarUY.Domain`)
* **`Nodo`**: `Nombre`, `UbicacionDentroParque`, `ParqueSolarId`, `Inversor`, `Paneles`. El constructor público **exige un `Inversor`** y lo asocia (`Inversor.AsignarANodo`), así un nodo nunca existe sin inversor (regla 1 de `4_modelado_dominio.md`).
  * `Actualizar(nombre, ubicacion)`, `TienePaneles()`, `BuscarPanelPorCodigo(codigo)` (sin distinguir mayúsculas).
* **`Inversor`**: valida `0 < EficienciaConversion <= 100`. `RegistrarSensor(...)`, `QuitarSensor()`, `EstablecerVolcadoRed(...)`.
* **`SensorDispositivo`**: `IdentificadorHardwareARM`, `VersionFirmware`, `UrlBase` (validada: URL absoluta `http/https`), `Activo`.
* **`INodoRepository`**: `ObtenerTodosAsync`, `ObtenerPorParqueAsync`, `ObtenerPorIdAsync`, `ExisteNumeroSerieInversorAsync(numeroSerie, nodoExcluidoId)`, `ExisteIdentificadorHardwareAsync(identificador, nodoExcluidoId)`, `AgregarAsync`, `ActualizarAsync(nodo, sensorNuevo)`, `EliminarAsync`.
  * El parámetro `nodoExcluidoId` permite que, al editar, el propio nodo no cuente como repetido.

## 2. Application (`SolarUY.Application`)
* **`NodoService`** (depende de `INodoRepository` e `IParqueRepository`):
  * `CrearAsync(CrearNodoRequest)`: valida que el parque exista, que el número de serie y el identificador ARM no estén usados, arma `Inversor` → `Nodo` y opcionalmente el sensor.
  * `ActualizarAsync(...)`: mismas validaciones excluyendo el nodo; agrega, reemplaza o quita el sensor.
  * `EliminarAsync(id)`: si `nodo.TienePaneles()` lanza `ReglaDeNegocioException`.
* **DTOs**: `NodoDto`, `InversorDto`, `SensorDispositivoDto`, `CrearNodoRequest`, `ActualizarNodoRequest`, `InversorRequest`, `SensorDispositivoRequest`.
* **`ISensorDisponibilidadService`** (`Sensors/`): `VerificarAsync(identificador)` → `DisponibilidadSensor(VerificacionPosible, SePudoConsultar, Existe, IdentificadoresExistentes)`; `CrearAsync(identificador)` → `ResultadoAltaSensor`.
  * Implementaciones: la del **simulador** (solo `Development`) y **`SensorDisponibilidadNula`** (en otros entornos: no verifica nada). Es un ejemplo de LSP/OCP de `2_principios_solid.md`.

## 3. Infrastructure (`SolarUY.Infrastructure`)
* `NodoRepository` (EF Core).
* Índice **único** en `inversores.numero_serie`; borrado en cascada nodo → inversor → sensor.
* ⚠️ El identificador ARM del sensor **no tiene índice único en la base**: la unicidad solo la controla `NodoService`. Pendiente de decisión: agregar el índice (ver `tasks.md`, 6.2).
* Cliente HTTP del simulador (`ISimuladorAdminClient`) para verificar y crear sensores.

## 4. Web (`SolarUY.Web`)
* **`NodosController`** (`[Authorize(Roles = "Administrador")]`): `Index(parqueId)`, `Detalle`, `Crear` (GET/POST), `Editar` (GET/POST), `Eliminar` / `EliminarConfirmado`.
  * En el POST de `Crear`: si hay sensor, llama a `VerificarAsync`; si no existe, `ResolverSensorInexistenteAsync` decide según `AccionArm` (`CrearEnHardware`, `SinSensor` o volver a mostrar el formulario con el aviso).
  * Las `ReglaDeNegocioException` se muestran en `ModelState` (alta/edición) o en `TempData["MensajeError"]` (baja).
* **`NodoFormViewModel`** (`IValidatableObject`): DataAnnotations de formato y la regla "si hay identificador, la URL es obligatoria".
* Vistas: `Index`, `Detalle`, `Crear`, `Editar`, `Eliminar` y el parcial `_Formulario.cshtml`.
* `Program.cs`: elige la implementación de `ISensorDisponibilidadService` según el entorno.

## 5. Trazabilidad

| Criterio | Dónde se cumple |
|---|---|
| 1, 2 | `NodosController.Index/Detalle`, `NodoService` |
| 3 | `NodoFormViewModel` (formato) + constructor de `Nodo` (inversor obligatorio) |
| 4 | `NodoService` con `IParqueRepository` |
| 5 | `Inversor` (constructor) |
| 6 | `NodoService` + `ExisteNumeroSerieInversorAsync` + índice único |
| 7 | `NodoFormViewModel.Validate` + `SensorDispositivo` (`UrlBase`) |
| 8 | `NodoService` + `ExisteIdentificadorHardwareAsync` (sin índice en la base) |
| 9 | `NodosController.ResolverSensorInexistenteAsync` + `ISensorDisponibilidadService` |
| 10 | `NodoService.ActualizarAsync` + `INodoRepository.ActualizarAsync(nodo, sensorNuevo)` |
| 11 | `Nodo.TienePaneles()` + `NodoService.EliminarAsync` |
| 12 | `TempData["MensajeExito"]` |
| 13 | `[Authorize]` (req002) |

## 6. Tests existentes
* `tests/SolarUY.Domain.Tests/Entities/NodoTests.cs`, `InversorTests.cs`, `SensorDispositivoTests.cs`.
