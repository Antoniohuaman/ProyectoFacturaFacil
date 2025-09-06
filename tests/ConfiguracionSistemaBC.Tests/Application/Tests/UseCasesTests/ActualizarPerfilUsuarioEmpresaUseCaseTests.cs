using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

// Use case + DTOs
using ConfiguracionSistemaBC.Application.UseCases;

// Domain
using ConfiguracionSistemaBC.Domain.Aggregates;           // UsuarioEmpresa, RolEmpresa
using ConfiguracionSistemaBC.Domain.Repositories;         // IUsuarioEmpresaRepository, IRolEmpresaRepository, IConfiguracionEmpresaRepository, IUnitOfWork

// Shared Kernel
using SharedKernel.Application.Interfaces;                // ITenantContext
using SharedKernel.ValueObjects;                          // EmpresaId, UsuarioId, NombrePersona, Email, Telefono, EstablecimientoId

namespace ConfiguracionSistemaBC.Application.Tests.UseCases
{
    [TestFixture]
    public class ActualizarPerfilUsuarioEmpresaUseCaseTests
    {
        private Mock<ITenantContext> _tenant = null!;
        private Mock<IUsuarioEmpresaRepository> _usuarioRepo = null!;
        private Mock<IConfiguracionEmpresaRepository> _configRepo = null!;
        private Mock<IRolEmpresaRepository> _rolRepo = null!;
        private Mock<IUnitOfWork> _uow = null!;
        private ActualizarPerfilUsuarioEmpresaUseCase _sut = null!;
    private EmpresaId _empresaId = null!;
    private UsuarioEmpresa _agg = null!;
    private UsuarioId _usuarioId = null!;

        [SetUp]
        public void SetUp()
        {
            _tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            _usuarioRepo = new Mock<IUsuarioEmpresaRepository>(MockBehavior.Strict);
            _configRepo = new Mock<IConfiguracionEmpresaRepository>(MockBehavior.Strict);
            _rolRepo = new Mock<IRolEmpresaRepository>(MockBehavior.Strict);
            _uow = new Mock<IUnitOfWork>(MockBehavior.Strict);

            _empresaId = EmpresaId.From("20600893409");
            _tenant.SetupGet(t => t.EmpresaId).Returns(_empresaId);

            _usuarioId = UsuarioId.New();

            _agg = UsuarioEmpresa.Crear(
                empresaId: _empresaId,
                usuarioId: _usuarioId,
                documento: null,
                nombre: NombrePersona.Crear("Juan", "Pérez"),
                emailContacto: Email.Create("juan.perez@empresa.com"),
                telefonoContacto: Telefono.FromTexto("999 111 222"),
                rolesEmpresaIds: new[] { Guid.NewGuid() },
                accesosIniciales: new[]
                {
                    (EstablecimientoId.New(), (IEnumerable<Guid>)new List<Guid> { Guid.NewGuid() })
                });

            _sut = new ActualizarPerfilUsuarioEmpresaUseCase(
                _tenant.Object, _usuarioRepo.Object, _configRepo.Object, _rolRepo.Object, _uow.Object);
        }

