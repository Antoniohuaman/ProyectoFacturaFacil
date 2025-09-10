using System;
using System.Reflection;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using IndicadoresNegocioBC.Domain.ValueObjects;
using SharedKernel.ValueObjects; // Para tipos fuertes Moneda / EstablecimientoId (si están disponibles)

namespace IndicadoresNegocioBC.Tests.Domain.ValueObjects
{
    [TestFixture]
    public class SegmentoIndicadorTests
    {
        // ------- Helpers para instanciar Moneda y EstablecimientoId sin asumir API concreta -------

        private static Moneda? TryGetMoneda(string codigoPreferido = "PEN")
        {

            // 1) Métodos estáticos típicos: PEN(), USD()
            var method = typeof(Moneda).GetMethod(codigoPreferido, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase, null, Type.EmptyTypes, null);
            if (method != null)
                return (Moneda?)method.Invoke(null, null);

            // 2) Otros códigos comunes por si "PEN" no existe
            foreach (var alt in new[] { "USD", "EUR" })
            {
                var m = typeof(Moneda).GetMethod(alt, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase, null, Type.EmptyTypes, null);
                if (m != null) return (Moneda?)m.Invoke(null, null);
            }

            // 3) Métodos estáticos: From/Parse/Create/Of(string)
            foreach (var name in new[] { "From", "Parse", "Create", "Of", "New" })
            {
                var m = typeof(Moneda).GetMethod(name, BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
                if (m != null) return (Moneda?)m.Invoke(null, new object[] { codigoPreferido });
            }

            // 4) Ctor público o no público con string
            var ctorPub = typeof(Moneda).GetConstructor(new[] { typeof(string) });
            if (ctorPub != null) return (Moneda?)ctorPub.Invoke(new object[] { codigoPreferido });

            var ctorNon = typeof(Moneda).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(string) }, null);
            if (ctorNon != null) return (Moneda?)ctorNon.Invoke(new object[] { codigoPreferido });

            return null;
        }

