/*
===============================================================================
TipoDeCambio — Documentación funcional y de dominio (ComprobantesElectronicosBC)
===============================================================================

1) Concepto y propósito
- Representa el tipo de cambio aplicado al comprobante (Factura/Boleta) cuando
  la moneda de la operación no es la moneda base de la empresa.
- Sirve para convertir importes entre monedas durante el cálculo de totales,
  impuestos y presentación (PDF), así como para registros contables/analíticos.

2) Alcance dentro del BC
- Se usa en la construcción/validación del comprobante.
- No resuelve la obtención externa del tipo de cambio (eso lo hace un adaptador
  o servicio de consulta). Este VO solo encapsula el valor y su intención de uso.
- No envía ni firma documentos; solo aporta datos al payload JSON del comprobante.

3) Origen del dato y edición por el usuario
- Origen primario: API de consulta (p.ej., SBS/SUNAT u otra fuente configurada).
- El usuario puede **reemplazar** manualmente el tipo de cambio (ej.: SBS 3.54,
  usuario ingresa 3.60) para cerrar la venta con un valor distinto.
- Debe registrarse la **fuente** (SBS/SUNAT/Manual) y la **fecha** del tipo de
  cambio utilizado, más auditoría (quién/ cuándo modificó).

4) Moneda base, sentido y política
- Definir explícitamente el sentido: valor expresado como **PEN por 1 unidad de
  moneda extranjera** (p.ej., 3.600000 PEN por 1 USD). Esto evita ambigüedades.
- La política (compra/venta/promedio) es configurable a nivel de empresa en
  ConfiguracionSistemaBC. Por defecto, usar la política de “venta” para conversión
  de montos de ventas hacia PEN, si así lo define la empresa.

5) Fecha de referencia
- Por defecto, el tipo de cambio corresponde a la **fecha de emisión** del
  comprobante. Si la empresa define otra política (p.ej., día hábil anterior),
  debe respetarse y quedar trazada.

6) Presentación (PDF) y flag de visibilidad
- El usuario puede activar/desactivar “Mostrar en PDF” (flag). Si está activo,
  sugerir formato: “Tipo de cambio: 3.600000 (fuente: SBS, 2025-08-09)”.
- La visibilidad en PDF **no** afecta los cálculos; solo la representación.

7) Redondeo y escala
- Recomendado almacenar el tipo de cambio con **6 decimales** para consistencia
  de cálculos; las cifras monetarias se redondean según la moneda (típicamente 2).
- Usar una única regla de redondeo en todo el dominio (p.ej., AwayFromZero).

8) Reglas de validación (invariantes)
- Valor **> 0** y dentro de un rango razonable (defensivo) para evitar outliers.
- Requiere moneda origen/destino y fecha del tipo de cambio cuando aplique.
- Si el usuario edita el valor, registrar “fuente = Manual” + auditoría.

9) Consideraciones normativas (resumen)
- En comprobantes electrónicos con moneda distinta a la base, los importes y
  tributos deben ser consistentes con el tipo de cambio utilizado al momento de
  la emisión, manteniendo trazabilidad de la fuente y la fecha.
*/