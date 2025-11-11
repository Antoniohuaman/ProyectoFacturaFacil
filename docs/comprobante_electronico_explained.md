# Diagrama y explicación — Aggregate ComprobanteElectronico

Este documento acompaña al diagrama Mermaid `docs/comprobante_electronico.mmd` y explica responsabilidades, invariantes, métodos clave, eventos y flujo de negocio encontrados en `ComprobanteElectronico.cs`.

## Resumen del aggregate
`ComprobanteElectronico` es el aggregate raíz del Bounded Context `ComprobantesElectronicosBC`. Agrupa la cabecera del comprobante, la colección de `ComprobanteLinea` y las reglas de negocio que gobiernan su ciclo (borrador → enviado → aceptado/rechazado → anulado).

## Responsabilidades principales
- Mantener consistencia de las líneas y totales.
- Aplicar reglas fiscales y operativas antes de transicionar estados (por ejemplo: validar RUC, exigir TipoCambio, límites de montos).
- Exponer comandos de modificación (AgregarLinea, EditarLinea, EliminarLinea, CambiarMoneda, CambiarCliente, CambiarFormaDePago, etc.).
- Publicar eventos de dominio cuando ocurren transiciones relevantes (`ComprobanteEnviado`, `ComprobanteAceptado`, `ComprobanteRechazado`, `ComprobanteAnulado`, `ComprobanteObservado`).

## Invariantes y reglas clave (extraídas del código)
- Solo los estados `Borrador` y `Corregir` permiten modificaciones (EnsureEditable).
- Para emitir: `SerieNumero` debe existir y el documento debe tener al menos una línea.
- Para Factura (tipo 01): el `Cliente` debe tener `RUC`.
- Si la moneda del comprobante no es la local (ej. PEN), `TipoCambio` es obligatorio al emitir.
- Para Boleta (tipo 03) en PEN, si `Total > 700` entonces `Cliente.Documento.EsDni` debe ser true.
- El total no puede ser negativo; existe un tope operativo en `RecalcularTotales` (1_000_000). Si se viola, se lanza `ReglaDeNegocioException`.
- Al eliminar líneas, se reenumera para mantener secuencia 1..N.

## Entidades y ValueObjects relevantes
- Entidad: `ComprobanteElectronico` (aggregate root).
- Entidad: `ComprobanteLinea` (pertenece al aggregate, tiene métodos de recálculo y ajuste de precios/cantidad).
- Value Objects: `ImporteMonetario`, `Moneda`, `TipoCambio`, `FechaEmision`, `FechaVencimiento`, `SerieYNumero`, `ClienteSnapshot`, `EmisorSnapshot`, `DescuentoGlobal`, `DescuentoLinea`, `AfectacionImpuesto`, `TasaImpuesto`, `UnidadDeMedida`, `Email`, `NotaInterna`.

## Servicios de dominio y utilidades
- `ComprobanteTotalesService` — centraliza la lógica de cálculo de subtotales, IGV, descuento global y total.
- `MonedaConversionService` — usado en `CambiarMoneda` para convertir precios de línea cuando se cambia la moneda del comprobante.

## Eventos de dominio publicados
- `ComprobanteEnviadoDomainEvent` — publicado en `Emitir()`; incluye identificadores multi-tenant y fecha de envío.
- `ComprobanteAceptadoDomainEvent` — publicado en `MarcarAceptado()`.
- `ComprobanteRechazadoDomainEvent` — publicado en `MarcarRechazado()` con CDR info.
- `ComprobanteAnuladoDomainEvent` — publicado en `MarcarAnulado()`.
- `ComprobanteObservadoDomainEvent` — publicado en `MarcarCorregir()`.

Consumidores típicos: `Integration` (envío a SUNAT), `IndicadoresNegocioBC` (reporting), `GestionInventarioBC` (reserva/ajuste de stock) y `ControlCajaBC` (registro de cobros).

## Flujo ejemplo: emisión de comprobante (paso a paso)
1. Usuario crea/edita comprobante en UI — Application crea/recupera `ComprobanteElectronico` en BORRADOR.
2. Se agregan líneas con `AgregarLinea(...)`. Cada línea recalcula su base/IGV/importe.
3. Antes de emitir, `RecalcularTotales()` valida montos y regla Boleta>700/DNI.
4. Usuario asigna `SerieYNumero` y presiona emitir; `Emitir()` valida precondiciones (Serie, líneas, RUC/TipoCambio según caso), recalcula totales y cambia `Estado` a `Enviado`.
5. `Emitir()` añade `ComprobanteEnviadoDomainEvent` al aggregate; el Application Service persiste el aggregate y publica el evento al bus.
6. `Integration` envía el comprobante a SUNAT; dependiendo de la respuesta, el Application puede llamar `MarcarAceptado()` o `MarcarRechazado()` en el aggregate.

## Consideraciones de diseño y recomendaciones
- Atomicidad: evitar transacciones distribuidas cuando se notifique a otros BCs; usar pub/sub y sagas para orquestar pasos multi-sistema.
- Conversión de moneda: `CambiarMoneda` tiene dos variantes (factor o VO `TipoCambio`). Recomendación: siempre usar `TipoCambio` con fecha para trazabilidad.
- Validaciones fiscales: están implementadas en el aggregate (p. ej., RUC/DNI). Mantenerlas aquí evita inconsistencias.
- Logs y auditoría: el aggregate tiene campos para `UltimoErrorTecnico`, `UltimoCdrCodigo`, `UltimoCdrDescripcion` y timestamps (`EnviadoEnUtc`, `AceptadoEnUtc`, `AnuladoEnUtc`). Exponerlos en read models para reporting.

## Cómo visualizar el diagrama
- Archivo Mermaid: `docs/comprobante_electronico.mmd`.
- Para renderizar localmente (PowerShell):
```powershell
npm install -g @mermaid-js/mermaid-cli
mmdc -i .\docs\comprobante_electronico.mmd -o .\docs\comprobante_electronico.png
```
- También puedes abrir `docs/comprobante_electronico.mmd` en https://mermaid.live para inspeccionar y exportar.

## Archivos creados
- `docs/comprobante_electronico.mmd` — Diagrama de clases Mermaid.
- `docs/comprobante_electronico_explained.md` — Este documento explicativo.

Si quieres, continúo con:
- Añadir un diagrama de secuencia (Mermaid Sequence) que muestre el intercambio entre `ComprobantesElectronicosBC`, `GestionInventarioBC`, `Integration` y `ControlCajaBC` durante la emisión y confirmación.
- Generar PNG/SVG desde los .mmd y guardarlos en `docs/`.
- Extraer JSON Schema para el evento `ComprobanteEnviado` en `docs/events/`.

Indícame cuál de estas opciones prefieres y lo hago. 