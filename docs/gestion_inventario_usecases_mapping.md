# Mapping: Use Cases → Agregado / Puertos (GestionInventarioBC)

Este documento mapea los Use Cases (carpetas `UseCases/*`) y DTOs relacionados en `src/GestionInventarioBC/Application` con los métodos de los agregados y los puertos (repositorios/servicios). Está basado en las clases reales encontradas en `src/GestionInventarioBC/Domain`.

## Resumen rápido del contrato de dominio
- Agregados principales: `Almacen`, `StockPorAlmacen`, `MovimientoInventario`, `ReservaStock`, `TransferenciaInventario`.
- Repositorios: `IAlmacenRepository`, `IStockPorAlmacenRepository`, `IMovimientoInventarioRepository`, `IReservaStockRepository`, `ITransferenciaInventarioRepository`.
- Políticas: `PoliticaReserva` (valida si es posible reservar contra la disponibilidad).
- Errores/Excepciones relevantes: `BusinessRuleException`, `ExcepcionStockInsuficiente`, `ExcepcionReservaNoEncontrada`, `ExcepcionMovimientoInvalido`.

---

## Use Cases importantes y mapeo a dominio

- Almacenes
  - CrearAlmacen / RegistrarAlmacen
    - Obtener: DTO input con `EmpresaId`, `EstablecimientoId`, `AlmacenId`, `Nombre`
    - Lógica: `Almacen.Crear(...)`
    - Persistencia: `IAlmacenRepository.GuardarAsync(almacen)` + `IUnitOfWork.CommitAsync()`
  - ActualizarNombreAlmacen
    - Cargar: `IAlmacenRepository.ObtenerAsync(...)`
    - Llamar: `almacen.ActualizarNombre(nuevoNombre)`
    - Persistir
  - Habilitar/Deshabilitar Almacén
    - `almacen.Habilitar()` / `almacen.Deshabilitar()` + persistir

- Movimientos de Inventario
  - RegistrarMovimiento (ingreso/egreso/ajuste)
    - Input: `MovimientoInventario` data (fecha, tipo, motivo, lineas)
    - Orquesta: crear `MovimientoInventario.Registrar(...)`
    - Para movimientos que afectan stock: cargar `StockPorAlmacen` por producto (vía `IStockPorAlmacenRepository`), aplicar `Ingresar/Egresar` según tipo, persistir `StockPorAlmacen` y `MovimientoInventario` (repositorio de movimientos)
    - Persistir: `IMovimientoInventarioRepository.GuardarAsync(movimiento)` + `IUnitOfWork.CommitAsync()`
  - AnularMovimiento
    - Obtener movimiento, validar estado, revertir efectos si aplica (depende de política), persistir

- Reservas de Stock
  - CrearReserva
    - Validar disponibilidad: cargar `StockPorAlmacen` y evaluar `PoliticaReserva.PuedeReservar(DisponibilidadStock, Cantidad)`
    - Si OK: `ReservaStock.Crear(...)` → persistir `IReservaStockRepository.GuardarAsync(reserva)` y actualizar `StockPorAlmacen.Reservar(cantidad)` y persistir
    - Si NO: devolver error de negocio con código `RESERVA_NO_PERMITIDA`
  - ConfirmarReserva / LiberarReserva / ExtenderReserva / CancelarReserva
    - Acciones: invocar `reserva.Confirmar()` / `reserva.Liberar()` / `reserva.ExtenderHasta(...)` / `reserva.Cancelar()`; cuando cambia estado, actualizar `StockPorAlmacen` si corresponde y persistir

- Consultas / Read Models
  - ConsultarDisponibilidad / ListarStockPorAlmacen
    - Usan `IStockPorAlmacenRepository.ListarPorAlmacenAsync(...)` o un `ICatalogoReadModel` para enriquecer con datos de producto
    - Read-only: no usan UoW

- Transferencias
  - CrearTransferencia
    - Validaciones: origen != destino, suficiente stock en origen (`StockPorAlmacen.Egresar` provisional o bloquear mediante reserva), crear `TransferenciaInventario.Crear(...)`, persistir
  - ConfirmarTransferencia
    - `transferencia.Confirmar()`; aplicar decremento en origen e incremento en destino vía `StockPorAlmacen` y persistir cambios en ambos almacenes
  - CancelarTransferencia
    - `transferencia.Cancelar()`; revertir bloqueos si existieran

- Operaciones masivas / Import
  - Importación de stock / Ajustes masivos
    - Flujo: `IImportService` procesa archivo → crea lotes de `MovimientoInventario` o `StockPorAlmacen` updates → persistir en transacción (UoW). Publicar `ImportacionStockCompletada` event cuando termine.

---

## Puertos y servicios externos esperados por adaptadores
- IAlmacenRepository / IStockPorAlmacenRepository / IMovimientoInventarioRepository / IReservaStockRepository / ITransferenciaInventarioRepository (contratos vistos en dominio).
- IUnitOfWork (commit atómico cuando se cambia stock y se registra movimiento/reserva).
- ICatalogoReadModel (opcional) para obtener información de `Producto` (nombre, UM) en queries.
- IFileStorage / IImportService para importaciones masivas.

---

## Políticas y reglas de negocio destacadas
- Política de reserva: `PoliticaReserva.PuedeReservar(DisponibilidadStock, CantidadStock)` devuelve SpecificationResult con código `RESERVA_NO_PERMITIDA` cuando no es posible.
- Stock invariant: `Reservado <= Real`. Operaciones que rompan el invariante deben lanzar `BusinessRuleException`.
- Transferencias: el origen y destino no pueden ser el mismo; confirmación solo desde estado `Creada`.
- Reservas: solo reservas en `Pendiente` permiten `Confirmar`, `Liberar`, `Vencer`, `Extender`.

---

## Casos de borde y recomendaciones de implementación
- Serialización y concurrencia: usar Version (optimistic concurrency) en `StockPorAlmacen`/`Almacen` para evitar sobrescrituras de stock concurrentes.
- Consistencia eventual: para integraciones con `GestionInventarioBC` y `ComprobantesElectronicosBC` se recomienda publicar Domain Events (ej. `MovimientoRegistrado`, `ReservaCreada`) y sincronizar por consumidores que actualicen read models.
- Operaciones críticas (ej. egresos masivos): proteger con transacciones y validaciones en repositorio para evitar race conditions.

---

Si quieres que: 
- 1) renderice el `gestion_inventario_domain_classes.mmd` a PNG/SVG y lo agregue a `docs/` (podemos ejecutar `mmdc` en PowerShell si instalas `@mermaid-js/mermaid-cli`),
- 2) genere diagramas de secuencia para flujos clave (RegistrarMovimiento, CrearReserva, ConfirmarTransferencia), o
- 3) extraiga los DTOs exactos de `src/GestionInventarioBC/Application/DTOs` y los mapee 1:1 en el mapping,
indica la opción y procedo.