// BLOQUEADO POR: c:\ProyectoFacturaFacil\src\GestionInventarioBC\Application\UseCases\Transferencias\TransferirEntreAlmacenesUseCase.cs
// Motivo: la clase usa ProductoId? donde se espera ProductoId en llamadas a IStockPorAlmacenRepository.ObtenerAsync y LineaMovimiento.Crear.
// Errores de compilación:
//  - CS1503 en líneas 92, 101, 102, 106, 107 (no se puede convertir de ProductoId? a ProductoId)
// Hasta que se ajuste el UseCase real, estos tests no podrán ejecutarse sin enlazar overrides (prohibido por las restricciones).

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Application.UseCases.Transferencias;
using GestionInventarioBC.Domain.ValueObjects;
using GestionInventarioBC.Tests.TestUtils;
using GestionInventarioBC.Domain.Repositories;
using GestionInventarioBC.Application.Interfaces;
using SharedKernel.Application.Interfaces;
using SharedKernel.ValueObjects;
using NUnit.Framework;

namespace GestionInventarioBC.Tests.Application.UseCasesTests.Transferencias
{
	[TestFixture]
	public class TransferirEntreAlmacenesUseCaseTests
	{
	private EmpresaId _empresa = EmpresaId.From("emp-test");
	private EstablecimientoId _estA = EstablecimientoId.From(Guid.NewGuid());
	private EstablecimientoId _estB = EstablecimientoId.From(Guid.NewGuid());
	private AlmacenId _almA = AlmacenId.From(Guid.NewGuid());
	private AlmacenId _almB = AlmacenId.From(Guid.NewGuid());

		private FakeTenantContext _tenant = default!;
		private FakeUnitOfWork _uow = default!;
		private FakeCatalogoReadModel _catalogo = default!;
		private FakeStockPorAlmacenRepository _stockRepo = default!;
		private FakeMovimientoInventarioRepository _movRepo = default!;

		[SetUp]
		public void SetUp()
		{
			_tenant = new FakeTenantContext(_empresa);
			_uow = new FakeUnitOfWork();
			_catalogo = new FakeCatalogoReadModel();
			_stockRepo = new FakeStockPorAlmacenRepository();
			_movRepo = new FakeMovimientoInventarioRepository();
		}

		private TransferirEntreAlmacenesUseCase CreateSut()
			=> new TransferirEntreAlmacenesUseCase(_stockRepo, _movRepo, _catalogo, _tenant, _uow);

		[Test]
		public async Task FlujoDirecto_MultiplesLineas_TransfiereYRegistraMovimientos()
		{
			// Arrange
			var ct = CancellationToken.None;
			var p1 = ProductoId.New();
			var p2 = ProductoId.New();
			_catalogo.Seed(_empresa.Value, "SKU-1", p1, "Prod 1");
			_catalogo.Seed(_empresa.Value, "SKU-2", p2, "Prod 2");
			// Stock en origen suficiente
			_stockRepo.Ensure(_empresa, _estA, _almA, p1, real: 10m);
			_stockRepo.Ensure(_empresa, _estA, _almA, p2, real: 5m);

			var sut = CreateSut();
			var req = new TransferirEntreAlmacenesUseCase.Request(
				OrigenEstablecimientoId: _estA.Value,
				OrigenAlmacenId: _almA.Value,
				DestinoEstablecimientoId: _estB.Value,
				DestinoAlmacenId: _almB.Value,
				Fecha: DateTimeOffset.UtcNow,
				Lineas: new List<TransferirEntreAlmacenesUseCase.Linea>
				{
					new("SKU-1", null, 3m),
					new(null, p2.Value, 2m),
				}
			);

			// Act
			var resp = await sut.Handle(req, ct);

			// Assert
			Assert.That(resp.MovimientoSalidaId, Is.Not.EqualTo(Guid.Empty));
			Assert.That(resp.MovimientoEntradaId, Is.Not.EqualTo(Guid.Empty));
			// Origen descontado
			var s1Origen = await _stockRepo.ObtenerAsync(_empresa, _estA, _almA, p1, ct);
			var s2Origen = await _stockRepo.ObtenerAsync(_empresa, _estA, _almA, p2, ct);
			Assert.That(s1Origen!.Real.Value, Is.EqualTo(7m)); // 10 - 3
			Assert.That(s2Origen!.Real.Value, Is.EqualTo(3m)); // 5 - 2
			// Destino incrementado (crea si no existe)
			var s1Dest = await _stockRepo.ObtenerAsync(_empresa, _estB, _almB, p1, ct);
			var s2Dest = await _stockRepo.ObtenerAsync(_empresa, _estB, _almB, p2, ct);
			Assert.That(s1Dest!.Real.Value, Is.EqualTo(3m));
			Assert.That(s2Dest!.Real.Value, Is.EqualTo(2m));
			// Commit llamado
			Assert.That(_uow.CommitCalls, Is.EqualTo(1));
		}

		[Test]
		public void Error_SinDisponibilidadEnOrigen_LanzaBusinessRuleException()
		{
			// Arrange
			var ct = CancellationToken.None;
			var p1 = ProductoId.New();
			_catalogo.Seed(_empresa.Value, "SKU-1", p1, "Prod 1");
			// Stock insuficiente (0)
			_stockRepo.Ensure(_empresa, _estA, _almA, p1, real: 0m);

			var sut = CreateSut();
			var req = new TransferirEntreAlmacenesUseCase.Request(
				OrigenEstablecimientoId: _estA.Value,
				OrigenAlmacenId: _almA.Value,
				DestinoEstablecimientoId: _estB.Value,
				DestinoAlmacenId: _almB.Value,
				Fecha: null,
				Lineas: new List<TransferirEntreAlmacenesUseCase.Linea>
				{
					new("SKU-1", null, 1m)
				}
			);

			// Act + Assert
			Assert.That(async () => await sut.Handle(req, ct), Throws.Exception);
			// No commit
			Assert.That(_uow.CommitCalls, Is.EqualTo(0));
		}

		[Test]
		public void Error_MismoOrigenYDestino_LanzaBusinessRuleException()
		{
			// Arrange
			var ct = CancellationToken.None;
			var p1 = ProductoId.New();
			_catalogo.Seed(_empresa.Value, "SKU-1", p1, "Prod 1");
			_stockRepo.Ensure(_empresa, _estA, _almA, p1, real: 5m);

			var sut = CreateSut();
			var req = new TransferirEntreAlmacenesUseCase.Request(
				OrigenEstablecimientoId: _estA.Value,
				OrigenAlmacenId: _almA.Value,
				DestinoEstablecimientoId: _estA.Value,
				DestinoAlmacenId: _almA.Value,
				Fecha: null,
				Lineas: new List<TransferirEntreAlmacenesUseCase.Linea>
				{
					new("SKU-1", null, 1m)
				}
			);

			// Act + Assert
			Assert.That(async () => await sut.Handle(req, ct), Throws.Exception);
			Assert.That(_uow.CommitCalls, Is.EqualTo(0));
		}
	}
}

