using System;
using NUnit.Framework;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using SharedKernel.Exceptions;

namespace ConfiguracionSistemaBC.Tests.Domain.ValueObjects
{
    [TestFixture]
    public class EmpresaIdTests
    {
        // ---------------- GUID ----------------

        [Test]
        public void Desde_Guid_valido_normaliza_a_formato_D()
        {
            var g = Guid.Parse("A2B44C33-6C7F-4E2C-9F5E-6B7B6E9F1B2C");

            var id1 = EmpresaId.Desde(g.ToString("N")); // sin guiones
            var id2 = EmpresaId.Desde(g.ToString("B")); // con llaves

            var canonico = g.ToString("D"); // 36 chars con guiones

            Assert.That(id1.Valor, Is.EqualTo(canonico));
            Assert.That(id2.Valor, Is.EqualTo(canonico));
            Assert.That(id1, Is.EqualTo(id2)); // equality por valor
        }

        [Test]
        public void DesdeGuid_con_empty_lanza()
        {
            Assert.That(() => _ = EmpresaId.DesdeGuid(Guid.Empty),
                Throws.TypeOf<BusinessRuleException>());
        }

        [Test]
        public void GenerarNueva_devuelve_un_guid_formato_D()
        {
            var id = EmpresaId.GenerarNueva();

            Assert.That(Guid.TryParse(id.Valor, out _), Is.True);
            Assert.That(id.Valor.Length, Is.EqualTo(36));
            Assert.That(id.Valor[8], Is.EqualTo('-')); // forma "D"
        }

        // ---------------- Código legible ----------------

        [Test]
        public void Desde_codigo_normaliza_upper_y_trim()
        {
            var id = EmpresaId.Desde("  emp-01  ");
            Assert.That(id.Valor, Is.EqualTo("EMP-01"));
            Assert.That(id.ToString(), Is.EqualTo("EMP-01"));
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase(null)]
        public void Desde_codigo_vacio_lanza(string input)
        {
            Assert.That(() => _ = EmpresaId.Desde(input),
                Throws.TypeOf<BusinessRuleException>().With.Message.Contains("obligatorio"));
        }

        [TestCase("-EMP01")]                   // no puede empezar en separador
        [TestCase("A")]                        // < 2 chars
        [TestCase("EMP 01")]                   // espacios
        [TestCase("EMP/01")]                   // char no permitido
        [TestCase("ÁREA")]                     // acentos
        public void Desde_codigo_invalido_lanza(string input)
        {
            Assert.That(() => _ = EmpresaId.Desde(input),
                Throws.TypeOf<BusinessRuleException>().With.Message.Contains("inválido"));
        }

        [Test]
        public void Desde_codigo_largo_mayor_64_lanza()
        {
            var s65 = new string('A', 65);
            Assert.That(() => _ = EmpresaId.Desde(s65),
                Throws.TypeOf<BusinessRuleException>().With.Message.Contains("inválido"));
        }

        // ---------------- DesdeNumero ----------------

        [Test]
        public void DesdeNumero_ok_formato_prefijo_y_padding()
        {
            var id = EmpresaId.DesdeNumero(123, "emp");
            Assert.That(id.Valor, Is.EqualTo("EMP-000123"));
        }

        [Test]
        public void DesdeNumero_con_numero_no_positivo_lanza()
        {
            Assert.That(() => _ = EmpresaId.DesdeNumero(0, "EMP"),
                Throws.TypeOf<BusinessRuleException>());
            Assert.That(() => _ = EmpresaId.DesdeNumero(-1, "EMP"),
                Throws.TypeOf<BusinessRuleException>());
        }

        [Test]
        public void DesdeNumero_con_prefijo_vacio_lanza()
        {
            Assert.That(() => _ = EmpresaId.DesdeNumero(1, "  "),
                Throws.TypeOf<BusinessRuleException>());
        }

        // ---------------- ROOC + correlativo ----------------

