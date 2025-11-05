using System;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Application.UseCases.Almacen;
using GestionInventarioBC.Tests.TestUtils;
using SharedKernel.ValueObjects;
using NUnit.Framework;

namespace GestionInventarioBC.Tests.Application.UseCasesTests.Almacen
{
	[TestFixture]
	public class ActualizarAlmacenUseCaseTests
	{
	private EmpresaId _empresa = EmpresaId.From("emp-test");
	private EstablecimientoId _est = EstablecimientoId.From(Guid.NewGuid());
	private AlmacenId _alm = AlmacenId.From(Guid.NewGuid());
		private FakeTenantContext _tenant = default!;
		private FakeUnitOfWork _uow = default!;
		private FakeAlmacenRepository _repo = default!;

		[SetUp]
		public void SetUp()
		{
			_tenant = new FakeTenantContext(_empresa);
			_uow = new FakeUnitOfWork();
			_repo = new FakeAlmacenRepository();
		}

		[Test]
		public async Task Renombra_OK()
		{
			// Arrange
			var ct = CancellationToken.None;
			var almacen = GestionInventarioBC.Domain.Aggregates.Almacen.Crear(_empresa, _est, _alm, "Viejo");
			await _repo.GuardarAsync(almacen, ct);

			var sut = new ActualizarAlmacenUseCase(_repo, _tenant, _uow);
			var req = new ActualizarAlmacenUseCase.Request(_est.Value, _alm.Value, "Nuevo");

			// Act
			var resp = await sut.Handle(req, ct);

			// Assert
			Assert.That(resp.Nombre, Is.EqualTo("Nuevo"));
			Assert.That(resp.Version, Is.GreaterThanOrEqualTo(1));
			Assert.That(_uow.CommitCalls, Is.EqualTo(1));
		}

		[Test]
		public void NotFound_CuandoNoExiste()
		{
			// Arrange
			var ct = CancellationToken.None;
			var sut = new ActualizarAlmacenUseCase(_repo, _tenant, _uow);
			var req = new ActualizarAlmacenUseCase.Request(_est.Value, _alm.Value, "Nuevo");

			// Act + Assert
			Assert.That(async () => await sut.Handle(req, ct), Throws.Exception);
			Assert.That(_uow.CommitCalls, Is.EqualTo(0));
		}

		[Test]
		public async Task Rechaza_DatosInvalidos_NombreVacio()
		{
			// Arrange
			var ct = CancellationToken.None;
			var almacen = GestionInventarioBC.Domain.Aggregates.Almacen.Crear(_empresa, _est, _alm, "Viejo");
			await _repo.GuardarAsync(almacen, ct);
			var sut = new ActualizarAlmacenUseCase(_repo, _tenant, _uow);
			var req = new ActualizarAlmacenUseCase.Request(_est.Value, _alm.Value, " ");

			// Act + Assert
			Assert.That(async () => await sut.Handle(req, ct), Throws.Exception);
			Assert.That(_uow.CommitCalls, Is.EqualTo(0));
		}
	}
}

