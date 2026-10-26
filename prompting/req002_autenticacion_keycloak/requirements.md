# Requerimientos: Inicio de sesión y roles con Keycloak

> Responsable del módulo: Guillermina Rodríguez.

## Descripción General
Como Administrador del sistema SolarUY, quiero iniciar sesión con un proveedor de identidad externo para que solo los usuarios autorizados puedan gestionar parques, nodos y paneles, sin que el sistema tenga que guardar usuarios ni contraseñas.

## Criterios de Aceptación

### Inicio de sesión
1. El inicio de sesión lo hace Keycloak (OpenID Connect). SolarUY no tiene pantalla de login propia ni guarda contraseñas.
2. Si un usuario sin sesión entra a una pantalla protegida, el sistema lo manda a la página de login de Keycloak.
3. Después de iniciar sesión, el usuario vuelve a la pantalla a la que quería entrar.
4. El menú muestra "Iniciar sesión" si no hay sesión, y "Hola, *usuario*" con la opción "Cerrar sesión" si la hay.
5. Mientras la sesión esté vigente, el sistema no vuelve a pedir el login en cada pantalla.

### Cierre de sesión
6. "Cerrar sesión" termina la sesión en SolarUY **y** en Keycloak. Al volver a iniciar sesión, Keycloak pide la contraseña de nuevo.
7. Cerrar sesión solo se puede hacer con una acción explícita del usuario (un botón), no con un enlace que otra página pueda disparar.

### Roles
8. El ABM de Parques, Nodos y Paneles y la pantalla de Diagnóstico solo son accesibles para el rol `Administrador`.
9. Un usuario con sesión pero sin ese rol ve una página "No tenés permiso", con su nombre de usuario y un enlace para volver al inicio.
10. La página de inicio y el chequeo de salud (`/health`) son accesibles sin iniciar sesión.
11. Los roles se administran en Keycloak, no en SolarUY.

### Configuración y seguridad
12. El secreto del cliente (client secret) nunca se guarda en el repositorio.
13. La dirección de Keycloak y el identificador del cliente se configuran sin tocar el código.
14. Solo se acepta volver a direcciones de SolarUY después del login (no se puede usar para redirigir a un sitio externo).

### Alta disponibilidad
15. El inicio de sesión funciona entrando por el balanceador (nginx), no solo con la web corriendo directo.
16. Si la réplica que atiende se cae, el usuario sigue con la sesión iniciada en la otra réplica.

## Reglas de Negocio
- El rol `Administrador` puede gestionar toda la topología (parques, nodos y paneles).
- El rol `Consulta` existe en Keycloak para los usuarios que solo ven información. En esta iteración no tiene pantallas propias: las va a tener con el módulo de Monitoreo.

## Fuera de Alcance
- Registro de usuarios desde SolarUY (los usuarios se crean en Keycloak).
- Recuperación de contraseña (la ofrece Keycloak).
- Permisos más finos dentro del ABM (por ejemplo, que `Consulta` vea el listado pero no pueda editar).
