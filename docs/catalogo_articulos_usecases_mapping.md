# Mapping: Use Cases → Agregado / Puertos (CatalogoArticulosBC)

Este documento mapea los DTOs y UseCases en `src/CatalogoArticulosBC/Application/DTOs` a las operaciones esperadas en el agregado `Producto`, repositorios y servicios externos.

## Contrato breve del agregado `Producto`
- Inputs: DTOs de creación/edición/importación (por ejemplo `CrearProductoSimpleInputDto`, `EditarProductoSimpleInputDto`, `ProductoImportacionDto`).
- Outputs: DTOs de producto (snapshots) o `ResultadoImportacionDto`/`ResultadoExportacionDto` para operaciones batch.
- Error modes: ValidationException (datos inválidos), NotFoundException (producto no existe), ConflictException (código/SKU duplicado).

---

## Mapeo de Use Cases (basado en `src/CatalogoArticulosBC/Application/DTOs` y `UseCases`)

- CrearProductoSimpleInputDto
  - Orquestador: `CrearProductoSimpleUseCase`
  - Acciones:
    - Validar DTO (Application)
    - `Producto.Create(...)` o factory de dominio
    - `IProductoRepository.Save(producto)`
    - `IUnitOfWork.CommitAsync()`
  - Resultado: `CrearProductoSimpleOutputDto` con `ProductoId` y snapshot.

- EditarProductoSimpleInputDto
  - Orquestador: `EditarProductoSimpleUseCase`
  - Acciones:
    - `IProductoRepository.GetById(productoId)`
    - `producto.Editar(...)` (método de agregado)
    - `IProductoRepository.Save(producto)` + `Commit`

- EliminarProductoSimpleInputDto / EliminarProductosSeleccionadosInputDto / EliminarTodosLosProductosInputDto
  - Orquestador: `EliminarProductoSimpleUseCase`, `EliminarProductosSeleccionadosUseCase`, etc.
  - Acciones:
    - Para eliminación simple: `producto.Inhabilitar()` o `Delete()` según política (se recomienda soft-delete)
    - Persistir cambios + `Commit`

- HabilitarProductoInputDto / InhabilitarProductoInputDto
  - Orquestador: `HabilitarProductoUseCase` / `InhabilitarProductoUseCase`
  - Acciones: `producto.Habilitar()` / `producto.Inhabilitar()` → persistir

- ListarProductosInputDto / ConsultarProductoInputDto
  - Orquestador: `ListarProductosUseCase` / `ConsultarProductoUseCase`
  - Acciones:
    - `IProductoRepository.List(filter)` para listados paginados
    - `IProductoRepository.GetById(id)` para consulta individual
    - Mapear a DTOs de salida (sin modificar el agregado)

- Registrar / Actualizar Atributos / Variantes
  - Orquestador: `AgregarVarianteUseCase` / `ActualizarStockUseCase` / `AplicarPrecioUseCase`
  - Acciones:
    - Cargar agregado `producto`
    - Llamar `producto.AgregarVariante(...)`, `producto.ActualizarStock(variantId, cantidad)`, `producto.AplicarPrecio(...)`
    - Persistir + commit

- DescargarPlantillaImportacionUseCase / ResultadoImportacionDto / ResultadoExportacionDto
  - Orquestador: `DescargarPlantillaImportacionUseCase`, `ImportarProductosUseCase`, `ExportarProductosUseCase`
  - Acciones:
    - Para import: `IImportExportService.Importar(file, options)` → devuelve `ResultadoImportacionDto` con filas procesadas/errores
    - Para export: `IImportExportService.Exportar(filter)` → devuelve archivo/URL o `ResultadoExportacionDto`
    - Opcional: procesado en background (job), con eventos de progreso publicados a un bus

---

## Puertos / Servicios externos relevantes
- IProductoRepository: persistencia de `Producto` (GetById, Save, Delete, List)
- IUnitOfWork: confirmar transacciones
- IImportExportService: importación/exportación masiva (CSV/XLSX)
- (Opcional) IFileStorage: almacenar/servir imágenes de producto
- (Opcional) IThumbnailService: generar miniaturas para imágenes importadas

---

## Casos de borde y recomendaciones
- Unicidad: validar código de producto y SKU a nivel de dominio y en persistencia (índice único).
- Importación: validar esquema por filas, reportar líneas erróneas con mensajes claros (uso de `ProductoImportacionSpecification`).
- Stock/Variantes: si el catálogo maneja inventario, preferir eventos (Domain Events) para publicar cambios de stock a `GestionInventarioBC` en lugar de llamadas sincrónicas.
- Imágenes: usar un `IFileStorage` (S3/Blob) y almacenar solo URL en el dominio.

---

Si quieres, puedo:
- 1) Renderizar el `.mmd` a PNG/SVG y colocarlo en `docs/`.
- 2) Generar diagramas de secuencia para flujos críticos (CrearProducto, ImportarProductos, ActualizarStock).
- 3) Refinar el diagrama leyendo las clases exactas en `src/CatalogoArticulosBC/Domain` para asegurar atributos 1:1.

Dime cuál prefieres y procedo.