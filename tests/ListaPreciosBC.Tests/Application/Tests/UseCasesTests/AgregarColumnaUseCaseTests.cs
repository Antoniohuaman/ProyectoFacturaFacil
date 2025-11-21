using System;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.DTOs;
using ListaPreciosBC.Application.UseCases;
using ListaPreciosBC.Domain.Aggregates;
using ListaPreciosBC.Domain.Repositories;
using ListaPreciosBC.Domain.ValueObjects;
using NUnit.Framework;
using Moq;
using SharedKernel.Exceptions;
using SharedKernel.Application.Interfaces;   // ITenantContext
using SharedKernel.ValueObjects;             // EmpresaId, TenantId

namespace ListaPreciosBC.Tests.Application.Tests.UseCasesTests
{
    [TestFixture]
    public class AgregarColumnaUseCaseTests
    {
        // ------------------------- Helpers -------------------------

        private static IdentificadorColumnaPrecio P(byte n) => IdentificadorColumnaPrecio.DesdeNumero(n);
        private static NombreColumnaPrecio N(string s)      => NombreColumnaPrecio.Crear(s);

        private static ConfiguracionColumnaPrecio C(
            byte id,
            string nombre,
            bool esBase,
            bool visible,
            byte orden,
            ModoValorizacionColumna? modo = null)
        {
            var m = modo ?? ModoValorizacionColumna.Fijo;
            return ConfiguracionColumnaPrecio.Crear(
                P(id),
                N(nombre),
                m,
                esBase: esBase,
                visible: visible,
                orden: orden);
        }

        private static ListaPrecio NuevaListaPrecioConP1(Guid id)
            => ListaPrecio.CrearConPlantillaPorDefecto(EmpresaId.From("EMP-01"), id);

        private static AgregarColumnaDto Dto(
            Guid listaPrecioId,
            byte numeroColumna = 2,
            string nombre = "Distribuidor",
            bool esBase = false,
            bool visible = true,
            byte orden = 2,
            ModoValorizacionColumna? modo = null,
            string? usuario = "u@test",
            DateTimeOffset? cuando = null)
        {
            return new AgregarColumnaDto
            {
                ListaPrecioId   = listaPrecioId,
                NumeroColumna   = numeroColumna,
                Nombre          = nombre,
                EsBase          = esBase,
                Visible         = visible,
                Orden           = orden,
                Modo            = modo ?? ModoValorizacionColumna.Fijo,
                Usuario         = usuario,
                Cuando          = cuando ?? DateTimeOffset.UtcNow
            };
        }

        // --------------------------- Tests ---------------------------

        [Test]
        public async Task Agrega_columna_con_exito_y_persiste_cambios()
        {
            // Arrange
            var lpId = Guid.NewGuid();
            var agg  = NuevaListaPrecioConP1(lpId); // ya tiene P1 Base (Fijo)
            var repo = new ListaPrecioRepoInMemory();
            repo.Semilla(agg);

            var uow  = new UnitOfWorkInMemory();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var uc   = new AgregarColumnaUseCase(repo, uow, tenant.Object);

            var dto  = Dto(listaPrecioId: lpId, numeroColumna: 2, nombre: "Distribuidor", orden: 2);

            var versionAntes = agg.Version;

            // Act
            var res = await uc.ExecuteAsync(dto, CancellationToken.None);

            // Assert resultado DTO
            Assert.That(res, Is.Not.Null);
            Assert.That(res!.ListaPrecioId, Is.EqualTo(lpId));
            Assert.That(res.IdColumna.Numero, Is.EqualTo(2));
            Assert.That(res.Nombre.Valor, Does.Contain("Distribuidor").IgnoreCase);
            Assert.That(res.Orden, Is.EqualTo(2));
            Assert.That(res.EsBase, Is.False);
            Assert.That(res.Visible, Is.True);
            Assert.That(res.Modo, Is.EqualTo(ModoValorizacionColumna.Fijo));

            // Assert que el repo guardó con la versión esperada (pre-movimiento)
            Assert.That(repo.LastExpectedVersion, Is.EqualTo(versionAntes));

            // Volver a leer y verificar que existe P2
            var recargada = await repo.ObtenerPorIdAsync(lpId, CancellationToken.None);
            Assert.That(recargada, Is.Not.Null);
            Assert.That(recargada!.Plantilla.Existe(P(2)), Is.True);

            // UoW SaveChanges fue invocado
            Assert.That(uow.CommitCount, Is.EqualTo(1));
        }

