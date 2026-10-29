# Requerimiento 010: Consultas históricas de generación

> **Documentado después de implementar.** El código ya estaba en `main` (commit «Consultas históricas»).

## Descripción
Como **responsable del parque**, quiero consultar la generación histórica de un panel agrupada por día, mes o año, con un rango de fechas opcional, para ver cómo evolucionó su generación y detectar períodos flojos sin revisar las lecturas una por una.

## Criterios de Aceptación
1. La pantalla **Histórico** permite elegir un panel y una granularidad de agrupación: **día**, **mes** o **año**. Por defecto, día.
2. El rango de fechas es **opcional**: se puede acotar con «desde», «hasta» o ambos. Sin fechas, se consideran todas las lecturas del panel.
3. El rango **incluye todo el día de «hasta»**: si se elige el 20/10/2026, entran las lecturas hasta las 23:59:59.9999999 de ese día. «Desde» entra desde la medianoche del día elegido.
4. Si el usuario no eligió panel, se propone el **primero** de la lista y se muestra su histórico.
5. La tabla muestra, por período: la **etiqueta del período**, la cantidad de lecturas, la potencia promedio (W), la potencia máxima (W) y la **energía estimada (kWh)**.
6. La etiqueta del período depende de la granularidad: `dd/MM/yyyy` para día, `mes en español y año` para mes («octubre 2026») y el año para año («2026»). Las filas salen del período más reciente al más antiguo.
7. La **energía es una estimación, no una medición**: se calcula sumando la potencia instantánea que guarda el colector en cada lectura y multiplicando por el intervalo entre lecturas (**5 s** por defecto, `Colector:IntervaloSegundos`). El sistema no mide energía; la aproxima. La pantalla lo aclara.
8. Solo se muestran los períodos **con lecturas**; no se rellenan los períodos sin datos, ni con ceros.
9. La tabla cierra con una fila **Total** con la suma de lecturas y la suma de la energía estimada de las filas mostradas.
10. La agregación se hace en la base: no se traen todas las lecturas a memoria para sumarlas en la aplicación.
11. Si el panel no tiene lecturas con esos filtros, se muestra un aviso y la pantalla no falla. Si todavía no hay paneles cargados, también hay aviso.
12. La consulta la puede hacer solo el rol **Administrador**, y la pantalla está en el menú principal como «Histórico».
