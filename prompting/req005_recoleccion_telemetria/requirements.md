# Requerimiento 005: Recolección de telemetría y diagnóstico de paneles

> **Documentado después de implementar.** El código ya estaba en `main` (iteración 1).

## Descripción
Como **operador de la planta**, quiero que el sistema consulte solo, cada pocos segundos, a los sensores ARM de los nodos, guarde las lecturas de cada panel y **decida el estado de salud** de cada uno, para detectar fallas sin revisar los paneles a mano.

## Criterios de Aceptación
1. Un proceso aparte (`SolarUY.Collector`) consulta a todos los nodos que tienen **sensor activo**, cada `Colector:IntervaloSegundos` (5 por defecto). Se puede apagar con `Colector:Habilitado = false`.
2. Cada lectura del sensor se empareja con un panel del nodo por **código de panel** (sin distinguir mayúsculas). Las lecturas de códigos que no están cargados se ignoran.
3. Se guarda una lectura por panel con: fecha y hora, corriente continua, voltaje, potencia generada, temperatura y estado detectado.
4. **El sensor mide, el dominio decide** el estado de cada panel:
   * panel inactivo → `DESCONECTADO`;
   * temperatura mayor a 75 °C → `FALLA_DETECTADA`;
   * si no, se compara su rendimiento (potencia / potencia nominal) con el **promedio de los otros paneles activos del nodo**: caída hasta 15 % → `OPERATIVO`; más de 15 % y hasta 40 % → `RENDIMIENTO_BAJO`; más de 40 % → `FALLA_DETECTADA`;
   * si el promedio de los otros es menor al 5 % del nominal (de noche) o no hay otros paneles con lectura, solo se evalúa la temperatura.
5. El estado del panel se actualiza con el resultado, y el estado del volcado a la red del inversor se toma del sensor.
6. Si un sensor no responde, se saltea en esa ronda y los demás siguen.
7. Si falla una ronda entera (por ejemplo, MySQL no responde), se registra el error y el colector sigue en la próxima ronda.
8. Corre **una sola instancia** del colector (dos duplicarían las lecturas).
