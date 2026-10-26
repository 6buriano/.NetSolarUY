# Tareas de Implementación: Gestión de Parques Solares (ABM)

> Primera versión. Las tareas marcadas ya estaban hechas en el código; las pendientes surgen de la trazabilidad de `specs.md`.

- [x] **1. Capa Domain**
  - [x] 1.1 Entidad `ParqueSolar` con propiedades de solo lectura desde afuera (`private set`).
  - [x] 1.2 Constructor que genera el `Id` y valida nombre, ubicación y capacidad.
  - [x] 1.3 Métodos `Actualizar` y `TieneNodos`.
  - [x] 1.4 Interfaz `IParqueRepository`.
  - [x] 1.5 Pruebas unitarias (`ParqueSolarTests`).
  - [ ] 1.6 Validar que la fecha de instalación no sea futura, con su prueba unitaria.

- [x] **2. Capa Infrastructure**
  - [x] 2.1 `ParqueSolarConfiguration` (tabla, longitudes, relación con nodos).
  - [x] 2.2 `ParqueRepository`.
  - [x] 2.3 Migración inicial.
  - [ ] 2.4 Método `ExisteConNombreAsync` en `IParqueRepository` y `ParqueRepository`.
  - [ ] 2.5 Índice único en `Nombre` y nueva migración.

- [x] **3. Capa Application**
  - [x] 3.1 DTOs `CrearParqueRequest`, `ActualizarParqueRequest` y `ParqueDto`.
  - [x] 3.2 `ParqueMappings`.
  - [x] 3.3 `ParqueService` (listar, obtener, crear, actualizar, eliminar).
  - [ ] 3.4 Controlar en `CrearAsync` y `ActualizarAsync` que el nombre no exista.

- [x] **4. Registro de dependencias**
  - [x] 4.1 `AddInfrastructure` y `ParqueService` en `Program.cs`.

- [x] **5. Capa Presentation**
  - [x] 5.1 `ParqueFormViewModel` con validaciones de formato.
  - [x] 5.2 `ParquesController` con las cinco acciones.
  - [x] 5.3 Vistas `Index`, `Detalle`, `Crear`, `Editar`, `Eliminar` y `_Formulario`.
  - [x] 5.4 Enlace "Parques" en el menú.
  - [x] 5.5 `[Authorize(Roles = "Administrador")]` (hecho en `req002_autenticacion_keycloak`).

- [ ] **6. Verificación de Calidad**
  - [x] 6.1 `dotnet build` sin errores.
  - [ ] 6.2 `dotnet test` sobre `CodeQuality.Tests` en verde.
  - [x] 6.3 Pruebas unitarias del dominio en verde.
  - [ ] 6.4 Probar a mano cada criterio de aceptación de `requirements.md`.
  - [ ] 6.5 Análisis con SonarQube.

## Uso de IA
- **Código del ABM:** generado con asistencia de IA por el equipo antes de adoptar este flujo.
- **Este requerimiento, `specs.md` y `tasks.md`:** armados con asistencia de IA a partir del código existente, y revisados por la responsable del módulo.
- **Problemas detectados en la revisión:** faltaban dos criterios de aceptación (nombre único y fecha no futura); quedan como tareas pendientes.
