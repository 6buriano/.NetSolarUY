# Requerimiento 013: Cierre de las tareas de mantenimiento

> **Documentado antes de implementar**, a partir de la devolución del docente de la segunda entrega.

## Descripción

Como **responsable de mantenimiento**, quiero que al completar una tarea pueda anotar **qué trabajo se hizo** y registrar la **fecha y hora reales** en que terminó, para que el historial del panel guarde algo más que «se completó» y para que la fecha de finalización coincida con la del trabajo y no con la del momento en que alguien la marcó en el sistema.

## Criterios de Aceptación

1. Al completar una tarea `EN_PROCESO` se pide la **fecha y hora de realización**, propuesta con el momento actual en hora de Montevideo.
2. La fecha de realización es **obligatoria** y **no puede ser posterior al momento actual**.
3. La fecha que se guarda es la que informó el usuario, no la del servidor: si la tarea se marca dos horas después de terminada, la hora guardada sigue siendo la real.
4. Se puede anotar, **opcionalmente**, una **observación de cierre** de hasta 500 caracteres. Se guarda sin espacios al principio ni al final; si queda vacía, se guarda nula.
5. La fecha de realización y las observaciones **sólo se guardan al completar**: una tarea `PENDIENTE`, `EN_PROCESO`, `CANCELADO` no las tiene.
6. Una tarea completada **no se puede volver a completar** ni reprogramar ni cancelar (las reglas del req009 siguen valiendo).
7. El **detalle de la tarea** muestra la fecha de realización en hora de Montevideo y las observaciones del cierre cuando existen.
8. El listado, el historial del panel y el resto de las pantallas no cambian.
