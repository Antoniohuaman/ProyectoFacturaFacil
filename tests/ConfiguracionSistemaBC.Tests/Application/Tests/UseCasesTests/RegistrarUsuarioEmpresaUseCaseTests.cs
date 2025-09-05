using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Application.DTOs;
using ConfiguracionSistemaBC.Application.UseCases;
using ConfiguracionSistemaBC.Domain.Repositories;
using Moq;
using NUnit.Framework;
using NUnit.Framework;
using SharedKernel.Application.Interfaces;
using SharedKernel.ValueObjects;

namespace ConfiguracionSistemaBC.Tests.Application.UseCases
{
    [TestFixture]
    public class RegistrarUsuarioEmpresaUseCaseTests
    {
    private Mock<ITenantContext>? _tenant;
    private Mock<IUsuarioEmpresaRepository>? _repo;
    private Mock<IUnitOfWork>? _uow;

    private RegistrarUsuarioEmpresaUseCase? _sut;

        [SetUp]
        public void SetUp()
        {
            _tenant = new Mock<ITenantContext>();
            _repo = new Mock<IUsuarioEmpresaRepository>();
            _uow = new Mock<IUnitOfWork>();

            _sut = new RegistrarUsuarioEmpresaUseCase(_tenant.Object, _repo.Object, _uow.Object);
        }

        [Test]
        public async Task CreaUsuarioInvitado_ConAccesosYRoles_CuandoEmailNoExiste()
        {
            // Arrange
            var empresaId = EmpresaId.From("empresa-20600893409");
            _tenant!.Setup(t => t.EmpresaId).Returns(empresaId);

            _repo!.Setup(r => r.EmailExisteEnEmpresaAsync(
                It.Is<EmpresaId>(e => e.Value == empresaId.Value),
                It.IsAny<Email>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _uow!.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            RegistrarUsuarioEmpresaInputDto input = new()
            {
                Email = "nuevo.user@demo.test",
                NombreCompleto = "Nuevo User",
                Celular = "999888777",
                Accesos = new List<RegistrarUsuarioEmpresaInputDto.AccesoItem>
                {
                    new()
                    {
                        EstablecimientoId = Guid.NewGuid(),
                        RolIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }
                    }
                }
            };

            // Act
            var result = await _sut!.ExecuteAsync(input);

            // Assert (output)
            Assert.That(result, Is.Not.Null);
            Assert.That(result.EmpresaId, Is.EqualTo(empresaId.Value));
            Assert.That(result.Email, Is.EqualTo(input.Email));
            Assert.That(result.UsuarioId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(result.Estado, Is.EqualTo("Invitado")); // Estado inicial del aggregate
            Assert.That(result.Accesos, Has.Count.EqualTo(1));
            Assert.That(result.Accesos[0].EstablecimientoId, Is.EqualTo(input.Accesos[0].EstablecimientoId));
            Assert.That(result.Accesos[0].RolIds, Is.SupersetOf(input.Accesos[0].RolIds));

            // Assert (interacciones)
            _repo!.Verify(r => r.EmailExisteEnEmpresaAsync(
                It.Is<EmpresaId>(e => e.Value == empresaId.Value),
                It.Is<Email>(m => m.Value == input.Email),
                It.IsAny<CancellationToken>()), Times.Once);

            _repo!.Verify(r => r.AddAsync(It.IsAny<ConfiguracionSistemaBC.Domain.Aggregates.UsuarioEmpresa>(), It.IsAny<CancellationToken>()), Times.Once);
            _uow!.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void LanzaExcepcion_CuandoEmailVacio()
        {
            // Arrange
            _tenant!.Setup(t => t.EmpresaId).Returns(EmpresaId.From("empresa-20600893409"));

            var input = new RegistrarUsuarioEmpresaInputDto
            {
                Email = "", // vacío
                Accesos = new List<RegistrarUsuarioEmpresaInputDto.AccesoItem>
                {
                    new()
                    {
                        EstablecimientoId = Guid.NewGuid(),
                        RolIds = new List<Guid> { Guid.NewGuid() }
                    }
                }
            };

            // Act + Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => await _sut!.ExecuteAsync(input));
            Assert.That(ex!.Message, Does.Contain("email").IgnoreCase);
        }

        [Test]
        public void LanzaExcepcion_CuandoSinAccesos()
        {
            // Arrange
            _tenant!.Setup(t => t.EmpresaId).Returns(EmpresaId.From("empresa-20600893409"));

            var input = new RegistrarUsuarioEmpresaInputDto
            {
                Email = "ok@demo.test",
                Accesos = new List<RegistrarUsuarioEmpresaInputDto.AccesoItem>() // vacía
            };

            // Act + Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => await _sut!.ExecuteAsync(input));
            Assert.That(ex!.Message, Does.Contain("al menos un establecimiento").IgnoreCase);
        }

        [Test]
        public void LanzaExcepcion_CuandoAccesoSinRoles()
        {
            // Arrange
            _tenant!.Setup(t => t.EmpresaId).Returns(EmpresaId.From("empresa-20600893409"));

            var input = new RegistrarUsuarioEmpresaInputDto
            {
                Email = "ok@demo.test",
                Accesos = new List<RegistrarUsuarioEmpresaInputDto.AccesoItem>
                {
                    new()
                    {
                        EstablecimientoId = Guid.NewGuid(),
                        RolIds = new List<Guid>() // sin roles
                    }
                }
            };

            // Act + Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => await _sut!.ExecuteAsync(input));
            Assert.That(ex!.Message, Does.Contain("al menos un RolId").IgnoreCase);
        }

        [Test]
        public void LanzaExcepcion_CuandoEmailDuplicado()
        {
            // Arrange
            var empresaId = EmpresaId.From("empresa-20600893409");
            _tenant!.Setup(t => t.EmpresaId).Returns(empresaId);

            _repo!.Setup(r => r.EmailExisteEnEmpresaAsync(
                It.IsAny<EmpresaId>(),
                It.IsAny<Email>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(true); // ya existe

            var input = new RegistrarUsuarioEmpresaInputDto
            {
                Email = "repetido@demo.test",
                Accesos = new List<RegistrarUsuarioEmpresaInputDto.AccesoItem>
                {
                    new()
                    {
                        EstablecimientoId = Guid.NewGuid(),
                        RolIds = new List<Guid> { Guid.NewGuid() }
                    }
                }
            };

            // Act + Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await _sut!.ExecuteAsync(input));
            Assert.That(ex!.Message, Does.Contain("ya existe").IgnoreCase);

            _repo!.Verify(r => r.AddAsync(It.IsAny<ConfiguracionSistemaBC.Domain.Aggregates.UsuarioEmpresa>(), It.IsAny<CancellationToken>()), Times.Never);
            _uow!.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
