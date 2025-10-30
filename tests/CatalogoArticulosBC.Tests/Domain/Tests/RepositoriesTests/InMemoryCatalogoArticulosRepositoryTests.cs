using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

using CatalogoArticulosBC.Adapters.Output.Persistence.InMemory;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace CatalogoArticulosBC.Tests.Domain.Repositories
{
    [TestFixture]
    public class InMemoryCatalogoArticulosRepositoryTests
    {
        // -------------------------------
        // Helpers de fábrica de datos
        // -------------------------------
        private static Moneda PEN() => Moneda.PEN();
        private static NombreProducto NOMBRE(string v = "Producto Test") => new NombreProducto(v);
        private static UnidadDeMedida UDM() => SharedKernel.ValueObjects.UnidadDeMedida.NIU;
        private static AfectacionImpuesto AfectG() => AfectacionImpuesto.Gravado_10;
        private static TasaImpuesto Tasa18() => TasaImpuesto.IGV18;
        private static Categoria CAT(string v = "Varios") => new Categoria(v);
        private static List<EstablecimientoId> Estabs1() => new() { EstablecimientoId.New() };

        private static ProductoSimple NewProducto(EmpresaId empresaId, string skuValor, string nombre = "Producto Test")
        {
            return new ProductoSimple(
                empresaId: empresaId,
                moneda: PEN(),
                sku: Sku.Crear(skuValor),
                nombre: NOMBRE(nombre),
                unidadMedida: UDM(),
                afectacionImpuesto: AfectG(),
                tasaImpuesto: Tasa18(),
                categoria: CAT(),
                establecimientosAsignados: Estabs1()
            );
        }

        [Test]
        public async Task ExisteSkuAsync_DevuelveFalse_ParaOtraEmpresa()
        {
            var repo = new InMemoryCatalogoArticulosRepository();
            var empresaA = EmpresaId.From("20111111111");
            var empresaB = EmpresaId.From("20222222222");

            var pA = NewProducto(empresaA, "SKU-1", "Prod A");
            await repo.AddAsync(pA, CancellationToken.None);

            Assert.That(await repo.ExisteSkuAsync(Sku.Crear("SKU-1"), empresaA, CancellationToken.None), Is.True);
            Assert.That(await repo.ExisteSkuAsync(Sku.Crear("SKU-1"), empresaB, CancellationToken.None), Is.False);
        }

        [Test]
        public async Task GetAllAsync_FiltraPorEmpresa()
        {
            var repo = new InMemoryCatalogoArticulosRepository();
            var empresaA = EmpresaId.From("20111111111");
            var empresaB = EmpresaId.From("20222222222");

            var pA = NewProducto(empresaA, "A-1", "Prod A");
            var pB = NewProducto(empresaB, "B-1", "Prod B");
            await repo.AddAsync(pA, CancellationToken.None);
            await repo.AddAsync(pB, CancellationToken.None);

            var listA = await repo.GetAllAsync(empresaA);
            Assert.That(listA.Count, Is.EqualTo(1));
            Assert.That(listA.Single().Sku.Valor, Is.EqualTo("A-1"));
        }
    }
}
