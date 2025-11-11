# Mapping: Use Cases → Agregado / Puertos (ConfiguracionSistemaBC)

Este documento mapea los DTOs / UseCases en `src/ConfiguracionSistemaBC/Application/DTOs` hacia los métodos del agregado `ConfiguracionEmpresa`, repositorios y puertos implicados. Está pensado para arquitectos y desarrolladores que implementan adaptadores (adapters) o pruebas.

## Contrato breve del agregado `ConfiguracionEmpresa`
- Inputs: DTOs provenientes de Application layer (ej. `RegistrarEstablecimientoInputDto`, `RegistrarSerieComprobanteInputDto`).
- Outputs: Snapshot/DTO de configuración o `void` para comandos; en errores devuelve exceptions con códigos (ValidationException, NotFoundException, ConflictException).
- Errores comunes: Empresa no encontrada, Serie ya existente, Usuario duplicado, validación RUC inválido.

---

## Mapeo (UseCase DTO -> Acciones esperadas / Puertos)

- RegistrarConfiguracionEmpresaInputDto
  - Orquestador: RegistrarConfiguracionEmpresaUseCase
  - Acciones:
    - Validar DTO (Application)
    - Crear nueva instancia `ConfiguracionEmpresa` (factory/domain)
    - `IConfiguracionRepository.Save(config)`
    - `IUnitOfWork.CommitAsync()`
  - Resultado: `RegistrarConfiguracionEmpresaOutputDto` con `EmpresaId` y snapshot minimal.

- ActualizarConfiguracionEmpresaInputDto
  - Orquestador: ActualizarConfiguracionEmpresaUseCase
  - Acciones:
    - Obtener configuración: `IConfiguracionRepository.GetByEmpresaId(empresaId)`
    - Llamar `config.ActualizarConfiguracion(dto)` (método de agregado)
    - `IConfiguracionRepository.Save(config)` + `IUnitOfWork.CommitAsync()`
  - Casos de error: validaciones de RUC/moneda/serie.

- CambiarAmbienteEmpresaInputDto
  - Orquestador: CambiarAmbienteEmpresaUseCase
  - Acciones: `config.CambiarAmbiente(nuevoAmbiente)` → persistir.

- RegistrarEstablecimientoInputDto
  - Orquestador: RegistrarEstablecimientoUseCase
  - Acciones:
    - `config.RegistrarEstablecimiento(dto)`
    - Persistir repo + commit

- RegistrarSerieComprobanteInputDto / RegistrarSerieEnEstablecimientoInputDto
  - Orquestador: RegistrarSerieComprobanteUseCase / RegistrarSerieEnEstablecimientoUseCase
  - Acciones:
    - Validar formato de serie según `TipoComprobante`
    - `config.RegistrarSerie(...)` o `establecimiento.AgregarSerie(...)`
    - Si `EsPorDefecto`, marcar otras series como no por defecto
    - `IConfiguracionRepository.Save(config)` + `Commit`

- EstablecerSeriePorDefectoInputDto
  - Orquestador: EstablecerSeriePorDefectoUseCase
  - Acciones:
    - `config.EstableserSeriePorDefecto(serieId, establecimientoId)`
    - Persistir

- EliminarSerieComprobanteInputDto
  - Orquestador: EliminarSerieComprobanteUseCase
  - Acciones:
    - Validar que la serie no tenga reservas activas (puerto `INumeracionService` si aplica)
    - `config.EliminarSerie(serieId)`
    - Persistir

- RegistrarUnidadDeMedidaInputDto
  - Orquestador: RegistrarUnidadDeMedidaUseCase
  - Acciones: `config.RegistrarUnidadDeMedida(dto)` → persistir

- RegistrarFormasDePagoInputDto
  - Orquestador: RegistrarFormasDePagoUseCase
  - Acciones: `config.RegistrarFormaDePago(dto)` → persistir

- RegistrarUsuarioEmpresaInputDto / ActualizarPerfilUsuarioEmpresaInputDto
  - Orquestador: RegistrarUsuarioEmpresaUseCase / ActualizarPerfilUsuarioEmpresaUseCase
  - Acciones:
    - `config.RegistrarUsuarioEmpresa(dto)` (valida unicidad username/email)
    - `config.AsignarPerfil(usuarioId, perfilId)` o `perfil.Actualizar(...)`
    - `IConfiguracionRepository.Save(config)` + `Commit`

- HabilitarUsuarioEmpresaInputDto / InhabilitarUsuarioEmpresaInputDto
  - Orquestador: HabilitarUsuarioEmpresaUseCase / InhabilitarUsuarioEmpresaUseCase
  - Acciones: `config.HabilitarUsuario(usuarioId)` o `config.InhabilitarUsuario(usuarioId)` → persistir

- EliminarUsuarioEmpresaInputDto
  - Orquestador: EliminarUsuarioEmpresaUseCase
  - Acciones: `config.EliminarUsuario(usuarioId)` -> persistir. "Soft delete" recomendado según reglas de negocio.

- ConsultarConfiguracionGeneralInputDto
  - Orquestador: ConsultarConfiguracionGeneralUseCase
  - Acciones: `IConfiguracionRepository.GetByEmpresaId(empresaId)` → mapear a `ConsultarConfiguracionGeneralOutputDto`
  - Observación: Read-only, no usar UoW

- ConsultarUsuariosEmpresaInputDto
  - Orquestador: ConsultarUsuariosEmpresaUseCase
  - Acciones: `IConfiguracionRepository.GetUsuarios(empresaId)` o leer `config.Usuarios` y mapear a DTO

---

## Puertos / Servicios externos relevantes
- IConfiguracionRepository: persistencia del agregado (SQL/NoSQL); contratos: GetByEmpresaId, Save, QueryUsuarios, etc.
- IUnitOfWork: confirmar transacciones.
- (Opcional) INumeracionService: cuando las series interactúan con reserva de números o validaciones de numeración.
- (Opcional) IEmailService / IUserDirectory: para notificaciones o sincronización de usuarios.

---

## Contratos mínimos para adaptadores (Adapter guidance)
- Save(config): upsert atómico; enviar cambios de series/usuarios en la misma transacción.
- GetByEmpresaId: devuelve `ConfiguracionEmpresa` con snapshot suficiente para UI (series, establecimientos, usuarios paginados).
- Para queries de usuarios prefiera endpoints específicos: `GetUsuarios(empresaId, filter, paging)`.

---

## Casos de borde / recomendaciones
- Validar unicidad de series por establecimiento y por tipo de comprobante.
- Cuando se elimina una serie, requerir verificación contra `INumeracionService` (números emitidos o reservados).
- Usuarios: preferir soft-delete para auditoría y mantener integridad referencial.
- Permisos/Perfiles: representar internamente como ValueObjects desacoplables para facilitar migraciones.

---

Si quieres, puedo:
- 1) Renderizar el `.mmd` a PNG/SVG y guardarlo en `docs/`.
- 2) Generar diagramas de secuencia (Emitir/RegistrarSerie/RegistrarUsuario) detallados.
- 3) Refinar el diagrama con atributos exactos leyendo las clases de `src/ConfiguracionSistemaBC/Domain`.

Di me cuál prefieres y lo hago a continuación.