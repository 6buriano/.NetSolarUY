# Requerimiento 003: ABM de Nodos (con inversor y sensor ARM)

> **Documentado después de implementar.** El código ya estaba en `main` (iteración 1). Este documento describe lo que hace hoy, para que el agente y el equipo trabajen sobre una especificación y no sobre el código suelto.

## Descripción
Como **Administrador**, quiero registrar, consultar, modificar y eliminar los **nodos** de cada parque solar. Cada nodo tiene **un inversor** (obligatorio) y, opcionalmente, **un sensor ARM** conectado al inversor, que es el que después reporta las lecturas de los paneles.

## Criterios de Aceptación
1. Se listan los nodos, con filtro opcional por parque.
2. El detalle de un nodo muestra su parque, el inversor, el sensor (si tiene) y sus paneles.
3. Para crear un nodo son obligatorios: nombre, ubicación dentro del parque, parque y los datos del inversor (modelo, número de serie, eficiencia de conversión).
4. El parque elegido tiene que existir.
5. La eficiencia del inversor tiene que ser mayor que 0 y como máximo 100.
6. El **número de serie del inversor** no se repite en todo el sistema (tampoco al editar).
7. El sensor ARM es **opcional**. Si se carga el identificador de hardware, la URL base es obligatoria y tiene que ser una dirección `http` o `https` absoluta.
8. El **identificador de hardware del sensor** no se repite en todo el sistema.
9. **Sensor inexistente (solo desarrollo, contra el simulador):** si el identificador no existe en el simulador, no se crea nada y se le muestra al usuario la lista de sensores que sí existen, con dos salidas:
   * **Crear el sensor en el simulador** y seguir con el alta (se avisa qué códigos de panel informa el sensor, para cargarlos);
   * **Crear el nodo sin sensor.**
   Si el simulador no responde, el nodo se crea igual y se avisa que no se pudo verificar.
10. Al editar se puede agregar, cambiar o quitar el sensor.
11. **No se puede eliminar un nodo que tiene paneles.** Se muestra un mensaje explicando por qué.
12. Cada operación exitosa muestra un mensaje de confirmación.
13. Solo el rol `Administrador` accede a estas pantallas (resuelto en `req002`).
