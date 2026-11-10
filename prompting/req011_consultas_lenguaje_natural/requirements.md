# Requerimiento 011: Consultas en lenguaje natural

## Descripción
Como **administrador**, quiero hacerle preguntas en lenguaje natural al sistema sobre el estado y la generación de energía (por ejemplo, «¿cómo está funcionando todo hoy?»), para conocer la situación de los parques sin recorrer cada pantalla. La letra pide el lenguaje natural **solo para consultar**.

## Criterios de Aceptación
1. La pantalla **Consultas**, en el menú principal, tiene un campo de texto y un botón **Enviar**. Muestra la pregunta y, debajo, la respuesta en español. Solo la puede usar el rol **Administrador**.
2. Cada envío es **independiente**: una pregunta, una respuesta, sin historial.
3. Se puede preguntar por el **estado general de un parque**, la **generación de hoy**, la **generación histórica** (por día, mes o año, con rango de fechas) y los **paneles en falla**. La energía histórica aclara que es una estimación.
4. Las respuestas salen de **datos reales** del sistema. Si no hay datos para lo preguntado, la respuesta lo dice.
5. Es **solo de lectura**: si se pide crear, modificar, borrar o mandar una orden al hardware, el sistema responde que solo puede consultar y no ejecuta nada. Lo mismo para preguntas ajenas al sistema.
6. Una pregunta vacía no se envía. Si el servicio de IA falla, se muestra un mensaje y la pantalla no se rompe.
7. La clave del servicio de IA no se versiona: va en *user secrets* y en variables de entorno.

## Fuera de alcance
Historial de conversación, acciones que cambien datos, respuestas en tiempo real y voz.
