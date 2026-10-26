---
trigger: always_on
---

# Principios SOLID y Buenas Prácticas

El agente debe revisar activamente el código que genera o analiza para que cumpla estos principios, con especial énfasis en el Principio de Responsabilidad Única (SRP).

## Los cinco principios en SolarUY

* **Single Responsibility Principle (SRP):** Cada controlador MVC solo maneja peticiones HTTP y orquestación UI. La lógica de cálculo de energía y estado de salud reside exclusivamente en las entidades de `Domain` o servicios de `Application`.
* **Open/Closed Principle (OCP):** Se agrega comportamiento nuevo con clases nuevas detrás de una interfaz, sin modificar las existentes. Ejemplo real: para usar hardware real en lugar del simulador se escribe otra implementación de `ISensorClient`; el colector no cambia.
* **Liskov Substitution Principle (LSP):** Las implementaciones de una interfaz son intercambiables sin que quien las usa se entere. Ejemplo real: `ISensorDisponibilidadService` tiene `SensorDisponibilidadSimulador` (desarrollo) y `SensorDisponibilidadNula` (producción), y `NodosController` funciona igual con las dos.
* **Interface Segregation Principle (ISP):** Interfaces pequeñas y enfocadas en lugar de interfaces monolíticas. Ejemplo real: `ISensorClient` (lecturas y órdenes, existe con hardware real) está separada de `ISimuladorAdminClient` (inventario, solo existe en el mock). Si una interfaz de repositorio crece demasiado, se divide en lectura y escritura (ej. `IParqueReadRepository`).
* **Dependency Inversion Principle (DIP):** Las capas de nivel superior (`Application`) dependen de abstracciones (`IParqueRepository`), no de implementaciones concretas de MySQL.

## SRP a nivel de método

* **Una sola tarea:** Un método hace exactamente lo que su nombre indica, sin efectos secundarios ocultos.
* **Detección de violaciones:** El agente marca como incorrecto un método que combine operaciones de negocio distintas.
  * *Ejemplo de lo que NO se debe hacer:* `CrearPanel(PanelDto dto)` que, si el nodo indicado no existe, lo crea antes de agregarle el panel. Crear un nodo y crear un panel son dos responsabilidades distintas.
  * *Solución esperada:* el caso de uso obtiene el nodo (y falla si no existe) y recién después agrega el panel.
* **Nombres con "Y":** Si un nombre usa "Y" o "And" (ej. `CrearNodoYPanel`), el agente advierte que probablemente se esté violando SRP y sugiere separarlo. Si el método hace dos cosas justificadas, el nombre debe reflejarlo (ej. `ObtenerOCrearSensor`).

## Tamaño y complejidad

* Los métodos deben poder leerse sin hacer scroll (como referencia, unas 30 líneas).
* Si un método necesita varios `if/else` para tareas no relacionadas con su propósito, el agente sugiere extraer esa lógica a métodos privados o clases especializadas.

## Simplicidad

* Los principios se aplican para que el código sea más fácil de entender, no más complejo. El agente **no** crea interfaces, capas ni patrones que el requerimiento no necesita "por si acaso".
