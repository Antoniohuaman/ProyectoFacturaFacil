using System;
using System.Linq;
using System.Threading.Tasks;
using GestionClientesBC.Application.DTOs;
using GestionClientesBC.Application.Interfaces;
using GestionClientesBC.Domain.Exceptions;

namespace GestionClientesBC.Application.UseCases
{
    /// <summary>
    /// Caso de uso para consultar la ficha completa de un cliente.
    /// </summary>
    public class ConsultarClienteUseCase
    {
        private readonly IGestionClientesRepository _clientesRepository;
        private readonly IUserContext _userContext;

        public ConsultarClienteUseCase(
            IGestionClientesRepository clientesRepository,
            IUserContext userContext)
        {
            _clientesRepository = clientesRepository ?? throw new ArgumentNullException(nameof(clientesRepository));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        }

        /// <summary>
        /// Consulta la ficha completa de un cliente por su Id.
        /// </summary>
        public async Task<ConsultarClienteDto> ConsultarAsync(Guid clienteId)
        {
            // 1. Buscar cliente
            var cliente = await _clientesRepository.GetByIdAsync(clienteId);
            if (cliente == null)
                throw new EntityNotFoundException("Cliente no encontrado.");

            // 2. Consultar contactos asociados
            var contactos = await _clientesRepository.ObtenerContactosPorClienteIdAsync(clienteId);

            // 3. Consultar adjuntos asociados
            var adjuntos = await _clientesRepository.ObtenerAdjuntosPorClienteIdAsync(clienteId);

            // 4. Consultar historial de operaciones (últimos 12 meses)
            var desde = DateTime.UtcNow.AddMonths(-12);
            var historial = await _clientesRepository.ObtenerOperacionesPorClienteIdAsync(clienteId, desde);

            // 5. Componer DTO de respuesta
            var dto = new ConsultarClienteDto
            {
                Cliente = ClienteDto.FromEntity(cliente),
                Contactos = contactos?.Select(ContactoClienteDto.FromEntity).ToList() ?? new(),
                Adjuntos = adjuntos?.Select(AdjuntoClienteDto.FromEntity).ToList() ?? new(),
                Historial = historial?.Select(OperacionClienteDto.FromEntity).ToList() ?? new()
            };

            return dto;
        }
    }
}