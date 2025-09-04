using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Application.UseCases;
using ConfiguracionSistemaBC.Application.UseCases.DTOs;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.Repositories;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using NUnit.Framework;
using SharedKernel.ValueObjects;

namespace ConfiguracionSistemaBC.Tests.Application.UseCases
{
    public class ConsultarConfiguracionGeneralUseCaseTests
    {
        // ===== In-memory repo mínimo para la consulta =====
        private sealed class InMemoryConfiguracionEmpresaRepository : IConfiguracionEmpresaRepository
        {
            private readonly Dictionary<string, ConfiguracionEmpresa> _byRuc = new(StringComparer.Ordinal);

            public void Save(ConfiguracionEmpresa agg) => _byRuc[agg.Ruc.Canonizado] = agg;

            public Task<ConfiguracionEmpresa?> FindByRucAsync(Ruc ruc, CancellationToken ct = default)
            {
                _byRuc.TryGetValue(ruc.Canonizado, out var found);
                return Task.FromResult(found);
            }

            public Task<ConfiguracionEmpresa?> GetByEmpresaIdAsync(EmpresaId empresaId, CancellationToken ct = default)
                => throw new NotImplementedException();

            public Task AddAsync(ConfiguracionEmpresa aggregate, CancellationToken ct = default)
                => throw new NotImplementedException();

            public Task UpdateAsync(ConfiguracionEmpresa aggregate, CancellationToken ct = default)
                => throw new NotImplementedException();

            public Task DeleteAsync(EmpresaId empresaId, CancellationToken ct = default)
                => throw new NotImplementedException();

            public Task<bool> UpdateIfVersionMatchAsync(ConfiguracionEmpresa aggregate, int expectedVersion, CancellationToken ct = default)
                => throw new NotImplementedException();
        }

        [Test]
        public async Task Handle_RetornaSnapshotCompleto_ConValoresPorDefectoCorrectos()
        {
            // Arrange
            var repo = new InMemoryConfiguracionEmpresaRepository();

            var ruc = Ruc.From("20600893409");
            var razon = "MI EMPRESA S.A.C.";
            var dirFiscal = CrearDomicilioFiscalPeru();
            var empresa = ConfiguracionEmpresa.RegistrarNueva(
                ruc,
                razon,
                dirFiscal,
                Moneda.PEN());

            // (Opcional) aquí podrías agregar más establecimientos/series si lo necesitas

            repo.Save(empresa);

            var usecase = new ConsultarConfiguracionGeneralUseCase(repo);
            var input = new ConsultarConfiguracionGeneralInputDto(ruc.Canonizado);

            // Act
            var result = await usecase.Handle(input);

            // Assert (perfil y preferencias)
            Assert.That(result, Is.Not.Null);
            Assert.That(result.EmpresaId, Is.EqualTo(empresa.EmpresaId.Value));
            Assert.That(result.Ruc.Canonizado, Is.EqualTo(empresa.Ruc.Canonizado));
            Assert.That(result.RazonSocial, Is.EqualTo(razon));
            Assert.That(result.MonedaBase, Is.EqualTo(Moneda.PEN()));
            Assert.That(result.Ambiente, Is.EqualTo(AmbienteFe.PRUEBA));
            Assert.That(result.MostrarImagenEnComprobanteImpresa, Is.False);

            // Establecimientos (bootstrap: 01 principal)
            Assert.That(result.EstablecimientoPrincipal, Is.Not.Null);
            Assert.That(result.EstablecimientoPrincipal!.Codigo, Is.EqualTo("01"));
            Assert.That(result.Establecimientos.Any(e => e.Codigo == "01"), Is.True);

            // Series (bootstrap FE01/BE01, cada una por defecto para su tipo)
            Assert.That(result.Series.Any(s => s.Serie == "FE01" && s.Tipo == TipoComprobanteCodigo.Factura && s.EsPorDefecto), Is.True);
            Assert.That(result.Series.Any(s => s.Serie == "BE01" && s.Tipo == TipoComprobanteCodigo.Boleta && s.EsPorDefecto), Is.True);

            // Formas de pago (default Contado)
            Assert.That(result.FormasDePago, Is.Not.Empty);
            Assert.That(result.FormaDePagoPorDefecto, Is.Not.Null);
            Assert.That(result.FormaDePagoPorDefecto!.Valor.EsContado, Is.True);
            Assert.That(result.FormasDePago.Any(fp => fp.Nombre.Equals("Efectivo", StringComparison.OrdinalIgnoreCase)), Is.True);

            // Unidades (default NIU / UNIDAD)
            Assert.That(result.UnidadesDeMedida, Is.Not.Empty);
            Assert.That(result.UnidadDeMedidaPorDefecto, Is.Not.Null);
            Assert.That(result.UnidadDeMedidaPorDefecto!.Unidad.Codigo, Is.EqualTo("NIU"));
            Assert.That(result.UnidadesDeMedida.Any(u => u.Nombre.Equals("UNIDAD", StringComparison.OrdinalIgnoreCase)), Is.True);
        }

        // ---- Helper para crear un DomicilioFiscal PE sin conocer tu factory exacta ----
        private static DomicilioFiscal CrearDomicilioFiscalPeru()
        {
            // Usar la fábrica pública con valores válidos para Perú
            return DomicilioFiscal.FromPeru(
                linea: "AV. PRINCIPAL 123",
                ubigeo: "150101",
                departamento: "LIMA",
                provincia: "LIMA",
                distrito: "LIMA",
                addressTypeCode: "0000"
            );
        }
    }
}
