using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using NUnit.Framework;
using Moq;

// Use case + DTOs
using ConfiguracionSistemaBC.Application.UseCases;

// Domain
using ConfiguracionSistemaBC.Domain.Aggregates;          // UsuarioEmpresa, UsuarioEmpresaEstado
using ConfiguracionSistemaBC.Domain.Repositories;        // IUsuarioEmpresaRepository
using ConfiguracionSistemaBC.Application.Interfaces;     // IUnitOfWork

// Shared Kernel
using SharedKernel.Application.Interfaces;               // ITenantContext
using SharedKernel.ValueObjects;                         // EmpresaId, UsuarioId, NombrePersona, Email

namespace ConfiguracionSistemaBC.Application.Tests.UseCases
{
    [TestFixture]
    public class HabilitarUsuarioEmpresaUseCaseTests
    {
        private Mock<ITenantContext> _tenant = null!;
        private Mock<IUsuarioEmpresaRepository> _usuarioRepo = null!;
        private Mock<IUnitOfWork> _uow = null!;
        private HabilitarUsuarioEmpresaUseCase _sut = null!;

        private EmpresaId _empresaId = null!;
        private Guid _usuarioGuid;

        [SetUp]
        public void SetUp()
        {
            _tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            _usuarioRepo = new Mock<IUsuarioEmpresaRepository>(MockBehavior.Strict);
            _uow = new Mock<IUnitOfWork>(MockBehavior.Strict);

            _empresaId = EmpresaId.From("20600893409"); // Empresa de ejemplo (tenant actual)
            _tenant.SetupGet(t => t.EmpresaId).Returns(_empresaId);

            _sut = new HabilitarUsuarioEmpresaUseCase(_usuarioRepo.Object, _uow.Object, _tenant.Object);

            _usuarioGuid = Guid.NewGuid();
        }

        private static UsuarioEmpresa CrearUsuarioInvitado(EmpresaId empresaId, Guid guid, string nombres, string apellidos, string email)
        {
            var usuarioId = UsuarioId.From(guid);
            var nombre = NombrePersona.Crear(nombres, apellidos);
            var mail = Email.Create(email);

            // Crea en estado Invitado
            var agg = UsuarioEmpresa.Crear(
                empresaId: empresaId,
                usuarioId: usuarioId,
                documento: null,
                nombre: nombre,
                emailContacto: mail,
                telefonoContacto: null,
                rolesEmpresaIds: null,
                accesosIniciales: null
            );
            return agg;
        }

        [Test]
        public async Task HabilitaUsuario_Inhabilitado_OK()
        {
            // Arrange: usuario inicialmente Invitado → lo inhabilitamos
            var agg = CrearUsuarioInvitado(_empresaId, _usuarioGuid, "Ana", "García", "ana@example.com");
            agg.Inhabilitar("suspensión temporal"); // Version++

            var expectedVersion = agg.Version; // versión observada por el cliente

            _usuarioRepo
                .Setup(r => r.GetAsync(_empresaId, UsuarioId.From(_usuarioGuid), It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            _usuarioRepo
                .Setup(r => r.UpdateAsync(agg, It.Is<int>(v => v == expectedVersion), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _uow
                .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var input = new HabilitarUsuarioEmpresaInputDto
            {
                UsuarioId = _usuarioGuid,
                ExpectedVersion = expectedVersion
            };

            // Act
            var result = await _sut.HandleAsync(input, CancellationToken.None);

            // Assert
            _usuarioRepo.Verify(r => r.GetAsync(_empresaId, UsuarioId.From(_usuarioGuid), It.IsAny<CancellationToken>()), Times.Once);
            _usuarioRepo.Verify(r => r.UpdateAsync(agg, expectedVersion, It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);

            Assert.That(result.UsuarioId, Is.EqualTo(_usuarioGuid));
            Assert.That(result.Estado, Is.EqualTo(UsuarioEmpresaEstado.Habilitado.ToString()));
            Assert.That(result.Version, Is.EqualTo(agg.Version)); // la versión debe reflejar el cambio tras habilitar
        }

        [Test]
        public async Task Idempotente_SiYaEstaHabilitado_NoFalla()
        {
            // Arrange: usuario ya habilitado
            var agg = CrearUsuarioInvitado(_empresaId, _usuarioGuid, "Luis", "Pérez", "luis@example.com");
            agg.MarcarConfirmadoPorIdentidad(); // pasa a Habilitado (Version++)

            var expectedVersion = agg.Version; // versión observada por el cliente

            _usuarioRepo
                .Setup(r => r.GetAsync(_empresaId, UsuarioId.From(_usuarioGuid), It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            _usuarioRepo
                .Setup(r => r.UpdateAsync(agg, It.Is<int>(v => v == expectedVersion), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _uow
                .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var input = new HabilitarUsuarioEmpresaInputDto
            {
                UsuarioId = _usuarioGuid,
                ExpectedVersion = expectedVersion
            };

            // Act
            var result = await _sut.HandleAsync(input, CancellationToken.None);

            // Assert
            _usuarioRepo.Verify(r => r.GetAsync(_empresaId, UsuarioId.From(_usuarioGuid), It.IsAny<CancellationToken>()), Times.Once);
            _usuarioRepo.Verify(r => r.UpdateAsync(agg, expectedVersion, It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);

            Assert.That(agg.Estado, Is.EqualTo(UsuarioEmpresaEstado.Habilitado));
            Assert.That(result.Estado, Is.EqualTo(UsuarioEmpresaEstado.Habilitado.ToString()));
            Assert.That(result.Version, Is.EqualTo(agg.Version));
        }

        [Test]
        public void Lanza_SiUsuarioNoExiste()
        {
            // Arrange
            _usuarioRepo
                .Setup(r => r.GetAsync(_empresaId, UsuarioId.From(_usuarioGuid), It.IsAny<CancellationToken>()))
                .ReturnsAsync((UsuarioEmpresa?)null);

            var input = new HabilitarUsuarioEmpresaInputDto
            {
                UsuarioId = _usuarioGuid,
                ExpectedVersion = 0
            };

            // Act + Assert
            var ex = Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.HandleAsync(input, CancellationToken.None));
            Assert.That(ex!.Message, Does.Contain("Usuario no encontrado"));
            
            _usuarioRepo.Verify(r => r.UpdateAsync(It.IsAny<UsuarioEmpresa>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public void Lanza_ConcurrenciaSiVersionNoCoincide()
        {
            // Arrange
            var agg = CrearUsuarioInvitado(_empresaId, _usuarioGuid, "María", "Ramos", "maria@example.com");

            _usuarioRepo
                .Setup(r => r.GetAsync(_empresaId, UsuarioId.From(_usuarioGuid), It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            _usuarioRepo
                .Setup(r => r.UpdateAsync(agg, It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Versión inesperada (concurrencia)."));

            var input = new HabilitarUsuarioEmpresaInputDto
            {
                UsuarioId = _usuarioGuid,
                ExpectedVersion = 999 // incorrecta
            };

            // Act + Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _sut.HandleAsync(input, CancellationToken.None));
            Assert.That(ex!.Message, Does.Contain("Versión inesperada").Or.Contain("concurrencia"));

            _uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
