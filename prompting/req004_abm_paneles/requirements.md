# Requerimiento 004: ABM de Paneles Solares

> **Documentado después de implementar.** El código ya estaba en `main` (iteración 1).

## Descripción
Como **Administrador**, quiero registrar, consultar, modificar y eliminar los **paneles solares** de cada nodo, y ver sus últimas lecturas, para saber qué paneles hay instalados y en qué estado están.

## Criterios de Aceptación
1. Se listan los paneles, con filtro opcional por nodo y por estado de salud.
2. El detalle de un panel muestra sus datos y sus **últimas 20 lecturas** de telemetría.
3. Para crear un panel son obligatorios: código, modelo, fabricante, potencia nominal (W), fecha de instalación, estado y nodo.
4. El **código de panel** tiene como máximo 20 caracteres y solo letras, números, `-` y `_`. Es lo que vincula el panel con el panel que informa el sensor.
5. El código de panel **no se repite dentro del mismo nodo** (sí puede repetirse en nodos distintos).
6. La potencia nominal tiene que ser mayor que 0.
7. El nodo elegido tiene que existir.
8. El estado se elige de la lista de estados (`OPERATIVO`, `RENDIMIENTO_BAJO`, `FALLA_DETECTADA`, `DESCONECTADO`) y se puede cambiar a mano al editar. Después, el colector lo actualiza con cada lectura (`req005`).
9. Valores por defecto del alta: 550 W y `OPERATIVO`.
10. Al eliminar un panel se eliminan también sus lecturas y sus tareas de mantenimiento.
11. Cada operación exitosa muestra un mensaje de confirmación.
12. Solo el rol `Administrador` accede a estas pantallas (resuelto en `req002`).
