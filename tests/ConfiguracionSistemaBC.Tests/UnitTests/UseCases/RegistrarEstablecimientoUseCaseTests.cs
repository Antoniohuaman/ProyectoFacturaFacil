using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Application.UseCases;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.Repositories;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;
using Moq;
using NUnit.Framework;

namespace ConfiguracionSistemaBC.Tests.Application.UseCases
{
    [TestFixture]
    public class RegistrarEstablecimientoUseCaseTests
    {
        // ----------------- Helpers -----------------

        private static RegistrarEstablecimientoUseCase.EstablecimientoParams EstParamsOk(string codigo = "TIENDA1") => new(
            Codigo: codigo,
            Nombre: "TIENDA PRINCIPAL",
            DireccionLinea: "AV. INDUSTRIAL 123",
            Ubigeo: "150101", // Lima-Lima-Lima
            Departamento: "LIMA",
            Provincia: "LIMA",
            Distrito: "LIMA",
            PaisIso: "PE",
            AddressTypeCode: "0000"
        );

        private static ConfiguracionEmpresa NuevaConfig(Guid tenantId)
        {
            return ConfiguracionEmpresa.RegistrarNueva(
                tenantId,
                Ruc.FromString("20100070970"),
                "ACME S.A.C.",
                DireccionPostal.From("AV. INDUSTRIAL 123", "150101", "LIMA", "LIMA", "LIMA"),
                Moneda.PEN()
            );
        }

        private static (Mock<IConfiguracionEmpresaRepository> repo, Mock<IUnitOfWork> uow, RegistrarEstablecimientoUseCase uc)
            BuildUseCase()
        {
            var repo = new Mock<IConfiguracionEmpresaRepository>(MockBehavior.Strict);
            var uow  = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var uc   = new RegistrarEstablecimientoUseCase(repo.Object, uow.Object);
            return (repo, uow, uc);
        }

        // ----------------- Tests -----------------

