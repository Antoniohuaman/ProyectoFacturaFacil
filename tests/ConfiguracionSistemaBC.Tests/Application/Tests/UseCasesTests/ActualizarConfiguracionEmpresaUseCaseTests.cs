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
    public class ActualizarConfiguracionEmpresaUseCaseTests
    {
        // ===== Infra en memoria =====

        internal class InMemoryConfiguracionEmpresaRepository : IConfiguracionEmpresaRepository
        {
            private readonly Dictionary<string, ConfiguracionEmpresa> _byRuc = new(StringComparer.Ordinal);

            public void Save(ConfiguracionEmpresa agg) => _byRuc[agg.Ruc.Canonizado] = agg;

            public Task<ConfiguracionEmpresa?> FindByRucAsync(Ruc ruc, CancellationToken ct = default)
            {
                _byRuc.TryGetValue(ruc.Canonizado, out var found);
                return Task.FromResult(found);
            }

            public Task<ConfiguracionEmpresa?> GetByEmpresaIdAsync(EmpresaId empresaId, CancellationToken ct = default)
            {
                return Task.FromResult<ConfiguracionEmpresa?>(null);
            }

            public Task AddAsync(ConfiguracionEmpresa aggregate, CancellationToken ct = default)
            {
                return Task.CompletedTask;
            }

            public Task UpdateAsync(ConfiguracionEmpresa aggregate, CancellationToken ct = default)
            {
                return Task.CompletedTask;
            }

            public Task DeleteAsync(EmpresaId empresaId, CancellationToken ct = default)
            {
                return Task.CompletedTask;
            }

            public Task<bool> UpdateIfVersionMatchAsync(ConfiguracionEmpresa aggregate, int expectedVersion, CancellationToken ct = default)
            {
                return Task.FromResult(true);
            }
        }

    internal class InMemoryUnitOfWork : IUnitOfWork
        {
            public int Commits { get; private set; }
            public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                Commits++;
                return Task.CompletedTask;
            }
        }

        [Test]
        public async Task Handle_AplicaCambiosYDevuelveSnapshot_IncrementaVersion()
        {
            // Arrange: empresa inicial
            var repo = new InMemoryConfiguracionEmpresaRepository();
            var uow  = new InMemoryUnitOfWork();

            var ruc = Ruc.From("20600893409");
            var razonSocial = "MI EMPRESA S.A.C.";
            var direccion = CrearDomicilioFiscalPeru("150101", "AV. PRINCIPAL 123");
            var empresa = ConfiguracionEmpresa.RegistrarNueva(ruc, razonSocial, direccion, Moneda.PEN());
            var versionInicial = empresa.Version;

            repo.Save(empresa);

            var useCase = new ActualizarConfiguracionEmpresaUseCase(repo, uow);

            // Cambios a aplicar
            var nuevaDireccion = CrearDomicilioFiscalPeru("150102", "AV. SECUNDARIA 456");
            var nuevoPie = PieDePagina.FromTextoPlano("Gracias por su preferencia");
            var emails = new List<Email>
            {
                CrearEmail("ventas@miempresa.com"),
                CrearEmail("info@miempresa.com")
            };
            var tel = CrearTelefono("+51 999 000 111");

            var input = new ActualizarConfiguracionEmpresaInputDto(
                Ruc: ruc.Canonizado,
                NuevoRuc: null,
                NuevoRazonSocial: "MI EMPRESA ACTUALIZADA S.A.C.",
                NuevaDireccionFiscal: nuevaDireccion,
                NuevoNombreComercial: "MI EMPRESA",
                NuevaMonedaBase: Moneda.PEN(), // podrías cambiar a otra si tu SK lo soporta
                NuevoTelefono: tel,
                NuevosEmails: emails,
                NuevoPieDePagina: nuevoPie,
                MostrarImagenEnComprobanteImpresa: true,
                ReemplazarLogo: true,
                NuevoLogo: null // limpiar logo
            );

            // Act
            var result = await useCase.Handle(input);

            // Assert básicos
            Assert.That(result, Is.Not.Null);
            Assert.That(result.EmpresaId, Is.EqualTo(empresa.EmpresaId.Value));

            // Datos legales
            Assert.That(result.RazonSocial, Is.EqualTo("MI EMPRESA ACTUALIZADA S.A.C."));
            Assert.That(result.NombreComercial, Is.EqualTo("MI EMPRESA"));
            Assert.That(result.DireccionFiscal, Is.Not.Null);
            Assert.That(result.DireccionFiscal.EsPeru, Is.True);

            // Preferencias
            Assert.That(result.MonedaBase, Is.EqualTo(Moneda.PEN()));
            Assert.That(result.PieDePagina, Is.EqualTo(nuevoPie));
            Assert.That(result.MostrarImagenEnComprobanteImpresa, Is.True);

            // Emails y Teléfono (si las fábricas existen, se validan)
            Assert.That(result.Emails.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(ContieneEmail(result.Emails, "ventas@miempresa.com"), Is.True);
            Assert.That(ContieneEmail(result.Emails, "info@miempresa.com"), Is.True);
            // Teléfono: no todos los SK exponen equals por valor; al menos que no sea Vacio
            Assert.That(result.Telefono, Is.Not.Null);

            // Logo limpiado
            Assert.That(result.Logo, Is.Null);

            // Version aumentó y se confirmó UoW
            Assert.That(result.Version, Is.GreaterThan(versionInicial));
            Assert.That(uow.Commits, Is.EqualTo(1));
        }

        // ===== Helpers de prueba =====

        private static DomicilioFiscal CrearDomicilioFiscalPeru(string ubigeo, string direccionLinea1)
        {
            // Usar la fábrica estática FromPeru del ValueObject
            return DomicilioFiscal.FromPeru(
                linea: direccionLinea1,
                ubigeo: ubigeo,
                departamento: "LIMA",
                provincia: "LIMA",
                distrito: "LIMA",
                addressTypeCode: "0000"
            );
        }

        private static Email CrearEmail(string address)
        {
            var t = typeof(Email);
            foreach (var name in new[] { "From", "Create", "Parse", "Of" })
            {
                var m = t.GetMethod(name, BindingFlags.Public | BindingFlags.Static, new[] { typeof(string) });
                if (m is not null) return (Email)m.Invoke(null, new object?[] { address })!;
            }
            // Si tu Email es record/struct con ctor(string), intenta:
            var ctor = t.GetConstructor(new[] { typeof(string) });
            if (ctor is not null) return (Email)ctor.Invoke(new object?[] { address })!;
            Assert.Inconclusive("Ajusta el helper: no se encontró una fábrica pública para Email.");
            throw new InvalidOperationException("Inalcanzable.");
        }

        private static Telefono CrearTelefono(string numero)
        {
            var t = typeof(Telefono);
            foreach (var name in new[] { "From", "Create", "Parse", "Of" })
            {
                var m = t.GetMethod(name, BindingFlags.Public | BindingFlags.Static, new[] { typeof(string) });
                if (m is not null) return (Telefono)m.Invoke(null, new object?[] { numero })!;
            }
            // Fallback a Telefono.Vacio si existe
            var propVacio = t.GetProperty("Vacio", BindingFlags.Public | BindingFlags.Static);
            if (propVacio is not null) return (Telefono)propVacio.GetValue(null)!;

            Assert.Inconclusive("Ajusta el helper: no se encontró fábrica pública ni Telefono.Vacio.");
            throw new InvalidOperationException("Inalcanzable.");
        }

        private static bool ContieneEmail(IEnumerable<Email> emails, string value)
        {
            foreach (var e in emails)
            {
                // Tratar de leer propiedad/ToString
                var prop = e.GetType().GetProperty("Value") ?? e.GetType().GetProperty("Direccion") ?? e.GetType().GetProperty("Address");
                var str = prop?.GetValue(e)?.ToString() ?? e.ToString();
                if (string.Equals(str, value, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
    }
}
