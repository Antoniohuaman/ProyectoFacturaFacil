using System;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Application.UseCases.Almacen;
using GestionInventarioBC.Tests.TestUtils;
using GestionInventarioBC.Domain.Repositories;
using GestionInventarioBC.Application.Interfaces;
using SharedKernel.Application.Interfaces;
using SharedKernel.ValueObjects;
using NUnit.Framework;

namespace GestionInventarioBC.Tests.Application.UseCasesTests.Almacen
{
	[TestFixture]
	public class CrearAlmacenUseCaseTests
	{
	private EmpresaId _empresa = EmpresaId.From("emp-test");
	private EstablecimientoId _est = EstablecimientoId.From(Guid.NewGuid());
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

		private CrearAlmacenUseCase CreateSut() => new CrearAlmacenUseCase(_repo, _tenant, _uow);

		[Test]
		public async Task Crea_OK_RegistraCommit()
		{
			// Arrange
			var ct = CancellationToken.None;
			var almId = Guid.NewGuid();
			var sut = CreateSut();
			var req = new CrearAlmacenUseCase.Request(_est.Value, "Almacen Principal", almId);

			// Act
			var resp = await sut.Handle(req, ct);

			// Assert
			Assert.That(resp.AlmacenId, Is.EqualTo(almId));
			Assert.That(resp.Nombre, Is.EqualTo("Almacen Principal"));
			Assert.That(resp.Activo, Is.True);
			Assert.That(_uow.CommitCalls, Is.EqualTo(1));
		}

		[Test]
		public async Task Rechaza_Duplicado_MismoIdEnEstablecimiento()
		{
			// Arrange
			var ct = CancellationToken.None;
			var almId = Guid.NewGuid();
			// Pre-existente con mismo ID
			var a = GestionInventarioBC.Domain.Aggregates.Almacen.Crear(_empresa, _est, AlmacenId.From(almId), "A1");
			await _repo.GuardarAsync(a, ct);

			var sut = CreateSut();
			var req = new CrearAlmacenUseCase.Request(_est.Value, "Otro", almId);

			// Act + Assert
			Assert.That(async () => await sut.Handle(req, ct), Throws.Exception);
			Assert.That(_uow.CommitCalls, Is.EqualTo(0));
		}

		[Test]
		public void Rechaza_NombreVacio()
		{
			// Arrange
			var ct = CancellationToken.None;
			var sut = CreateSut();
			var req = new CrearAlmacenUseCase.Request(_est.Value, " ");

			// Act + Assert
			Assert.That(async () => await sut.Handle(req, ct), Throws.Exception);
			Assert.That(_uow.CommitCalls, Is.EqualTo(0));
		}
	}
}