        [Test]
        public async Task ExecuteAsync_Exito_RegistraNuevoEstablecimiento()
        {
            // Arrange
            var (repo, uow, uc) = BuildUseCase();
            var tenantId = Guid.NewGuid();
            var agg = NuevaConfig(tenantId);

            repo.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

            var p = new RegistrarEstablecimientoUseCase.Params(
                TenantId: tenantId,
                Establecimiento: EstParamsOk("PRIN")
            );

            // Act
            var result = await uc.ExecuteAsync(p, CancellationToken.None);

            // Assert
            Assert.That(result.EstablecimientoId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(result.Codigo, Is.EqualTo("PRIN"));
            Assert.That(result.Nombre, Is.EqualTo("TIENDA PRINCIPAL"));
            Assert.That(result.Ubigeo, Is.EqualTo("150101"));
            Assert.That(result.PaisIso, Is.EqualTo("PE"));
            Assert.That(result.AddressTypeCode, Is.EqualTo("0000"));

            var listado = agg.ListarEstablecimientos();
            Assert.That(listado.Any(x => x.Id == result.EstablecimientoId && x.Codigo == "PRIN"), Is.True);

            repo.Verify(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
            uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            repo.VerifyNoOtherCalls();
            uow.VerifyNoOtherCalls();
        }

        [Test]
        public async Task ExecuteAsync_Exito_TrimDeCodigoYNombre()
        {
            // Arrange
            var (repo, uow, uc) = BuildUseCase();
            var tenantId = Guid.NewGuid();
            var agg = NuevaConfig(tenantId);

            repo.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

            var p = new RegistrarEstablecimientoUseCase.Params(
                TenantId: tenantId,
                Establecimiento: new RegistrarEstablecimientoUseCase.EstablecimientoParams(
                    Codigo: "  ABC ",
                    Nombre: "  Sucursal Centro  ",
                    DireccionLinea: "JR. COMERCIO 456",
                    Ubigeo: "150101",
                    Departamento: "LIMA",
                    Provincia: "LIMA",
                    Distrito: "LIMA"
                )
            );

            // Act
            var result = await uc.ExecuteAsync(p);

            // Assert (debe quedar sin espacios a los lados)
            Assert.That(result.Codigo, Is.EqualTo("ABC"));
            Assert.That(result.Nombre, Is.EqualTo("Sucursal Centro"));

            repo.Verify(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
            uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            repo.VerifyNoOtherCalls();
            uow.VerifyNoOtherCalls();
        }

        [Test]
        public void ExecuteAsync_Error_TenantNoExiste()
        {
            // Arrange
            var (repo, uow, uc) = BuildUseCase();
            var tenantId = Guid.NewGuid();

            repo.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ConfiguracionEmpresa?)null);

            var p = new RegistrarEstablecimientoUseCase.Params(
                TenantId: tenantId,
                Establecimiento: EstParamsOk()
            );

            // Act + Assert
            Assert.That(
                async () => await uc.ExecuteAsync(p),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("aún no existe")
            );

            repo.Verify(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
            repo.VerifyNoOtherCalls();
            uow.VerifyNoOtherCalls();
        }

        [Test]
        public void ExecuteAsync_Error_TenantIdVacio()
        {
            var (repo, uow, uc) = BuildUseCase();

            var p = new RegistrarEstablecimientoUseCase.Params(
                TenantId: Guid.Empty,
                Establecimiento: EstParamsOk()
            );

            Assert.That(
                async () => await uc.ExecuteAsync(p),
                Throws.TypeOf<ArgumentException>().With.Message.Contains("TenantId inválido")
            );

            repo.VerifyNoOtherCalls();
            uow.VerifyNoOtherCalls();
        }

        [Test]
        public void ExecuteAsync_Error_CodigoObligatorio()
        {
            var (repo, uow, uc) = BuildUseCase();
            var tenantId = Guid.NewGuid();
            var agg = NuevaConfig(tenantId);

            var p = new RegistrarEstablecimientoUseCase.Params(
                TenantId: tenantId,
                Establecimiento: EstParamsOk(codigo: "  ") // vacío
            );

            Assert.That(
                async () => await uc.ExecuteAsync(p),
                Throws.TypeOf<ArgumentNullException>().With.Message.Contains("código del establecimiento")
            );

            repo.VerifyNoOtherCalls();
            uow.VerifyNoOtherCalls();
        }

        [Test]
        public void ExecuteAsync_Error_NombreObligatorio()
        {
            var (repo, uow, uc) = BuildUseCase();
            var tenantId = Guid.NewGuid();
            var agg = NuevaConfig(tenantId);

            var sinNombre = new RegistrarEstablecimientoUseCase.EstablecimientoParams(
                Codigo: "XYZ",
                Nombre: "   ", // vacío
                DireccionLinea: "CALLE 1",
                Ubigeo: "150101",
                Departamento: "LIMA",
                Provincia: "LIMA",
                Distrito: "LIMA"
            );

            var p = new RegistrarEstablecimientoUseCase.Params(tenantId, sinNombre);

            Assert.That(
                async () => await uc.ExecuteAsync(p),
                Throws.TypeOf<ArgumentNullException>().With.Message.Contains("nombre del establecimiento")
            );

            repo.VerifyNoOtherCalls();
            uow.VerifyNoOtherCalls();
        }

        [Test]
        public void ExecuteAsync_Error_DireccionInvalida_Ubigeo()
        {
            var (repo, uow, uc) = BuildUseCase();
            var tenantId = Guid.NewGuid();
            var agg = NuevaConfig(tenantId);

            repo.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            var inval = new RegistrarEstablecimientoUseCase.EstablecimientoParams(
                Codigo: "BAD1",
                Nombre: "MALO",
                DireccionLinea: "CALLE 1",
                Ubigeo: "000000", // inválido según VO
                Departamento: "LIMA",
                Provincia: "LIMA",
                Distrito: "LIMA"
            );

            var p = new RegistrarEstablecimientoUseCase.Params(tenantId, inval);

            Assert.That(
                async () => await uc.ExecuteAsync(p),
                Throws.TypeOf<ArgumentOutOfRangeException>().With.Message.Contains("Ubigeo inválido")
            );

            repo.Verify(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
            repo.VerifyNoOtherCalls();
            uow.VerifyNoOtherCalls();
        }

        [Test]
    public void ExecuteAsync_Error_CodigoDuplicado_DesdeAggregate()
        {
            // Arrange: ya existe "PRIN" en el aggregate
            var (repo, uow, uc) = BuildUseCase();
            var tenantId = Guid.NewGuid();
            var agg = NuevaConfig(tenantId);

            var idExistente = agg.RegistrarEstablecimiento(
                codigo: "PRIN",
                nombre: "Principal",
                direccion: DireccionPostal.From("AV. 1", "150101", "LIMA", "LIMA", "LIMA")
            );
            Assert.That(idExistente, Is.Not.EqualTo(Guid.Empty));

            repo.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(agg);

            var p = new RegistrarEstablecimientoUseCase.Params(
                TenantId: tenantId,
                Establecimiento: EstParamsOk("PRIN") // duplicado
            );

            // Act + Assert
            Assert.That(
                async () => await uc.ExecuteAsync(p),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("Ya existe un establecimiento")
            );

            repo.Verify(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
            repo.VerifyNoOtherCalls();
            uow.VerifyNoOtherCalls();
        }
    }
}