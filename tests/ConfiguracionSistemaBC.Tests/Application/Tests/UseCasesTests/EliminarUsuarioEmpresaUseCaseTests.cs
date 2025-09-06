using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using NUnit.Framework;
using Moq;

// Use case + DTOs
using ConfiguracionSistemaBC.Application.UseCases;

// Domain
using ConfiguracionSistemaBC.Domain.Aggregates;          // UsuarioEmpresa
using ConfiguracionSistemaBC.Domain.Repositories;        // IUsuarioEmpresaRepository, IUnitOfWork

// Shared Kernel
using SharedKernel.Application.Interfaces;               // ITenantContext
using SharedKernel.ValueObjects;                         // EmpresaId, UsuarioId, NombrePersona, Email

namespace ConfiguracionSistemaBC.Application.Tests.UseCases
{
    [TestFixture]
    public class EliminarUsuarioEmpresaUseCaseTests
    {
        private Mock<ITenantContext> _tenant = null!;
        private Mock<IUsuarioEmpresaRepository> _usuarioRepo = null!;
        private Mock<IUnitOfWork> _uow = null!;
        private EliminarUsuarioEmpresaUseCase _sut = null!;

        private EmpresaId _empresaId = null!;
        private Guid _usuarioGuid;

        [SetUp]
        public void SetUp()
        {
            _tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            _usuarioRepo = new Mock<IUsuarioEmpresaRepository>(MockBehavior.Strict);
            _uow = new Mock<IUnitOfWork>(MockBehavior.Strict);

            _empresaId = EmpresaId.From("20600893409"); // tenant de ejemplo
            _tenant.SetupGet(t => t.EmpresaId).Returns(_empresaId);

            _sut = new EliminarUsuarioEmpresaUseCase(_usuarioRepo.Object, _uow.Object, _tenant.Object);

            _usuarioGuid = Guid.NewGuid();
        }

        private static UsuarioEmpresa CrearInvitado(EmpresaId empresaId, Guid guid, string nombres, string apellidos, string email)
        {
            var usuarioId = UsuarioId.From(guid);
            var nombre = NombrePersona.Crear(nombres, apellidos);
            var mail = Email.Create(email);

            // Crea en estado Invitado (sin acciones relevantes)
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
        public async Task Elimina_OK_SinAccionesRelevantes()
        {
            // Arrange
            var agg = CrearInvitado(_empresaId, _usuarioGuid, "Ana", "García", "ana@example.com");
            var expectedVersion = agg.Version;

            _usuarioRepo
                .Setup(r => r.GetAsync(_empresaId, UsuarioId.From(_usuarioGuid), It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            _usuarioRepo
                .Setup(r => r.DeleteAsync(_empresaId, UsuarioId.From(_usuarioGuid), expectedVersion, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _uow
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var input = new EliminarUsuarioEmpresaInputDto
            {
                UsuarioId = _usuarioGuid,
                ExpectedVersion = expectedVersion
            };

            // Act
            var result = await _sut.HandleAsync(input, CancellationToken.None);

            // Assert
            _usuarioRepo.Verify(r => r.GetAsync(_empresaId, UsuarioId.From(_usuarioGuid), It.IsAny<CancellationToken>()), Times.Once);
            _usuarioRepo.Verify(r => r.DeleteAsync(_empresaId, UsuarioId.From(_usuarioGuid), expectedVersion, It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            Assert.That(result.Eliminado, Is.True);
            Assert.That(result.EmpresaId, Is.EqualTo(_empresaId.Value));
            Assert.That(result.UsuarioId, Is.EqualTo(_usuarioGuid));
        }

        [Test]
        public void Rechaza_SiTieneAccionesRelevantes()
        {
            // Arrange: usuario con acciones relevantes → no puede eliminarse
            var agg = CrearInvitado(_empresaId, _usuarioGuid, "Luis", "Pérez", "luis@example.com");
            agg.MarcarAccionRelevante(); // ahora PuedeSerEliminado == false

            _usuarioRepo
                .Setup(r => r.GetAsync(_empresaId, UsuarioId.From(_usuarioGuid), It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            var input = new EliminarUsuarioEmpresaInputDto
            {
                UsuarioId = _usuarioGuid,
                ExpectedVersion = agg.Version
            };

            // Act + Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _sut.HandleAsync(input, CancellationToken.None));
            Assert.That(ex!.Message, Does.Contain("No se puede eliminar el usuario").And.Contain("inhabilitar"));

            _usuarioRepo.Verify(r => r.DeleteAsync(It.IsAny<EmpresaId>(), It.IsAny<UsuarioId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public void Lanza_SiUsuarioNoExiste()
        {
            // Arrange
            _usuarioRepo
                .Setup(r => r.GetAsync(_empresaId, UsuarioId.From(_usuarioGuid), It.IsAny<CancellationToken>()))
                .ReturnsAsync((UsuarioEmpresa?)null);

            var input = new EliminarUsuarioEmpresaInputDto
            {
                UsuarioId = _usuarioGuid,
                ExpectedVersion = 0
            };

            // Act + Assert
            var ex = Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.HandleAsync(input, CancellationToken.None));
            Assert.That(ex!.Message, Does.Contain("Usuario no encontrado"));

            _usuarioRepo.Verify(r => r.DeleteAsync(It.IsAny<EmpresaId>(), It.IsAny<UsuarioId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public void Propaga_ErrorDeConcurrencia()
        {
            // Arrange
            var agg = CrearInvitado(_empresaId, _usuarioGuid, "María", "Ramos", "maria@example.com");

            _usuarioRepo
                .Setup(r => r.GetAsync(_empresaId, UsuarioId.From(_usuarioGuid), It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            _usuarioRepo
                .Setup(r => r.DeleteAsync(_empresaId, UsuarioId.From(_usuarioGuid), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Versión inesperada (concurrencia)."));

            var input = new EliminarUsuarioEmpresaInputDto
            {
                UsuarioId = _usuarioGuid,
                ExpectedVersion = 999 // incorrecta
            };

            // Act + Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _sut.HandleAsync(input, CancellationToken.None));
            Assert.That(ex!.Message, Does.Contain("Versión inesperada").Or.Contain("concurrencia"));

            _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public void Valida_UsuarioId_NoVacio()
        {
            var input = new EliminarUsuarioEmpresaInputDto
            {
                UsuarioId = Guid.Empty,
                ExpectedVersion = 0
            };

            var ex = Assert.ThrowsAsync<ArgumentNullException>(() => _sut.HandleAsync(input, CancellationToken.None));
            Assert.That(ex!.ParamName, Is.EqualTo("UsuarioId"));
        }
    }
}
