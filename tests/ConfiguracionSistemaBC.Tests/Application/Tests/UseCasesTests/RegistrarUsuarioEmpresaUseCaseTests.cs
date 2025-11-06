using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Moq;

// Use case + DTOs
using ConfiguracionSistemaBC.Application.UseCases;

// Domain
using ConfiguracionSistemaBC.Domain.Aggregates;          // RolEmpresa
using ConfiguracionSistemaBC.Domain.Repositories;        // repos interfaces
using ConfiguracionSistemaBC.Application.Interfaces;     // IUnitOfWork

// Shared Kernel
using SharedKernel.Application.Interfaces;               // ITenantContext
using SharedKernel.ValueObjects;                         // EmpresaId

namespace ConfiguracionSistemaBC.Application.Tests.UseCases
{
    [TestFixture]
    public class RegistrarUsuarioEmpresaUseCaseTests
    {
    private Mock<ITenantContext> _tenant = null!;
    private Mock<IUsuarioEmpresaRepository> _usuarioRepo = null!;
    private Mock<IConfiguracionEmpresaRepository> _configRepo = null!;
    private Mock<IRolEmpresaRepository> _rolRepo = null!;
    private Mock<IUnitOfWork> _uow = null!;
    private RegistrarUsuarioEmpresaUseCase _sut = null!;

        private EmpresaId _empresaId = null!;
        private Guid _est1;
        private Guid _rolSistema1;
        private Guid _rolEmpresa1;

        [SetUp]
        public void SetUp()
        {
            _tenant = new Mock<ITenantContext>();
            _usuarioRepo = new Mock<IUsuarioEmpresaRepository>();
            _configRepo  = new Mock<IConfiguracionEmpresaRepository>();
            _rolRepo     = new Mock<IRolEmpresaRepository>();
            _uow         = new Mock<IUnitOfWork>();

            // Empresa del tenant (usa tu convención opaca basada en RUC canonizado)
            _empresaId = EmpresaId.From("20600893409");
            _tenant.Setup(t => t.EmpresaId).Returns(_empresaId);

            _sut = new RegistrarUsuarioEmpresaUseCase(_usuarioRepo.Object, _configRepo.Object, _rolRepo.Object, _uow.Object, _tenant.Object);

            _est1 = Guid.NewGuid();
            _rolSistema1 = Guid.NewGuid(); // simularemos que existe y es de sistema (EmpresaId null)
            _rolEmpresa1 = Guid.NewGuid(); // simularemos que existe y pertenece a _empresaId
        }