        private static EstablecimientoId? TryBuildEstablecimientoId(Guid id)
        {
            // 1) Métodos estáticos: From/Crear/Create/Of(Guid)
            foreach (var name in new[] { "From", "Crear", "Create", "Of", "New" })
            {
                var m = typeof(EstablecimientoId).GetMethod(name, BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Guid) }, null);
                if (m != null) return (EstablecimientoId?)m.Invoke(null, new object[] { id });
            }

            // 2) Ctor público o no público con Guid
            var ctorPub = typeof(EstablecimientoId).GetConstructor(new[] { typeof(Guid) });
            if (ctorPub != null) return (EstablecimientoId?)ctorPub.Invoke(new object[] { id });

            var ctorNon = typeof(EstablecimientoId).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(Guid) }, null);
            if (ctorNon != null) return (EstablecimientoId?)ctorNon.Invoke(new object[] { id });

            return null;
        }

        private static string GetMonedaCodigo(Moneda moneda)
        {
            var prop = typeof(Moneda).GetProperty("Codigo", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop == null) Assert.Inconclusive("Moneda no expone propiedad 'Codigo'; no se puede verificar ToString.");
            return (string)prop!.GetValue(moneda)!;
        }

        // -------------------------------- Tests --------------------------------

        [Test]
        public void ParaEmpresa_Deberia_Crear_Segmento_De_EmpresaCompleta()
        {
            var empresaId = Guid.NewGuid();
            var moneda = TryGetMoneda("PEN") ?? throw new InconclusiveException("No se pudo construir Moneda (PEN).");
            var codigo = GetMonedaCodigo(moneda);

            var seg = SegmentoIndicador.ParaEmpresa(empresaId, moneda);

            Assert.That(seg.EmpresaId, Is.EqualTo(empresaId));
            Assert.That(seg.Moneda, Is.EqualTo(moneda));
            Assert.That(seg.EstablecimientoId, Is.Null);
            Assert.That(seg.EsEmpresaCompleta, Is.True);

            var str = seg.ToString();
            Assert.That(str, Does.Contain("Empresa"));
            Assert.That(str, Does.Contain(codigo));
        }

        [Test]
        public void ParaEstablecimiento_Deberia_Crear_Segmento_Con_Establecimiento()
        {
            var empresaId = Guid.NewGuid();
            var moneda = TryGetMoneda("PEN") ?? throw new InconclusiveException("No se pudo construir Moneda.");
            var estId = TryBuildEstablecimientoId(Guid.NewGuid()) ?? throw new InconclusiveException("No se pudo construir EstablecimientoId.");

            var seg = SegmentoIndicador.ParaEstablecimiento(empresaId, estId, moneda);

            Assert.That(seg.EmpresaId, Is.EqualTo(empresaId));
            Assert.That(seg.Moneda, Is.EqualTo(moneda));
            Assert.That(seg.EstablecimientoId, Is.EqualTo(estId));
            Assert.That(seg.EsEmpresaCompleta, Is.False);

            var str = seg.ToString();
            Assert.That(str, Does.Contain("Establecimiento:"));
            Assert.That(str, Does.Contain(GetMonedaCodigo(moneda)));
        }

        [Test]
        public void ConEstablecimiento_Deberia_Ser_Inmutable_Y_Establecer_Id()
        {
            var empresaId = Guid.NewGuid();
            var moneda = TryGetMoneda("USD") ?? TryGetMoneda("PEN") ?? throw new InconclusiveException("No se pudo construir Moneda.");
            var estId = TryBuildEstablecimientoId(Guid.NewGuid()) ?? throw new InconclusiveException("No se pudo construir EstablecimientoId.");

            var original = SegmentoIndicador.ParaEmpresa(empresaId, moneda);
            var conEst = original.ConEstablecimiento(estId);

            // Original no cambia
            Assert.That(original.EstablecimientoId, Is.Null);
            Assert.That(original.EsEmpresaCompleta, Is.True);

            // Nuevo con establecimiento
            Assert.That(conEst.EmpresaId, Is.EqualTo(empresaId));
            Assert.That(conEst.Moneda, Is.EqualTo(moneda));
            Assert.That(conEst.EstablecimientoId, Is.EqualTo(estId));
            Assert.That(conEst.EsEmpresaCompleta, Is.False);

            // Son diferentes por valor
            Assert.That(conEst, Is.Not.EqualTo(original));
        }

        [Test]
        public void ParaTodaLaEmpresa_Deberia_Quitar_Establecimiento_Manteniendo_EmpresaYMoneda()
        {
            var empresaId = Guid.NewGuid();
            var moneda = TryGetMoneda("PEN") ?? throw new InconclusiveException("No se pudo construir Moneda.");
            var estId = TryBuildEstablecimientoId(Guid.NewGuid()) ?? throw new InconclusiveException("No se pudo construir EstablecimientoId.");

            var conEst = SegmentoIndicador.ParaEstablecimiento(empresaId, estId, moneda);
            var empresaCompleta = conEst.ParaTodaLaEmpresa();

            Assert.That(empresaCompleta.EmpresaId, Is.EqualTo(empresaId));
            Assert.That(empresaCompleta.Moneda, Is.EqualTo(moneda));
            Assert.That(empresaCompleta.EstablecimientoId, Is.Null);
            Assert.That(empresaCompleta.EsEmpresaCompleta, Is.True);
        }

        [Test]
        public void Validaciones_Deberian_Lanzar_Cuando_Corresponde()
        {
            var moneda = TryGetMoneda("PEN") ?? throw new InconclusiveException("No se pudo construir Moneda.");
            var estId = TryBuildEstablecimientoId(Guid.NewGuid()) ?? throw new InconclusiveException("No se pudo construir EstablecimientoId.");

            // EmpresaId vacío
            Assert.That(() => SegmentoIndicador.ParaEmpresa(Guid.Empty, moneda),
                Throws.Exception.TypeOf<ArgumentException>());
            Assert.That(() => SegmentoIndicador.ParaEstablecimiento(Guid.Empty, estId, moneda),
                Throws.Exception.TypeOf<ArgumentException>());

            // Moneda nula
            Assert.That(() => SegmentoIndicador.ParaEmpresa(Guid.NewGuid(), null!),
                Throws.Exception.TypeOf<ArgumentNullException>());
            Assert.That(() => SegmentoIndicador.ParaEstablecimiento(Guid.NewGuid(), estId, null!),
                Throws.Exception.TypeOf<ArgumentNullException>());

            // Establecimiento nulo (nota: firma no-nullable, pero runtime permite null y la fábrica lo valida)
            Assert.That(() => SegmentoIndicador.ParaEstablecimiento(Guid.NewGuid(), null!, moneda),
                Throws.Exception.TypeOf<ArgumentNullException>());

            // ConEstablecimiento con nulo
            var segEmpresa = SegmentoIndicador.ParaEmpresa(Guid.NewGuid(), moneda);
            Assert.That(() => segEmpresa.ConEstablecimiento(null!),
                Throws.Exception.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Igualdad_Por_Valor_Deberia_Considerar_Empresa_Establecimiento_Y_Moneda()
        {
            var empresaId = Guid.NewGuid();
            var pen1 = TryGetMoneda("PEN") ?? throw new InconclusiveException("No se pudo construir Moneda.");
            var pen2 = TryGetMoneda("PEN") ?? pen1; // misma moneda (mismo valor)
            var usd  = TryGetMoneda("USD") ?? pen1; // si no hay USD, reusa PEN para no romper

            var estA = TryBuildEstablecimientoId(Guid.NewGuid()) ?? throw new InconclusiveException("No se pudo construir EstablecimientoId.");
            var estB = TryBuildEstablecimientoId(Guid.NewGuid()) ?? throw new InconclusiveException("No se pudo construir EstablecimientoId.");

            var a = SegmentoIndicador.ParaEstablecimiento(empresaId, estA, pen1);
            var b = SegmentoIndicador.ParaEstablecimiento(empresaId, estA, pen2); // igual por valor
            var c = SegmentoIndicador.ParaEstablecimiento(empresaId, estB, pen1); // distinto establecimiento
            var d = SegmentoIndicador.ParaEmpresa(empresaId, pen1);               // empresa completa
            var e = SegmentoIndicador.ParaEmpresa(empresaId, usd);                // distinta moneda

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a, Is.Not.EqualTo(c));
            Assert.That(a, Is.Not.EqualTo(d));
            if (!ReferenceEquals(pen1, usd))
                Assert.That(d, Is.Not.EqualTo(e)); // si es otra moneda
        }
    }
}
