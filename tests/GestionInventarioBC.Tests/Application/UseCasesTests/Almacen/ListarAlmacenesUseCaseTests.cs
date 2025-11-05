using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Application.UseCases.Almacen;
using GestionInventarioBC.Tests.TestUtils;
using SharedKernel.ValueObjects;
using NUnit.Framework;

namespace GestionInventarioBC.Tests.Application.UseCasesTests.Almacen
{
	[TestFixture]
	public class ListarAlmacenesUseCaseTests
	{
	private EmpresaId _empresa = EmpresaId.From("emp-test");
	private EstablecimientoId _est1 = EstablecimientoId.From(Guid.NewGuid());
	private EstablecimientoId _est2 = EstablecimientoId.From(Guid.NewGuid());
		private FakeTenantContext _tenant = default!;
		private FakeAlmacenRepository _repo = default!;

		[SetUp]
		public void SetUp()
		{
			_tenant = new FakeTenantContext(_empresa);
			_repo = new FakeAlmacenRepository();
		}

		[Test]
		public async Task ListaPorEstablecimiento_SoloAlmacenesDeEseScope()
		{
			// Arrange
			var ct = CancellationToken.None;
			// Seed varios almacenes en dos establecimientos
			_repo.Ensure(_empresa, _est1, AlmacenId.From(Guid.NewGuid()), "A1");
			_repo.Ensure(_empresa, _est1, AlmacenId.From(Guid.NewGuid()), "A2");
			_repo.Ensure(_empresa, _est2, AlmacenId.From(Guid.NewGuid()), "B1");

			var sut = new ListarAlmacenesUseCase(_repo, _tenant);
			var req = new ListarAlmacenesUseCase.Request(_est1.Value);

			// Act
			var resp = await sut.Handle(req, ct);

			// Assert
			Assert.That(resp.Almacenes.Count, Is.EqualTo(2));
			Assert.That(resp.Almacenes.All(a => a.EstablecimientoId == _est1.Value), Is.True);
		}

		[Test]
		public async Task Lista_Vacio_CuandoNoHayAlmacenes()
		{
			// Arrange
			var ct = CancellationToken.None;
			var sut = new ListarAlmacenesUseCase(_repo, _tenant);

			// Act
			var resp = await sut.Handle(new ListarAlmacenesUseCase.Request(_est1.Value), ct);

			// Assert
			Assert.That(resp.Almacenes.Count, Is.EqualTo(0));
		}
	}
}

