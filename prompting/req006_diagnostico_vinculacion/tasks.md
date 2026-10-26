# Tareas de Implementación: Verificación de vinculación

> Documentado después de implementar: las tareas reflejan lo que ya está en `main`.

- [x] **1. Application**
  - [x] 1.1 `ISimuladorAdminClient` e `InventarioSensorDto`.
  - [x] 1.2 `InformeVinculacion`.
  - [x] 1.3 `ComparadorVinculacion` (errores, advertencias y sensores libres).
  - [x] 1.4 `VerificacionVinculacionService`.

- [x] **2. Infrastructure**
  - [x] 2.1 Cliente HTTP del simulador y registro en `AddInfrastructure`.

- [x] **3. Web**
  - [x] 3.1 `DiagnosticoController` solo en `Development`.
  - [x] 3.2 Vista `Vinculacion.cshtml`.
  - [x] 3.3 `[Authorize(Roles = "Administrador")]` (hecho en `req002`).

- [ ] **4. Pendiente**
  - [ ] 4.1 Tests de `ComparadorVinculacion`.
  - [ ] 4.2 Pasar `verificar-calidad` (ArchUnit + SonarQube) cuando estén integrados.

## Uso de IA
- **Código:** implementado en la iteración 1 con asistencia de IA (Antigravity), sin documentos SDD previos.
- **Este requerimiento, `specs.md` y `tasks.md`:** reconstruidos con asistencia de IA leyendo el código de `main`, y revisados por el equipo. *(Completar: quién lo revisó.)*
