# Mapa de Contexto (Context Map) — ProyectoFacturaFacil

Este documento presenta un mapa de contexto (Context Map) de alto nivel para el proyecto ProyectoFacturaFacil, con una explicación clara de los límites entre los Bounded Contexts (BC), las dependencias (Upstream/Downstream), los patrones de integración recomendados (Shared Kernel, Customer-Supplier, Anticorruption Layer, Published Language, Conformist), y los eventos / contratos más relevantes.

## Objetivo
Dar una visión práctica y accionable de cómo los distintos BCs interactúan, quién es dueño de cada dato, qué comunicaciones deben ser síncronas o asíncronas, y qué patrones aplicar para mantener límites claros y evitar acoplamientos indeseados.

## Lista de Bounded Contexts principales
- `SharedKernel` — Value Objects, tipos y utilidades compartidas (por ejemplo `Dinero`, `Moneda`, `DocumentoIdentidad`, `ProductoId`, `UnidadDeMedida`).
- `ConfiguracionSistemaBC` — Maestro de configuración (series, formas de pago, unidades de medida, datos de empresa/establecimientos).
- `CatalogoArticulosBC` — Catálogo de productos, atributos, imágenes y metadatos del producto.
- `ListaPreciosBC` — Definición y resolución de precios según reglas (listas, descuentos, promociones, reglas por cliente/empresa).
- `GestionClientesBC` — Gestión de clientes, contactos, documentos, adjuntos.
- `GestionInventarioBC` — Stock, movimientos, reservas, transferencias y reglas de valoración.
- `ComprobantesElectronicosBC` — Emisión y ciclo de vida de comprobantes electrónicos (facturas, boletas, notas), líneas, totales y estados.
- `ControlCajaBC` — Turnos de caja, movimientos de caja, conciliación de pagos.
- `IndicadoresNegocioBC` — Consolida eventos y métricas (ventass, ranking, KPIs) para reporting y alertas.
- `Integration` — Adaptadores hacia sistemas externos (SUNAT, pasarelas de pago, almacenes externos, integraciones con ERPs externos).

## Principios generales de comunicación
- Ownership: Cada BC es dueño de su modelo de persistencia y sus invariantes. Otros BCs no deben escribir directamente en la base de datos de otro BC.
- Read vs Write: Cuando un BC necesita información de otro, preferir:
  - Consulta explícita (síncrona) para datos maestros pequeños y estables (p.ej., `CatalogoArticulos` → `Producto` metadata). Uso de APIs REST/grpc.
  - Repliación/ReadModel (asíncrona) para alta escalabilidad y baja latencia (p.ej., `IndicadoresNegocio` consume eventos para construir read models).
- Mensajería de eventos (pub/sub) para notificar cambios de estado importantes (emisión de comprobante, confirmación de transferencia, reserva creada/liberada).
- Contratos y versión: Definir mensajes/eventos con contratos estables (Published Language). Versionar cuando sea necesario.

## Patrón de integración recomendado por relación
A continuación se lista cada relación importante, con el patrón recomendado y la justificación.

### `ComprobantesElectronicosBC` (Downstream heavy)
- Depende de (Upstream): `CatalogoArticulosBC`, `ListaPreciosBC`, `GestionClientesBC`, `GestionInventarioBC`, `ConfiguracionSistemaBC`, `ControlCajaBC`.
- Recomendación de integración:
  - `CatalogoArticulosBC` → Consulta síncrona de metadatos (SKU, UMs) y/o cache local (read model) para performance. Use API con contrato estable.
  - `ListaPreciosBC` → Resolución de precio preferentemente síncrona en el momento de la creación del comprobante (o una llamada de fallback). También soportar policy: pedir precio final antes de emitir.
  - `GestionInventarioBC` → Validación de disponibilidad: preferir reserva asíncrona (evento "ReservaCreada") o una llamada síncrona para bloquear stock si el negocio lo exige. Use patrón Customer-Supplier si `ComprobantesElectronicosBC` consume y depende fuertemente del comportamiento de `GestionInventarioBC`.
  - `GestionClientesBC` → Consulta de datos cliente (síncrona) o consumo de eventos para réplica local. Para datos sensibles a reglas fiscales (RUC/DNI), consulta directa o servicio de identidad maestro.
  - `ConfiguracionSistemaBC` → Shared Kernel ligero: UMs, series y reglas de validación deben provenir desde el maestro. `ComprobantesElectronicosBC` puede leer configuración vía API o disponer de un caché read-model.
  - `ControlCajaBC` → Publicar eventos de pago; puede ser síncrono para confirmación de cobro.