        [Test]
        public async Task Actualiza_Nombre_Telefono_RolesEmpresa_y_Accesos_OK()
        {
            // Arrange
            var nuevoRolEmp = Guid.NewGuid();
            var est1 = EstablecimientoId.New();
            var est2 = EstablecimientoId.New();
            var rolA = Guid.NewGuid();
            var rolB = Guid.NewGuid();

            _usuarioRepo.Setup(r => r.GetAsync(_empresaId, _usuarioId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(_agg);

            // validar establecimientos
            _configRepo.Setup(r => r.EstablecimientoExisteAsync(_empresaId, est1, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(true);
            _configRepo.Setup(r => r.EstablecimientoExisteAsync(_empresaId, est2, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(true);

            // validar roles (sistema o de esta empresa)
            _rolRepo.Setup(r => r.GetByIdAsync(nuevoRolEmp, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(RolEmpresa.CrearSistema("RolSys", new [] { ConfiguracionSistemaBC.Domain.ValueObjects.Permiso.SoloLeer(ConfiguracionSistemaBC.Domain.ValueObjects.Recurso.Usuarios) }));
            _rolRepo.Setup(r => r.GetByIdAsync(rolA, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(RolEmpresa.CrearPersonalizado(_empresaId, "RolA", new [] { ConfiguracionSistemaBC.Domain.ValueObjects.Permiso.CRUD(ConfiguracionSistemaBC.Domain.ValueObjects.Recurso.Usuarios) }));
            _rolRepo.Setup(r => r.GetByIdAsync(rolB, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(RolEmpresa.CrearPersonalizado(_empresaId, "RolB", new [] { ConfiguracionSistemaBC.Domain.ValueObjects.Permiso.CRUD(ConfiguracionSistemaBC.Domain.ValueObjects.Recurso.Comprobantes) }));

            _usuarioRepo.Setup(r => r.UpdateAsync(_agg, 3, It.IsAny<CancellationToken>()))
                        .Returns(Task.CompletedTask);
            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var input = new ActualizarPerfilUsuarioEmpresaInputDto
            {
                UsuarioId = _usuarioId.Value,
                ExpectedVersion = 3,
                Nombres = "Juan Carlos",
                Apellidos = "Pérez Gómez",
                Telefono = "988-777-666",
                RolesEmpresaIds = new List<Guid> { nuevoRolEmp },
                AccesosPorEstablecimiento = new List<ActualizarPerfilUsuarioEmpresaInputDto.AccesoIn>
                {
                    new() { EstablecimientoId = est1.Value, RolIds = new List<Guid>{ rolA } },
                    new() { EstablecimientoId = est2.Value, RolIds = new List<Guid>{ rolA, rolB } }
                }
            };

            // Act
            var outDto = await _sut.HandleAsync(input, CancellationToken.None);

            // Assert
            _usuarioRepo.VerifyAll();
            _configRepo.VerifyAll();
            _rolRepo.VerifyAll();
            _uow.VerifyAll();

            Assert.That(outDto.UsuarioId, Is.EqualTo(_usuarioId.Value));
            Assert.That(outDto.NombreCompleto, Is.EqualTo("Juan Carlos Pérez Gómez"));
            Assert.That(outDto.Email, Is.EqualTo("juan.perez@empresa.com")); // email se mantiene
            Assert.That(outDto.Telefono, Does.Contain("988"));                // actualizado

            // roles empresa reemplazados
            Assert.That(outDto.RolesEmpresaIds, Has.Count.EqualTo(1));
            Assert.That(outDto.RolesEmpresaIds[0], Is.EqualTo(nuevoRolEmp));

            // accesos reemplazados (2 establecimientos)
            Assert.That(outDto.Accesos, Has.Count.EqualTo(2));
            var acc1 = outDto.Accesos.First(a => a.EstablecimientoId == est1.Value);
            Assert.That(acc1.RolIds, Is.EquivalentTo(new[] { rolA }));
            var acc2 = outDto.Accesos.First(a => a.EstablecimientoId == est2.Value);
            Assert.That(acc2.RolIds, Is.EquivalentTo(new[] { rolA, rolB }));
        }

        [Test]
        public async Task Solo_Telefono_Borrar_Telefono_Cuando_Vacio()
        {
            // Arrange
            _usuarioRepo.Setup(r => r.GetAsync(_empresaId, _usuarioId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(_agg);

            _usuarioRepo.Setup(r => r.UpdateAsync(_agg, 1, It.IsAny<CancellationToken>()))
                        .Returns(Task.CompletedTask);
            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var input = new ActualizarPerfilUsuarioEmpresaInputDto
            {
                UsuarioId = _usuarioId.Value,
                ExpectedVersion = 1,
                Telefono = "   " // borra
            };

            // Act
            var outDto = await _sut.HandleAsync(input, CancellationToken.None);

            // Assert
            _usuarioRepo.VerifyAll();
            _uow.VerifyAll();

            Assert.That(outDto.Telefono, Is.EqualTo(string.Empty));
            Assert.That(outDto.NombreCompleto, Is.EqualTo(_agg.Nombre.Completo)); // nombre intacto
            Assert.That(outDto.Email, Is.EqualTo(_agg.EmailContacto.Value));
        }

        [Test]
        public void Falla_si_Rol_de_Otra_Empresa()
        {
            // Arrange
            var otroRol = Guid.NewGuid();
            var otraEmpresa = EmpresaId.From("20123456789");

            _usuarioRepo.Setup(r => r.GetAsync(_empresaId, _usuarioId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(_agg);

            _rolRepo.Setup(r => r.GetByIdAsync(otroRol, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(RolEmpresa.CrearPersonalizado(otraEmpresa, "Externo", new [] { ConfiguracionSistemaBC.Domain.ValueObjects.Permiso.SoloLeer(ConfiguracionSistemaBC.Domain.ValueObjects.Recurso.Usuarios) }));

            var input = new ActualizarPerfilUsuarioEmpresaInputDto
            {
                UsuarioId = _usuarioId.Value,
                ExpectedVersion = 1,
                RolesEmpresaIds = new List<Guid> { otroRol } // pertenece a otra empresa
            };

            // Act
            var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.HandleAsync(input, CancellationToken.None));

            // Assert
            Assert.That(ex!.Message, Does.Contain("pertenece a otra empresa"));
        }

        [Test]
        public void Lanza_si_NoExisteUsuario()
        {
            _usuarioRepo.Setup(r => r.GetAsync(_empresaId, _usuarioId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync((UsuarioEmpresa?)null);

            var input = new ActualizarPerfilUsuarioEmpresaInputDto
            {
                UsuarioId = _usuarioId.Value,
                ExpectedVersion = 1,
                Nombres = "X"
            };

            var ex = Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _sut.HandleAsync(input, CancellationToken.None));

            Assert.That(ex!.Message, Does.Contain("Usuario no encontrado"));
        }

        [Test]
        public void Lanza_si_TenantSinEmpresa()
        {
            var tenantNull = new Mock<ITenantContext>();
            tenantNull.SetupGet(t => t.EmpresaId).Returns((EmpresaId?)null!);

            var sut2 = new ActualizarPerfilUsuarioEmpresaUseCase(
                tenantNull.Object, _usuarioRepo.Object, _configRepo.Object, _rolRepo.Object, _uow.Object);

            var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut2.HandleAsync(new ActualizarPerfilUsuarioEmpresaInputDto
                {
                    UsuarioId = _usuarioId.Value,
                    ExpectedVersion = 1
                }, CancellationToken.None));

            Assert.That(ex!.Message, Does.Contain("EmpresaId no disponible"));
        }

        [Test]
        public void Lanza_si_ReemplazoAccesos_Sin_Roles()
        {
            _usuarioRepo.Setup(r => r.GetAsync(_empresaId, _usuarioId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(_agg);

            var estId = EstablecimientoId.New();

            _configRepo.Setup(r => r.EstablecimientoExisteAsync(_empresaId, estId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(true);

            var input = new ActualizarPerfilUsuarioEmpresaInputDto
            {
                UsuarioId = _usuarioId.Value,
                ExpectedVersion = 1,
                AccesosPorEstablecimiento = new List<ActualizarPerfilUsuarioEmpresaInputDto.AccesoIn>
                {
                    new() { EstablecimientoId = estId.Value, RolIds = new List<Guid>() } // sin roles
                }
            };

            var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.HandleAsync(input, CancellationToken.None));

            Assert.That(ex!.Message, Does.Contain("al menos un rol"));
        }
    }
}
