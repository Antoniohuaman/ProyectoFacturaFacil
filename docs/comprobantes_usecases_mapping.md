# Casos de uso vs métodos del Aggregate — ComprobantesElectronicosBC

Este documento mapea los casos de uso de `src/ComprobantesElectronicosBC/Application/UseCases` a los métodos y responsabilidades del aggregate `ComprobanteElectronico` y sus entidades.

## Resumen rápido
- Aggregate principal: `ComprobanteElectronico` (cambia estado, gestiona líneas y totales y publica eventos).  
- Entidad interna: `ComprobanteLinea` (gestiona cálculos por línea).  
- Use Cases: orquestadores de aplicación que crean/actualizan el aggregate y llaman repositorios/adapters.

---

## Mapeo detallado (UseCase → Aggregate / Repos / Servicios)

1) EmitirComprobanteUseCase (Application/UseCases/EmitirComprobanteUseCase.cs)
   - Acciones principales en UC:
     - Construir EmisorSnapshot y ClienteSnapshot.
     - Crear agregado en BORRADOR (ComprobanteElectronico.CrearBorrador).
     - Agregar líneas: llama internamente a ComprobanteElectronico.AgregarLinea(...) por cada ítem.
     - Reservar numeración: usa `INumeracionService.ReservarSiguienteAsync` (infraestructura).
     - Asignar serie/número: ComprobanteElectronico.AsignarSerieYNumero(...).
     - Emitir: ComprobanteElectronico.Emitir() (valida reglas, recalcula totales y agrega ComprobanteEnviadoDomainEvent).
     - Persistir: `IComprobanteEmitidoPersister.GuardarEmitidoAsync(snapshot)` (adapter especializado).
     - Publicar eventos: drena events con DrainDomainEvents() y publica via `IEventBus`.
   - Servicios externos usados: INumeracionService, IComprobanteEmitidoPersister, IEventBus.

2) GuardarBorradorUseCase (Application/UseCases/GuardarBorradorUseCase.cs)
   - Acciones principales en UC:
     - Valida formato serie/tipo (TipoDeComprobante + SerieYNumero).
     - Usa `IComprobanteDraftFactory` (puerta de fábrica) para crear o aplicar cambios sobre el agregado en estado BORRADOR.
     - Repositorio: AddAsync / UpdateAsync en `IComprobanteRepository`.
     - Unit of Work: `IUnitOfWork.CommitAsync()`.
     - Publicación opcional de eventos drenados (IEventBus).
   - Métodos del agregado invocados por la factoría: AgregarLinea(), EditarLinea(), EliminarLinea(), AsignarSerieYNumero(), CambiarCliente(), CambiarObservaciones(), CambiarFormaDePago(), etc.

3) CorregirComprobanteUseCase (Application/UseCases/CorregirComprobanteUseCase.cs)
   - Acciones principales:
     - Carga agregado por Id (IComprobanteRepository.GetByIdAsync).
     - Validación Serie/Número si vienen fijados (pre-check duplicidad).
     - Delegación a `IComprobanteCorrector.CorregirAsync` que aplica cambios al agregado (usa métodos del aggregate para cambiar campos permitidos).
     - Persistencia (UpdateAsync + Commit) y publicación de events.
   - Métodos del agregado típicamente usados: CambiarCliente(), AsignarSerieYNumero(), CambiarVencimiento(), CambiarObservaciones(), RecalcularTotales(), MarcarCorregir() si se requiere.

4) AnularComprobanteUseCase (Application/UseCases/AnularComprobanteUseCase.cs)
   - Acciones principales:
     - Carga agregado.
     - Delegación a `IComprobanteAnulador.AnularAsync` que invoca ComprobanteElectronico.MarcarAnulado(cdrBajaEnUtc) o métodos equivalentes y añade notas internas.
     - Persistencia y publicación de events (ComprobanteAnuladoDomainEvent).

5) DuplicarComprobanteUseCase (Application/UseCases/DuplicarComprobanteUseCase.cs)
   - Acciones principales:
     - Carga el comprobante origen.
     - Delegación a `IComprobanteDuplicator.DuplicarAsync` que construye un nuevo ComprobanteElectronico (nuevo borrador) copiando líneas, VOs y aplicando overrides.
     - Persistencia (AddAsync) y publicación de events si procede.
   - Métodos del agregado que el duplicador suele usar: ComprobanteElectronico.CrearBorrador (factory), AgregarLinea(...), AsignarSerieYNumero(...) (si se fija serie), CambiarCliente(...).

6) ConsultarComprobanteUseCase / ListarComprobantesUseCase
   - Son casos de consulta (read-only). Normalmente usan `IComprobanteQueryRepository` para devolver read-models (`ComprobanteResumenDto`, `ComprobanteDetalleDto`). No invocan métodos mutantes del agregado.

---

## Recomendaciones para documentación y pruebas
- Completar diagramas por agregado (ej.: separar `ComprobanteLinea` en su propio mmd con atributos y reglas de cálculo).  
- Generar diagramas de secuencia para los casos: Emitir (incluye INumeracionService, IComprobanteEmitidoPersister, EventBus) y Corregir/Anular (incluye validaciones y adaptadores).  
- Añadir test de integración del UseCase `EmitirComprobanteUseCase` que compruebe: creación de borrador, agregado de líneas, asignación de serie, Emitir() y publicación de `ComprobanteEnviadoDomainEvent`.

---

Archivos creados recientemente:
- `docs/comprobantes_domain_classes.mmd` (diagrama Mermaid del dominio).  
- `docs/comprobantes_usecases_mapping.md` (este documento).

Si quieres, puedo:
- Renderizar `comprobantes_domain_classes.mmd` a PNG/SVG y añadirlo en `docs/`.
- Generar diagramas de secuencia (Mermaid) para `EmitirComprobante` y `CorregirComprobante`.
- Extraer interfaces/VOs faltantes y añadirlos al diagrama (por ejemplo `AfectacionImpuesto`, `TasaImpuesto`, `CentroDeCosto`).

Dime qué prefieres y lo continúo.