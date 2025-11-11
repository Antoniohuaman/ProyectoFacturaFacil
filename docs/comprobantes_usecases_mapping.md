## Casos de uso vs métodos del Aggregate — ComprobantesElectronicosBC

Este documento mapea los casos de uso de `src/ComprobantesElectronicosBC/Application/UseCases` a los métodos y responsabilidades del aggregate `ComprobanteElectronico` y sus entidades internas (p. ej. `ComprobanteLinea`). Está basado en el código real leído en `src/ComprobantesElectronicosBC`.

### Resumen rápido
- Aggregate principal: `ComprobanteElectronico` — maneja ciclo de vida (Borrador → Emitido/Enviado → Aceptado/Anulado/Observado) y cálculos de totales.
- Entidad interna: `ComprobanteLinea` — cálculos unitarios, descuentos y redondeos según UM.
- Puertos/Adapters usados por los UseCases: `INumeracionService`, `IComprobanteEmitidoPersister`, `IComprobanteRepository`, `IComprobanteQueryRepository`, `IUnitOfWork`, `IEventBus`.

---

### Mapeo detallado (UseCase → Aggregate / Repos / Servicios)

1) EmitirComprobanteUseCase (`Application/UseCases/EmitirComprobanteUseCase.cs`)
   - Flujo resumido:
     1. Normaliza inputs (tipo, moneda, tasa, fecha).
     2. Construye `EmisorSnapshot` y `ClienteSnapshot`.
     3. Crea agregado en BORRADOR con `ComprobanteElectronico.CrearBorrador(...)`.
     4. Por cada ítem del request invoca `ComprobanteElectronico.AgregarLinea(...)` (factores: Descripcion, UM, Cantidad, Precio, Afectacion, Tasa).
     5. Reserva numeración vía `INumeracionService.ReservarSiguienteAsync(...)`.
     6. Llama `ComprobanteElectronico.AsignarSerieYNumero(...)` y `ComprobanteElectronico.Emitir()`.
     7. Persiste resultado emitido con `IComprobanteEmitidoPersister.GuardarEmitidoAsync(snapshot)`.
     8. Drena events del agregado (`DrainDomainEvents()`) y publica por `IEventBus`.

   - Puertos/servicios implicados: `INumeracionService`, `IComprobanteEmitidoPersister`, `IEventBus`.

2) GuardarBorradorUseCase (`Application/UseCases/GuardarBorradorUseCase.cs`)
   - Flujo resumido:
     1. Valida tipo/serie y duplicidad si viene número.
     2. Usa `IComprobanteDraftFactory` para crear o aplicar cambios sobre `ComprobanteElectronico` (factory aplica llamadas al agregado: AgregarLinea, CambiarCliente, AsignarSerieYNumero, etc.).
     3. Persiste con `IComprobanteRepository.AddAsync` o `UpdateAsync` y confirma con `IUnitOfWork.CommitAsync`.
     4. Opcional: publica eventos drenados a `IEventBus`.

   - Métodos de agregado que la factoría invoca: `AgregarLinea(...)`, `CambiarCliente(...)`, `AsignarSerieYNumero(...)`, `CambiarFormaDePago(...)`, `CambiarVencimiento(...)`, `RecalcularTotales()`.

3) CorregirComprobanteUseCase
   - Flujo resumido:
     1. Carga agregado (`IComprobanteRepository.GetByIdAsync`).
     2. Valida y prepara cambios (serie/número, campos permitidos).
     3. Delegación a `IComprobanteCorrector` / factoria que aplica mutaciones sobre el agregado.
     4. Persiste (`UpdateAsync(expectedVersion)`) y confirma (`IUnitOfWork.CommitAsync`).
     5. Publica events si existen (`IEventBus`).

4) AnularComprobanteUseCase
   - Carga el agregado, delega a `IComprobanteAnulador` que invoca `ComprobanteElectronico.MarcarAnulado(...)` y persiste el cambio; publica `ComprobanteAnuladoDomainEvent`.

5) DuplicarComprobanteUseCase
   - Carga comprobante origen y crea un nuevo borrador copiando líneas/VOs (`ComprobanteElectronico.CrearBorrador` + `AgregarLinea(...)`), persiste y devuelve el nuevo Id.

6) Consultar / Listar
   - `ConsultarComprobanteUseCase` / `ListarComprobantesUseCase` usan `IComprobanteQueryRepository` (read models: `ComprobanteResumenDto`, `ComprobanteDetalleDto`) — operaciones read-only que no modifican el agregado.

---

### Puntos prácticos (basados en el código)
- Validaciones en Aggregate: `EnsureEditable()` limita mutaciones a estados Borrador/Corregir.
- Recalculo de montos y reglas fiscales está implementado en `ComprobanteLinea.Recalcular()` y `ComprobanteElectronico.RecalcularTotales()`.
- La numeración es reservada por el UseCase antes de llamar `Emitir()` — la persistencia del emitido usa un persister especializado (`IComprobanteEmitidoPersister`) que suele encapsular operaciones transversales (persistir snapshot, asociados UBL, CDR metadata).
- Publicación de eventos: UseCases drenan eventos del agregado y los publican por `IEventBus`.

---

### Recomendaciones y siguientes pasos
- Renderizar `docs/comprobantes_domain_classes.mmd` a PNG/SVG para presentaciones.
- Generar diagramas de secuencia para `EmitirComprobante` y `CorregirComprobante` (incluye `INumeracionService`, `IComprobanteEmitidoPersister`, `IEventBus`).
- Añadir tests de integración del UseCase `EmitirComprobanteUseCase` que simulen numeración y persister.

Si quieres que automatice alguno de esos pasos (render, secuencias, extracción DTOs), dime cuál y lo hago.