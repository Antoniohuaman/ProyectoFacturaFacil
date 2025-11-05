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
	public class DeshabilitarAlmacenUseCaseTests
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
		public async Task Deshabilita_OK_Idempotente()
		{
			// Arrange
			var ct = CancellationToken.None;
			var almacen = GestionInventarioBC.Domain.Aggregates.Almacen.Crear(_empresa, _est, _alm, "A1");
			await _repo.GuardarAsync(almacen, ct);

			var sut = new DeshabilitarAlmacenUseCase(_repo, _tenant, _uow);
			var req = new DeshabilitarAlmacenUseCase.Request(_est.Value, _alm.Value);

			// Act
			var resp1 = await sut.Handle(req, ct);
			var resp2 = await sut.Handle(req, ct); // idempotente

			// Assert
			Assert.That(resp1.Activo, Is.False);
			Assert.That(resp2.Activo, Is.False);
			Assert.That(_uow.CommitCalls, Is.EqualTo(2));
		}

		[Test]
		public void NotFound_SiNoExiste()
		{
			// Arrange
			var ct = CancellationToken.None;
			var sut = new DeshabilitarAlmacenUseCase(_repo, _tenant, _uow);
			var req = new DeshabilitarAlmacenUseCase.Request(_est.Value, _alm.Value);

			// Act + Assert
			Assert.That(async () => await sut.Handle(req, ct), Throws.Exception);
			Assert.That(_uow.CommitCalls, Is.EqualTo(0));
		}
	}
}

