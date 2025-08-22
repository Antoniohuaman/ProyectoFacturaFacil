using System;
using System.Threading.Tasks;
using ListaPreciosBC.Domain.Aggregates;
using ListaPreciosBC.Domain.ValueObjects;
using ListaPreciosBC.Application.DTOs;
using ListaPreciosBC.Application.Interfaces;
using ListaPreciosBC.Domain.Repositories;

namespace ListaPreciosBC.Application.UseCases
{
    /// <summary>
    /// Caso de uso: Agregar una nueva columna a la plantilla de la lista de precios.
    /// </summary>
    public class AgregarColumnaUseCase
    {
        private readonly IListaPrecioRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public AgregarColumnaUseCase(IListaPrecioRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        /// <summary>
        /// Ejecuta el caso de uso para agregar una columna.
        /// </summary>
        public async Task<AgregarColumnaResultDto> ExecuteAsync(AgregarColumnaDto dto)
        {
            // Recuperar el agregado
            var listaPrecio = await _repository.ObtenerPorIdAsync(dto.ListaPrecioId);
            if (listaPrecio == null)
                throw new InvalidOperationException("No se encontró la lista de precios.");

            // Crear configuración de la nueva columna
            var nuevaColumna = ConfiguracionColumnaPrecio.Crear(
                id: IdentificadorColumnaPrecio.DesdeNumero(dto.NumeroColumna),
                nombre: NombreColumnaPrecio.Crear(dto.Nombre),
                modo: dto.Modo,
                esBase: dto.EsBase,
                visible: dto.Visible,
                orden: dto.Orden
            );

            // Agregar columna usando el método del agregado
            listaPrecio.AgregarColumna(nuevaColumna, dto.Usuario, dto.Cuando);

            // Persistir cambios
            await _repository.GuardarAsync(listaPrecio, expectedVersion: 0);
            await _unitOfWork.SaveChangesAsync();

            // Retornar resultado
            return new AgregarColumnaResultDto
            {
                ListaPrecioId = listaPrecio.Id,
                IdColumna = nuevaColumna.Id,
                Nombre = nuevaColumna.Nombre,
                Orden = nuevaColumna.Orden,
                EsBase = nuevaColumna.EsBase,
                Visible = nuevaColumna.Visible,
                Modo = nuevaColumna.Modo
            };
        }
    }
}