        [Test]
        public void DesdeRooc_correlativo_1_devuelve_base_sin_sufijo()
        {
            var id = EmpresaId.DesdeRooc("rooc20", 1);
            Assert.That(id.Valor, Is.EqualTo("ROOC20"));
        }

        [Test]
        public void DesdeRooc_correlativo_mayor_1_agrega_sufijo()
        {
            var id = EmpresaId.DesdeRooc("rooc20", 3);
            Assert.That(id.Valor, Is.EqualTo("ROOC20-3"));
        }

        [Test]
        public void DesdeRooc_validaciones_vacias_o_no_positivas_lanzan()
        {
            Assert.That(() => _ = EmpresaId.DesdeRooc("  ", 1),
                Throws.TypeOf<BusinessRuleException>());
            Assert.That(() => _ = EmpresaId.DesdeRooc("rooc20", 0),
                Throws.TypeOf<BusinessRuleException>());
        }

        [Test]
        public void GenerarParaRooc_usa_servicio_para_correlativo_incremental()
        {
            var svc = new RoocCorrelativoFake();

            var id1 = EmpresaId.GenerarParaRooc("rooc20", svc);
            var id2 = EmpresaId.GenerarParaRooc("rooc20", svc);
            var id3 = EmpresaId.GenerarParaRooc("rooc21", svc);

            Assert.That(id1.Valor, Is.EqualTo("ROOC20"));
            Assert.That(id2.Valor, Is.EqualTo("ROOC20-2"));
            Assert.That(id3.Valor, Is.EqualTo("ROOC21")); // primer workspace de otro ROOC
        }

        // ---------------- TryParse & conversiones ----------------

        [Test]
        public void TryParse_exitoso_para_guid_y_codigo()
        {
            var okGuid = EmpresaId.TryParse("f8b0a7e5-1c2d-4e3f-9a10-112233445566", out var idGuid);
            var okCode = EmpresaId.TryParse("emp_00-1", out var idCode);

            Assert.That(okGuid, Is.True);
            Assert.That(okCode, Is.True);
            Assert.That(idGuid!.Valor, Is.EqualTo("f8b0a7e5-1c2d-4e3f-9a10-112233445566"));
            Assert.That(idCode!.Valor, Is.EqualTo("EMP_00-1"));
        }

        [Test]
        public void TryParse_falla_para_string_invalida()
        {
            var ok = EmpresaId.TryParse("  ", out var id);
            Assert.That(ok, Is.False);
            Assert.That(id, Is.Null);
        }

        [Test]
        public void Conversiones_explicit_e_implicit()
        {
            var id = (EmpresaId)"emp01";  // explicit from string
            string s = EmpresaId.Desde("emp01"); // implicit to string

            Assert.That(id.Valor, Is.EqualTo("EMP01"));
            Assert.That(s, Is.EqualTo("EMP01"));
        }

        // ---------------- Igualdad / helpers ----------------

        [Test]
        public void Igualdad_por_valor_y_EsMismaEmpresaQue()
        {
            var a = EmpresaId.Desde("emp01");
            var b = EmpresaId.Desde("EMP01");
            var c = EmpresaId.Desde("EMP02");

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.EsMismaEmpresaQue(b), Is.True);
            Assert.That(a.EsMismaEmpresaQue(c), Is.False);
        }

        // ===== Fake para correlativo por ROOC =====
        private sealed class RoocCorrelativoFake : IRoocCorrelativoService
        {
            private readonly System.Collections.Generic.Dictionary<string, int> _map = new(StringComparer.OrdinalIgnoreCase);

            public int ObtenerSiguienteCorrelativo(string rooc)
            {
                if (string.IsNullOrWhiteSpace(rooc))
                    throw new ArgumentException("rooc");
                var key = rooc.Trim();
                if (!_map.TryGetValue(key, out var cur))
                {
                    cur = 0;
                }
                cur++;
                _map[key] = cur;
                return cur; // 1, 2, 3...
            }
        }
    }
}
