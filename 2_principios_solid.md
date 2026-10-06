# Principios SOLID y Buenas Prácticas

- **Single Responsibility Principle (SRP):** Cada controlador MVC solo maneja peticiones HTTP y orquestación UI. La lógica de cálculo de energía y estado de salud reside exclusivamente en las entidades de `Domain` o servicios de `Application`.
- **Open/Closed Principle (OCP):** Los algoritmos de cálculo de eficiencia telemétrica y comandos del inversor se extienden mediante interfaces (`ISensorCommand`) sin modificar la clase base del inversor.
- **Liskov Substitution Principle (LSP):** Cualquier implementación de un simulador de inversor (ej. `MockInversorService` o `RealInversorService`) debe ser intercambiable sin alterar el comportamiento de la aplicación.
- **Interface Segregation Principle (ISP):** Interfaces pequeñas y enfocadas (ej. `IParqueReadRepository`, `ISignalRNotifier`, `ICommandExecutor`) en lugar de interfaces monolíticas.
- **Dependency Inversion Principle (DIP):** Las capas de nivel superior (`Application`) dependen de abstracciones (`IParqueRepository`), no de implementaciones concretas de MySQL.
