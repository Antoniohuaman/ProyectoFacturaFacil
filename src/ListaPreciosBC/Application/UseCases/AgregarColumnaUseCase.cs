using System;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Domain.Aggregates;
using ListaPreciosBC.Domain.ValueObjects;
using ListaPreciosBC.Application.DTOs;
using ListaPreciosBC.Application.Interfaces;    // IUnitOfWork
using ListaPreciosBC.Domain.Repositories;      // IListaPrecioRepository
using SharedKernel.Exceptions;                 // NotFoundException, ConcurrencyException

namespace ListaPreciosBC.Application.UseCases
{
    /// <summary>
    /// Caso de uso: Agregar una nueva columna a la plantilla de la lista de precios.
    /// </summary>
    public sealed class AgregarColumnaUseCase
    {
        private readonly IListaPrecioRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public AgregarColumnaUseCase(IListaPrecioRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        /// <summary>
        /// Agrega una columna a la plantilla de la lista indicada.
        /// Lanza NotFoundException si la lista no existe y ConcurrencyException si la versión cambió.
        /// </summary>
        public async Task<AgregarColumnaResultDto> ExecuteAsync(AgregarColumnaDto dto, CancellationToken ct = default)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));

            // 1) Cargar agregado
            var listaPrecio = await _repository.ObtenerPorIdAsync(dto.ListaPrecioId, ct);
            if (listaPrecio is null)
                throw new NotFoundException(resource: "ListaPrecio", resourceId: dto.ListaPrecioId.ToString());

            // 2) Capturar versión esperada ANTES de mutar (concurrencia optimista)
            var expectedVersion = listaPrecio.Version;

            // 3) Construir VO de configuración
            var nuevaColumna = ConfiguracionColumnaPrecio.Crear(
                id:     IdentificadorColumnaPrecio.DesdeNumero(dto.NumeroColumna),
                nombre: NombreColumnaPrecio.Crear(dto.Nombre),
                modo:   dto.Modo,               // si tu DTO trae string, mapea aquí al VO
                esBase: dto.EsBase,
                visible:dto.Visible,
                orden:  dto.Orden
            );

            // 4) Mutar agregado (invariantes se validan adentro)
            listaPrecio.AgregarColumna(nuevaColumna, dto.Usuario, dto.Cuando);

            // 5) Guardar + UoW (puede lanzar ConcurrencyException desde la infra)
            await _repository.GuardarAsync(listaPrecio, expectedVersion, ct);
            await _unitOfWork.SaveChangesAsync();

            // 6) Mapear respuesta (recomendado: primitivos, no VOs)
            return new AgregarColumnaResultDto
            {
                ListaPrecioId = listaPrecio.Id,
                // Si tu DTO de salida acepta VOs, lo de abajo compila tal cual:
                IdColumna = nuevaColumna.Id,
                Nombre    = nuevaColumna.Nombre,
                Orden     = nuevaColumna.Orden,
                EsBase    = nuevaColumna.EsBase,
                Visible   = nuevaColumna.Visible,
                Modo      = nuevaColumna.Modo

                // Alternativa (primitivos):
                // IdColumnaNumero = nuevaColumna.Id.Numero,
                // NombreVisible   = nuevaColumna.Nombre.Valor,
                // ModoTexto       = nuevaColumna.Modo.ToString()
            };
        }
    }
}