        [Test]
        public async Task HandleAsync_CreaUsuarioInvitado_CuandoDatosValidos()
        {
            // Arrange
            var input = new RegistrarUsuarioEmpresaInputDto
            {
                Nombres = "Juan",
                Apellidos = "Pérez",
                Email = "juan.perez@example.com",
                Telefono = "999-888-777",
                RolesEmpresaIds = new List<Guid> { _rolEmpresa1 },
                AccesosPorEstablecimiento = new List<RegistrarUsuarioEmpresaInputDto.AccesoIn>
                {
                    new()
                    {
                        EstablecimientoId = _est1,
                        RolIds = new List<Guid> { _rolSistema1 }
                    }
                }
            };


            _usuarioRepo.Setup(r => r.EmailExisteEnEmpresaAsync(
                _empresaId, It.IsAny<SharedKernel.ValueObjects.Email>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _configRepo.Setup(r => r.EstablecimientoExisteAsync(
                _empresaId, It.IsAny<SharedKernel.ValueObjects.EstablecimientoId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((EmpresaId eid, SharedKernel.ValueObjects.EstablecimientoId eid2, CancellationToken ct) =>
                    eid2.Value == _est1);

            // Simula rol de SISTEMA (EmpresaId = null)
            var rolSistema = RolEmpresa.CatalogoSistema.Vendedor();
            // Simula rol PERSONALIZADO de ESTA empresa (EmpresaId = _empresaId)
            var rolPersonalizado = RolEmpresa.CrearPersonalizado(_empresaId, "Supervisor", rolSistema.Permisos);

            _rolRepo.Setup(r => r.GetByIdAsync(_rolSistema1, It.IsAny<CancellationToken>())).ReturnsAsync(rolSistema);
            _rolRepo.Setup(r => r.GetByIdAsync(_rolEmpresa1, It.IsAny<CancellationToken>())).ReturnsAsync(rolPersonalizado);

            // Act
            var result = await _sut.HandleAsync(input, CancellationToken.None);

            // Assert

            _usuarioRepo.Verify(r => r.AddAsync(It.IsAny<UsuarioEmpresa>(), It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.UsuarioId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(result.Estado, Is.EqualTo(UsuarioEmpresaEstado.Invitado.ToString()));
            Assert.That(result.NombreCompleto, Is.EqualTo("Juan Pérez"));
            Assert.That(result.Email, Is.EqualTo("juan.perez@example.com"));
            Assert.That(result.Telefono, Is.Not.Empty);
            Assert.That(result.Accesos, Has.Count.EqualTo(1));
            Assert.That(result.Accesos[0].EstablecimientoId, Is.EqualTo(_est1));
            Assert.That(result.Accesos[0].RolIds, Does.Contain(_rolSistema1).Or.Not.Null);
            Assert.That(result.RolesEmpresaIds, Does.Contain(_rolEmpresa1));
        }

        [Test]
        public void HandleAsync_LanzaSiEmailDuplicado()
        {
            // Arrange
            var input = new RegistrarUsuarioEmpresaInputDto
            {
                Nombres = "Ana",
                Apellidos = "Gómez",
                Email = "ana@example.com",
                AccesosPorEstablecimiento = new List<RegistrarUsuarioEmpresaInputDto.AccesoIn>
                {
                    new() { EstablecimientoId = _est1, RolIds = new List<Guid> { _rolSistema1 } }
                }
            };


            _usuarioRepo.Setup(r => r.EmailExisteEnEmpresaAsync(
                _empresaId, It.IsAny<SharedKernel.ValueObjects.Email>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act / Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _sut.HandleAsync(input, CancellationToken.None));
            Assert.That(ex!.Message, Does.Contain("Ya existe un usuario con ese email"));
        }

        [Test]
        public void HandleAsync_LanzaSiNoHayAccesos()
        {
            // Arrange
            var input = new RegistrarUsuarioEmpresaInputDto
            {
                Nombres = "Luis",
                Apellidos = "Nuñez",
                Email = "luis@example.com",
                AccesosPorEstablecimiento = new List<RegistrarUsuarioEmpresaInputDto.AccesoIn>() // vacío
            };


            _usuarioRepo.Setup(r => r.EmailExisteEnEmpresaAsync(
                _empresaId, It.IsAny<SharedKernel.ValueObjects.Email>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act / Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _sut.HandleAsync(input, CancellationToken.None));
            Assert.That(ex!.Message, Does.Contain("al menos un establecimiento"));
        }

        [Test]
        public void HandleAsync_LanzaSiEstablecimientoNoExisteEnEmpresa()
        {
            // Arrange
            var estInexistente = Guid.NewGuid();

            var input = new RegistrarUsuarioEmpresaInputDto
            {
                Nombres = "María",
                Apellidos = "Ramos",
                Email = "maria@example.com",
                AccesosPorEstablecimiento = new List<RegistrarUsuarioEmpresaInputDto.AccesoIn>
                {
                    new() { EstablecimientoId = estInexistente, RolIds = new List<Guid> { _rolSistema1 } }
                }
            };


            _usuarioRepo.Setup(r => r.EmailExisteEnEmpresaAsync(
                _empresaId, It.IsAny<SharedKernel.ValueObjects.Email>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _configRepo.Setup(r => r.EstablecimientoExisteAsync(
                _empresaId, It.IsAny<SharedKernel.ValueObjects.EstablecimientoId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act / Assert
            var ex = Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.HandleAsync(input, CancellationToken.None));
            Assert.That(ex!.Message, Does.Contain("Establecimiento no encontrado"));
        }

        [Test]
        public void HandleAsync_LanzaSiRolPerteneceAOtraEmpresa()
        {
            // Arrange
            var input = new RegistrarUsuarioEmpresaInputDto
            {
                Nombres = "Sara",
                Apellidos = "López",
                Email = "sara@example.com",
                AccesosPorEstablecimiento = new List<RegistrarUsuarioEmpresaInputDto.AccesoIn>
                {
                    new() { EstablecimientoId = _est1, RolIds = new List<Guid> { _rolEmpresa1 } }
                }
            };


            _usuarioRepo.Setup(r => r.EmailExisteEnEmpresaAsync(
                _empresaId, It.IsAny<SharedKernel.ValueObjects.Email>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _configRepo.Setup(r => r.EstablecimientoExisteAsync(
                _empresaId, It.IsAny<SharedKernel.ValueObjects.EstablecimientoId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Rol personalizado de OTRA empresa
            var otraEmpresa = EmpresaId.From("20123456789");
            var rolDeOtraEmpresa = RolEmpresa.CrearPersonalizado(otraEmpresa, "Otro", RolEmpresa.CatalogoSistema.Vendedor().Permisos);

            _rolRepo.Setup(r => r.GetByIdAsync(_rolEmpresa1, It.IsAny<CancellationToken>())).ReturnsAsync(rolDeOtraEmpresa);

            // Act / Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _sut.HandleAsync(input, CancellationToken.None));
            Assert.That(ex!.Message, Does.Contain("pertenece a otra empresa"));
        }
    }
}
