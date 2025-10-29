using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CatalogoArticulosBC.Adapters.Output.Persistence.InMemory;
using CatalogoArticulosBC.Application.UseCases.ListarProductos;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.Filters;
using CatalogoArticulosBC.Domain.ValueObjects;
using NUnit.Framework;
using SharedKernel.Application.Interfaces;
using SharedKernel.ValueObjects;

namespace CatalogoArticulosBC.Tests.Application.UseCases
{
    public class MultiEmpresaIsolationTests
    {
        private static Moneda PEN() => Moneda.PEN();
        private static AfectacionImpuesto Afectacion() => AfectacionImpuesto.Gravado_10;
        private static TasaImpuesto IGV18() => TasaImpuesto.IGV18;
        private static UnidadDeMedida Udm() => UnidadDeMedida.From("NIU");
        private static Categoria Cat(string nombre = "BEBIDAS") => new(nombre);
        private static System.Collections.Generic.List<EstablecimientoId> Ests() => new() { EstablecimientoId.New() };
        private static NombreProducto Np(string v) => new(v);

        private static ProductoSimple P(EmpresaId empresaId, string sku, string nombre, string categoria, bool habilitado = true)
        {
            var p = new ProductoSimple(
                empresaId: empresaId,
                moneda: PEN(),
                sku: Sku.Crear(sku),
                nombre: Np(nombre),
                unidadMedida: Udm(),
                afectacionImpuesto: Afectacion(),
                tasaImpuesto: IGV18(),
                categoria: new Categoria(categoria),
                establecimientosAsignados: Ests(),
                descripcion: "desc"
            );
            if (!habilitado) p.Deshabilitar("test");
            return p;
        }

        private sealed class TestTenant : ITenantContext
        {
            public EmpresaId EmpresaId { get; }
            public TestTenant(string ruc) { EmpresaId = EmpresaId.From(ruc); }
        }

        [Test]
        public async Task ExisteSkuAsync_respeta_empresa()
        {
            var repo = new InMemoryCatalogoArticulosRepository();
            var empresaA = EmpresaId.From("20111111111");
            var empresaB = EmpresaId.From("20222222222");
            var sku = Sku.Crear("SKU-ISO-1");

            // Solo agregamos en empresa B
            await repo.AddAsync(P(empresaB, sku.Valor, "Prod B", "CAT"), CancellationToken.None);

            // Existe para B
            var existsB = await repo.ExisteSkuAsync(sku, empresaB, CancellationToken.None);
            // No debe existir para A
            var existsA = await repo.ExisteSkuAsync(sku, empresaA, CancellationToken.None);

            Assert.That(existsB, Is.True);
            Assert.That(existsA, Is.False);
        }

        [Test]
        public async Task ListarProductosUseCase_filtra_por_empresa()
        {
            var repo = new InMemoryCatalogoArticulosRepository();
            var empresaA = EmpresaId.From("20123456789");
            var empresaB = EmpresaId.From("20987654321");

            // Seed A
            await repo.AddAsync(P(empresaA, "A-001", "Agua", "BEBIDAS"), CancellationToken.None);
            await repo.AddAsync(P(empresaA, "A-002", "Aceite", "ABARROTES"), CancellationToken.None);

            // Seed B
            await repo.AddAsync(P(empresaB, "B-001", "Bujia", "AUTO"), CancellationToken.None);

            var tenantA = new TestTenant("20123456789");
            var useCase = new ListarProductosUseCase(repo, tenantA);

            var output = await useCase.ExecuteAsync(new ListarProductosInputDto
            {
                Page = 1,
                PageSize = 50
            });

            // Debe listar solo los de A
            Assert.That(output.Items.Length, Is.EqualTo(2));
            Assert.That(output.Items.Select(i => i.Sku), Is.EquivalentTo(new[] { "A-001", "A-002" }));
            Assert.That(output.EmpresaId, Is.EqualTo("20123456789"));
        }
    }
}