- Eventos publicados por `ComprobantesElectronicosBC`:
  - `ComprobanteEmitido` (payload: comprobanteId, tipo, total, clienteId, fecha)
  - `ComprobanteAnulado` / `ComprobanteAceptado` / `ComprobanteCorregido`
- Consumo de eventos: `ComprobanteEmitido` consumido por `IndicadoresNegocioBC` y `Integration`.
- Patrón sugerido: mezcla de API (síncrona) para consultas críticas y eventos pub/sub para notificaciones y eventual consistencia.

### `GestionInventarioBC`
- Upstream/Downstream:
  - Provee stock y reservas a `ComprobantesElectronicosBC` y a `CatalogoArticulosBC` (información de disponibilidad).
  - Publica eventos: `ReservaCreada`, `ReservaLiberada`, `MovimientoRegistrado`, `TransferenciaConfirmada`.
- Recomendación:
  - Exponer API para consultas en tiempo real del stock si la latencia/consistencia es crítica.
  - Implementar endpoints asíncronos y publicar eventos en bus para sincronización eventual (consumidores: `ComprobantesElectronicosBC`, `IndicadoresNegocioBC`).
  - Patrón Customer-Supplier o Anticorruption Layer (ACL) si consumidores transforman o adaptan el modelo (ej. marketplaces externos).

### `CatalogoArticulosBC` y `ListaPreciosBC`
- `CatalogoArticulosBC` propietario del modelo del producto (SKU, UM, atributos).
- `ListaPreciosBC` propietario de reglas de precio.
- Integración:
  - `ListaPreciosBC` puede suscribirse a eventos de `CatalogoArticulosBC` para actualizar reglas (p.ej., cambio de atributos que afectan precio).
  - `ComprobantesElectronicosBC` consulta a `ListaPreciosBC` o utiliza read-model construido por `ListaPreciosBC`.
- Patrón: API + Published Language para eventos (p.ej., `ProductoActualizado`, `PrecioActualizado`).

### `GestionClientesBC`
- Propietario de datos de cliente.
- Publica eventos: `ClienteCreado`, `ClienteActualizado`.
- Uso: `ComprobantesElectronicosBC` y `IndicadoresNegocioBC` consumen estos eventos.
- Patrón: Shared Kernel para Value Objects (DocumentoIdentidad), y Published Language para eventos.

### `ControlCajaBC`
- Maneja turnos y movimientos de caja.
- Produce eventos: `MovimientoRegistrado`, `TurnoCajaCerrado`.
- `ComprobantesElectronicosBC` puede notificar pagos a `ControlCajaBC` (síncrono o asíncrono según necesidad).

### `ConfiguracionSistemaBC`
- Maestro para series, UMs y reglas fiscales.
- Debería ser tratado como servicio maestro con acceso de sólo lectura para otros BCs (recomendado caché local o endpoint de configuración).
- Patrón: Shared Kernel para UMs y tipos; API para cambios de configuración.

### `IndicadoresNegocioBC`
- Consumidor principal de eventos (pub/sub). No es maestro de datos de negocio, sino que consolida.
- Debe consumir eventos de: `ComprobantesElectronicosBC`, `GestionInventarioBC`, `GestionClientesBC`, `ControlCajaBC`.
- Patrón: Event-driven (suscriptor), construye sus read models para reporting.

### `Integration` (externo)
- Contiene adaptadores a SUNAT, pasarelas de pago, ERPs externos.
- Debe recibir eventos de `ComprobantesElectronicosBC` (para envío a SUNAT) y de `ControlCajaBC` (conciliación), y exponer endpoints para callbacks.
- Patrón: Anticorruption Layer entre sistemas externos y nuestros BCs si los modelos son incompatibles.

## Patrón de consistencia y transacciones
- Evitar transacciones distribuidas. Preferir sagas/event-driven workflows para orquestar multi-step operations (por ejemplo: emisión de comprobante → confirmación de pago → ajuste de stock → notificación a SUNAT).
- Implementar sagas orquestadas en la capa de Application (o un orquestador) o sagas coreografiadas mediante eventos (cada BC actúa al recibir eventos y publica nuevos eventos).

