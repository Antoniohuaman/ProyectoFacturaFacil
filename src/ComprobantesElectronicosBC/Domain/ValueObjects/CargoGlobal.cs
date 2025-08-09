/*
CargoGlobal (Value Object) — propósito y uso

Representa “otros cargos” a nivel de comprobante (p. ej., flete, embalaje, comisión, recargo por financiamiento).
Es un VO porque no tiene identidad propia: solo importan sus valores (motivo y monto/porcentaje), y sus reglas.

Impacto en totales:
- Aumenta ChargeTotalAmount del documento.
- Si el cargo es gravado con IGV (10%/18%), incrementa base imponible e IGV.
- Si es exonerado/inafecto, no genera IGV; solo incrementa el total.

Mapeo UBL/CPE:
- Se expresa con cac:AllowanceCharge donde cbc:ChargeIndicator = true.
- Se consignan cbc:Amount (o BaseAmount + MultiplierFactorNumeric si es %),
  cbc:AllowanceChargeReason/ReasonCode, y cac:TaxCategory si aplica IGV.

Notas:
- No es obligatorio para emitir la factura/boleta mínima; úsalo cuando el formulario incluya “Otros cargos”.
- Campos típicos a modelar cuando se implemente: CodigoMotivo/Descripcion, Monto o Porcentaje+Base,
  y el VO ImpuestoIGV (10%/18%/Exo/Ina) para su tratamiento tributario.
*/
