# Requerimiento 006: Verificación de vinculación con el simulador

> **Documentado después de implementar.** El código ya estaba en `main` (iteración 1). Es una herramienta de **desarrollo**, no una pantalla del producto.

## Descripción
Como **desarrollador**, quiero una pantalla que compare lo que está cargado en el sistema (nodos, sensores, paneles) con lo que emula el simulador de sensores, y me liste todo lo que no cierra, para entender por qué un panel no recibe lecturas.

## Criterios de Aceptación
1. La pantalla existe **solo en el entorno `Development`**. En cualquier otro entorno responde 404.
2. Solo la ve el rol `Administrador` (`req002`).
3. Marca como **error** (el panel nunca va a recibir lecturas):
   * un nodo sin sensor ARM;
   * un sensor que el simulador no emula (y lista los que sí existen);
   * un panel cuyo código no existe en el sensor (y lista los códigos que informa el sensor).
4. Marca como **advertencia**:
   * un sensor desactivado en el sistema (el colector no lo consulta);
   * un panel con potencia nominal distinta a la del simulador (distorsiona la comparación entre paneles de `req005`);
   * un panel que informa el sensor y que el sistema no tiene cargado (esas lecturas se descartan).
5. Lista también los sensores del simulador que no usa ningún nodo.
6. Si todo coincide, lo dice ("todo vinculado").
7. Si el simulador no responde, la pantalla lo avisa con el comando para levantarlo, en lugar de fallar.
