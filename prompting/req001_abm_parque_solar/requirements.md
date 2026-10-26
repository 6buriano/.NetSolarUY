# Requerimientos: Gestión de Parques Solares (ABM)

> Responsable del módulo: Guillermina Rodríguez.

## Descripción General
Como Administrador del sistema SolarUY, quiero poder registrar, consultar, modificar y eliminar Parques Solares en la plataforma para poder gestionar de forma independiente sus nodos, paneles e inversores.

## Criterios de Aceptación

### Listado
1. El sistema muestra la lista de parques registrados con su nombre, ubicación, capacidad instalada y fecha de instalación.
2. Desde cada fila se puede acceder al detalle, a la edición y a la eliminación del parque.
3. Si no hay parques registrados, se muestra un mensaje que lo indica.

### Alta
4. El sistema permite ingresar los siguientes datos del parque:
   - Nombre (obligatorio, máximo 150 caracteres, único en el sistema).
   - Ubicación (obligatorio, máximo 200 caracteres).
   - Capacidad instalada en kW (obligatorio, mayor a 0).
   - Fecha de instalación (obligatorio, no puede ser futura).
5. El identificador del parque lo genera el sistema; el usuario no lo ingresa.
6. Al crear el parque, este se registra sin nodos ni paneles asignados.
7. Si un dato es inválido, se vuelve a mostrar el formulario con lo que el usuario había ingresado y un mensaje explicando el error.

### Detalle
8. El sistema muestra todos los datos del parque y la lista de sus nodos.

### Modificación
9. Se pueden modificar todos los datos ingresados en el alta, con las mismas validaciones.
10. El identificador no se puede modificar.

### Eliminación
11. Antes de eliminar, el sistema pide confirmación mostrando los datos del parque.
12. Un parque que tiene nodos no se puede eliminar; el sistema muestra un mensaje explicando el motivo.

### Seguridad
13. Solo los usuarios autenticados con el rol "Administrador" pueden acceder a cualquiera de estas pantallas. *(Implementado en `req002_autenticacion_keycloak`.)*

## Reglas de Negocio
- No pueden existir dos parques solares con el mismo nombre.
- El alta de un parque es el paso previo y obligatorio antes de poder configurar su topología (nodos, inversores y paneles).

## Fuera de Alcance
- Gestión de nodos y paneles (requerimientos propios).
- Cálculo de la generación actual del parque (módulo de Monitoreo).
