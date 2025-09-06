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
using ConfiguracionSistemaBC.Domain.Aggregates;           // UsuarioEmpresa, UsuarioEmpresaEstado
using ConfiguracionSistemaBC.Domain.Repositories;         // IUsuarioEmpresaRepository

// Shared Kernel
using SharedKernel.Application.Interfaces;                // ITenantContext
using SharedKernel.ValueObjects;                          // EmpresaId, UsuarioId, NombrePersona, Email, Telefono, EstablecimientoId

namespace ConfiguracionSistemaBC.Application.Tests.UseCases
{
    [TestFixture]
    public class ConsultarUsuariosEmpresaUseCaseTests
    {
        private Mock<ITenantContext> _tenant = null!;
        private Mock<IUsuarioEmpresaRepository> _repo = null!;
        private ConsultarUsuariosEmpresaUseCase _sut = null!;
        private EmpresaId _empresaId = null!;

        [SetUp]
        public void SetUp()
        {
            _tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            _repo = new Mock<IUsuarioEmpresaRepository>(MockBehavior.Strict);

            _empresaId = EmpresaId.From("20600893409");
            _tenant.SetupGet(t => t.EmpresaId).Returns(_empresaId);

            _sut = new ConsultarUsuariosEmpresaUseCase(_tenant.Object, _repo.Object);
        }

        private static UsuarioEmpresa CrearUsuario(EmpresaId emp, string nombres, string apellidos, string email, UsuarioEmpresaEstado estado,
                                                   IEnumerable<Guid>? rolesEmpresa = null,
                                                   IEnumerable<(EstablecimientoId, IEnumerable<Guid>)>? accesos = null)
        {
            var agg = UsuarioEmpresa.Crear(
                empresaId: emp,
                usuarioId: UsuarioId.New(),
                documento: null,
                nombre: NombrePersona.Crear(nombres, apellidos),
                emailContacto: Email.Create(email),
                telefonoContacto: Telefono.FromTexto("999-888-777"),
                rolesEmpresaIds: rolesEmpresa,
                accesosIniciales: accesos
            );

            // Forzar estado si no es Invitado por defecto
            if (estado == UsuarioEmpresaEstado.Habilitado) agg.MarcarConfirmadoPorIdentidad();
            if (estado == UsuarioEmpresaEstado.Inhabilitado) agg.Inhabilitar("prueba");

            return agg;
        }

        [Test]
        public async Task Lista_por_Estado_y_Paginacion_y_Total_OK()
        {
            // Arrange
            var u1 = CrearUsuario(_empresaId, "Ana", "García", "ana@x.com", UsuarioEmpresaEstado.Habilitado,
                rolesEmpresa: new[] { Guid.NewGuid() },
                accesos: new[]
                {
                    (EstablecimientoId.New(), (IEnumerable<Guid>)new List<Guid> { Guid.NewGuid(), Guid.NewGuid() })
                });

            var u2 = CrearUsuario(_empresaId, "Luis", "Pérez", "luis@x.com", UsuarioEmpresaEstado.Habilitado,
                rolesEmpresa: Array.Empty<Guid>(),
                accesos: new[]
                {
                    (EstablecimientoId.New(), (IEnumerable<Guid>)new List<Guid> { Guid.NewGuid() })
                });

            var input = new ConsultarUsuariosEmpresaInputDto
            {
                Estado = "HABILITADO",
                Page = 1,
                PageSize = 50
            };

            _repo.Setup(r => r.ListAsync(
                    _empresaId,
                    UsuarioEmpresaEstado.Habilitado,
                    null,
                    null,
                    0,
                    50,
                    It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<UsuarioEmpresa> { u1, u2 });

            _repo.Setup(r => r.CountAsync(_empresaId, UsuarioEmpresaEstado.Habilitado, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(2);

            // Act
            var result = await _sut.HandleAsync(input, CancellationToken.None);

            // Assert
            _repo.VerifyAll();
            Assert.That(result.Total, Is.EqualTo(2));
            Assert.That(result.ItemsCount, Is.EqualTo(2));
            Assert.That(result.Items, Has.Count.EqualTo(2));
            Assert.That(result.Items.All(i => i.Estado == "HABILITADO"), Is.True);
            Assert.That(result.Items[0].Email, Is.EqualTo("ana@x.com"));
            Assert.That(result.Items[1].Email, Is.EqualTo("luis@x.com"));
        }

        [Test]
        public async Task Lista_filtrando_por_Establecimiento_y_Rol_NoHaceCount()
        {
            // Arrange
            var estId = EstablecimientoId.New();
            var rolId = Guid.NewGuid();

            var u = CrearUsuario(_empresaId, "María", "Ramos", "maria@x.com", UsuarioEmpresaEstado.Invitado,
                rolesEmpresa: null,
                accesos: new[]
                {
                    (estId, (IEnumerable<Guid>)new List<Guid> { rolId })
                });

            var input = new ConsultarUsuariosEmpresaInputDto
            {
                Estado = null,
                EstablecimientoId = estId.Value,
                RolId = rolId,
                Page = 2,
                PageSize = 10
            };

            _repo.Setup(r => r.ListAsync(
                    _empresaId,
                    null,
                    estId,
                    rolId,
                    10, // skip: (2-1)*10
                    10,
                    It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<UsuarioEmpresa> { u });

            // no se debe llamar CountAsync cuando hay filtro por establecimiento o rol
            _repo.Setup(r => r.CountAsync(It.IsAny<EmpresaId>(), It.IsAny<UsuarioEmpresaEstado?>(), It.IsAny<CancellationToken>()))
                 .Throws(new Exception("CountAsync NO debería llamarse en este escenario."));

            // Act
            var result = await _sut.HandleAsync(input, CancellationToken.None);

            // Assert
            Assert.That(result.Total, Is.Null);
            Assert.That(result.Page, Is.EqualTo(2));
            Assert.That(result.ItemsCount, Is.EqualTo(1));
            Assert.That(result.Items.Single().Email, Is.EqualTo("maria@x.com"));
        }

        [Test]
        public void Valida_Paginacion()
        {
            var bad1 = new ConsultarUsuariosEmpresaInputDto { Page = 0, PageSize = 10 };
            var ex1 = Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _sut.HandleAsync(bad1, CancellationToken.None));
            Assert.That(ex1!.ParamName, Is.EqualTo("Page"));

            var bad2 = new ConsultarUsuariosEmpresaInputDto { Page = 1, PageSize = 0 };
            var ex2 = Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _sut.HandleAsync(bad2, CancellationToken.None));
            Assert.That(ex2!.ParamName, Is.EqualTo("PageSize"));

            var bad3 = new ConsultarUsuariosEmpresaInputDto { Page = 1, PageSize = 1000 };
            var ex3 = Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _sut.HandleAsync(bad3, CancellationToken.None));
            Assert.That(ex3!.ParamName, Is.EqualTo("PageSize"));
        }

        [Test]
        public void Lanza_SiTenantSinEmpresa()
        {
            var tenantNull = new Mock<ITenantContext>();
            tenantNull.SetupGet(t => t.EmpresaId).Returns((EmpresaId?)null!);

            var sut2 = new ConsultarUsuariosEmpresaUseCase(tenantNull.Object, _repo.Object);

            var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut2.HandleAsync(new ConsultarUsuariosEmpresaInputDto { Page = 1, PageSize = 10 }, CancellationToken.None));

            Assert.That(ex!.Message, Does.Contain("EmpresaId no disponible"));
        }
    }
}