        [Test]
        public void Lanza_NotFound_si_lista_no_existe()
        {
            // Arrange
            var repo = new ListaPrecioRepoInMemory(); // sin semilla
            var uow  = new UnitOfWorkInMemory();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var uc   = new AgregarColumnaUseCase(repo, uow, tenant.Object);

            var dto  = Dto(listaPrecioId: Guid.NewGuid());

            // Act + Assert
            Assert.That(
                async () => await uc.ExecuteAsync(dto, CancellationToken.None),
                Throws.TypeOf<NotFoundException>()
            );
        }

        [Test]
        public void Lanza_ConcurrencyException_si_la_version_no_coincide()
        {
            // Arrange
            var lpId = Guid.NewGuid();
            var agg  = NuevaListaPrecioConP1(lpId);
            var repo = new ListaPrecioRepoInMemory { ForceConcurrencyConflictNextSave = true };
            repo.Semilla(agg);

            var uow  = new UnitOfWorkInMemory();
            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaId.From("EMP-01"));
            var uc   = new AgregarColumnaUseCase(repo, uow, tenant.Object);

            var dto  = Dto(listaPrecioId: lpId, numeroColumna: 2, nombre: "Mayorista", orden: 2);

            // Act + Assert
            Assert.That(
                async () => await uc.ExecuteAsync(dto, CancellationToken.None),
                Throws.TypeOf<ConcurrencyException>()
            );

            // No debió confirmar cambios
            Assert.That(uow.CommitCount, Is.EqualTo(0));
        }

        // ---------------------- Dobles InMemory (solo tests) ----------------------

    private sealed class ListaPrecioRepoInMemory : IListaPrecioRepository
        {
            private readonly System.Collections.Generic.Dictionary<Guid, ListaPrecio> _store
                = new();
            private readonly System.Collections.Generic.Dictionary<Guid, int> _versionStore
                = new();

            public int? LastExpectedVersion { get; private set; }
            public bool ForceConcurrencyConflictNextSave { get; set; }

            public void Semilla(ListaPrecio lista)
            {
                _store[lista.Id] = lista;
                _versionStore[lista.Id] = lista.Version;
            }

            public Task<ListaPrecio?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default)
            {
                _store.TryGetValue(id, out var v);
                return Task.FromResult(v);
            }

            // Si tu interfaz también tiene ObtenerActivaAsync, agrégala aquí sin uso.
            public Task<ListaPrecio?> ObtenerActivaAsync(EmpresaId empresaId, Guid? sucursalId = null, CancellationToken ct = default)
                => Task.FromResult<ListaPrecio?>(null);

            // Save con control de concurrencia optimista
            public Task GuardarAsync(ListaPrecio agregado, EmpresaId empresaId, Guid? sucursalId, int expectedVersion, CancellationToken ct = default)
            {
                LastExpectedVersion = expectedVersion;

                if (!_store.TryGetValue(agregado.Id, out var actual))
                    throw new NotFoundException("ListaPrecio", agregado.Id.ToString());

                var versionEnAlmacen = _versionStore[agregado.Id];

                // para simular un tercero escribiendo antes de nosotros
                if (ForceConcurrencyConflictNextSave)
                {
                    versionEnAlmacen++; // alguien más guardó
                    _versionStore[agregado.Id] = versionEnAlmacen;
                }

                if (expectedVersion != versionEnAlmacen)
                    throw new ConcurrencyException(
                        aggregate: nameof(ListaPrecio),
                        aggregateId: agregado.Id.ToString(),
                        expectedVersion: expectedVersion,
                        currentVersion: versionEnAlmacen);

                // "Persistir": sobrescribimos y actualizamos versión almacenada
                _store[agregado.Id] = agregado;
                _versionStore[agregado.Id] = agregado.Version;

                return Task.CompletedTask;
            }
        }

        

        private sealed class UnitOfWorkInMemory : ListaPreciosBC.Application.Interfaces.IUnitOfWork
        {
            public int CommitCount { get; private set; }
            public Task CommitAsync(CancellationToken ct = default)
            {
                CommitCount++;
                return Task.CompletedTask;
            }
        }
    }
}