## Contratos / Mensajes clave (Published Language sugerido)
- ComprobanteEmitido { comprobanteId, tipo, serie, numero, fecha, clienteId, total, moneda }
- ComprobanteAnulado { comprobanteId, motivo, fecha }
- ReservaCreada { reservaId, productoId, almacenId, cantidad, vencimiento }
- MovimientoRegistrado { movimientoId, almacenId, lineas[], fecha }
- ClienteCreado/ClienteActualizado { clienteId, documento, razonSocial }
- PrecioActualizado { productoId, listaPrecioId, precio }

> Implementar estos mensajes en un esquema versionable (por ejemplo JSON Schema o Protobuf) y publicar en un bus (RabbitMQ/Kafka/Azure Service Bus). Mantener backward compatibility tanto como sea posible.

## Recomendaciones prácticas inmediatas
1. Definir un catálogo de eventos (Published Language) con JSON Schema/Protobuf y sus versiones. Empezar por los mensajes listados arriba.
2. Para `ComprobantesElectronicosBC`, decidir si la verificación de stock se hace mediante reserva síncrona o reservación asíncrona. Recomiendo reservar (asíncrono) para reducir acoplamiento en picos altos y permitir reintentos / compensaciones.
3. Implementar read-models replicados para datos de consulta frecuentes (productos, precios, clientes) usados por `ComprobantesElectronicosBC` para mejorar latencia.
4. Establecer un pequeño Shared Kernel (VOs) con reglas de validación centralizadas (p.ej., `Dinero`, `Moneda`, `DocumentoIdentidad`). Evitar que SharedKernel crezca demasiado: mantener solo VOs estables.
5. Para integraciones con SUNAT y pasarelas, crear un `Integration` BC con ACL para evitar que cambios externos rompan el dominio.

## Ejemplo de flujo (comprobante completo)
1. Usuario solicita crear comprobante en UI/API.
2. `ComprobantesElectronicosBC` consulta (o usa read-model): `CatalogoArticulosBC`(metadatos), `ListaPreciosBC`(precio), `GestionClientesBC`(datos cliente).
3. `ComprobantesElectronicosBC` publica evento `ComprobanteEmitido` tras emitir.
4. `GestionInventarioBC` consume `ComprobanteEmitido` (o un evento específico `ReservarStockSolicitud`) y crea reservas o ajusta stock (publica `ReservaCreada`/`MovimientoRegistrado`).
5. `ControlCajaBC` recibe notificación si hay pago y registra movimiento.
6. `Integration` envía el comprobante a SUNAT; `IndicadoresNegocioBC` consolida métricas.

## Riesgos y mitigaciones
- Riesgo: Shared Kernel crece y provoca acoplamientos. Mitigación: versionar Shared Kernel y limitar inclusiones; preferir contratos via events/APIs cuando sea posible.
- Riesgo: Consistencia de stock en picos altos. Mitigación: diseñar reservas con expiración, permitir compensaciones y reconciliación manual.
- Riesgo: Cambios en contratos externos (SUNAT). Mitigación: ACL + adaptadores en `Integration` y pruebas end-to-end.

## Siguiente pasos sugeridos
- Validar con el equipo los eventos y contratos propuestos; crear un directorio `docs/events/` con JSON Schema para cada mensaje.
- Implementar prototipo de bus (Kafka/Rabbit) en entorno de staging y publicar un POC de `ComprobanteEmitido` → `IndicadoresNegocioBC`.
- Refinar diagrama y generar PNG/SVG; crear un README corto en `docs/` con instrucciones de render.

---

Archivo relacionado creado: `docs/bounded_contexts_overview.mmd` (diagrama de alto nivel). También se generaron diagramas por BC en `docs/comprobantes.mmd` y `docs/gestion_inventario.mmd`.

Si quieres, genero ahora:
- Los JSON Schemas de los eventos principales en `docs/events/`.
- Un PNG/SVG de este mapa (necesitaré mermaid-cli disponible o puedo darte el comando para que lo generes localmente).

¿Procedo con alguna de estas acciones? 
