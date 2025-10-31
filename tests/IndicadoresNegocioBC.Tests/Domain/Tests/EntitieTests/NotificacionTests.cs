using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using IndicadoresNegocioBC.Domain.Entities;
using IndicadoresNegocioBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace IndicadoresNegocioBC.Tests.Domain.Entities
{
    [TestFixture]
    public class NotificacionTests
    {
        // ===================== Helpers para crear VOs robustamente =====================
        // Intenta patrones comunes: static From/Create/Parse/Of/New (Guid o string),
        // o constructores públicos/no públicos (Guid o string).
        private static T CreateId<T>()
        {
            var t = typeof(T);
            var g = Guid.NewGuid();
            var s = g.ToString("N");

            // 1) Métodos estáticos más comunes
            var staticFactories = new[] { "From", "Create", "Parse", "Of", "New" };
            foreach (var name in staticFactories)
            {
                // Guid
                var mGuid = t.GetMethod(name, BindingFlags.Public | BindingFlags.Static, new[] { typeof(Guid) });
                if (mGuid != null) return (T)mGuid.Invoke(null, new object[] { g })!;
                // string
                var mStr = t.GetMethod(name, BindingFlags.Public | BindingFlags.Static, new[] { typeof(string) });
                if (mStr != null) return (T)mStr.Invoke(null, new object[] { s })!;
            }

            // 2) Constructores públicos
            // Guid
            var ctorGuidPublic = t.GetConstructor(new[] { typeof(Guid) });
            if (ctorGuidPublic != null) return (T)ctorGuidPublic.Invoke(new object[] { g })!;
            // string
            var ctorStrPublic = t.GetConstructor(new[] { typeof(string) });
            if (ctorStrPublic != null) return (T)ctorStrPublic.Invoke(new object[] { s })!;

            // 3) Constructores NO públicos (muy común en strongly-typed ids generados)
            var ctorGuidNonPublic = t.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(Guid) }, null);
            if (ctorGuidNonPublic != null) return (T)ctorGuidNonPublic.Invoke(new object[] { g })!;
            var ctorStrNonPublic = t.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(string) }, null);
            if (ctorStrNonPublic != null) return (T)ctorStrNonPublic.Invoke(new object[] { s })!;

            // 4) Si es struct sin ctor, usar default
            if (t.IsValueType)
                return default!;

            Assert.Inconclusive($"No se pudo crear {t.Name}. Expón un método estático (From/Create/Parse/Of/New) o un ctor (Guid/string).");
            return default!;
        }

        private static EmpresaId NewEmpresaId()
        {
            // Muchos modelos usan string para EmpresaId (RUC/slug). Probamos ambos.
            return CreateId<EmpresaId>();
        }

        private static EstablecimientoId NewEstablecimientoId() => CreateId<EstablecimientoId>();
        private static UsuarioId NewUsuarioId() => CreateId<UsuarioId>();
        // ==============================================================================

        [Test]
        public void Crear_Notificacion_Con_Todos_Los_Campos_Requeridos_Deberia_Persistir_Valores()
        {
            // Arrange
            var now = new DateTime(2025, 01, 31, 8, 30, 0, DateTimeKind.Utc);
            var id = Guid.NewGuid();
            var indicadorId = Guid.NewGuid();
            var empresaId = NewEmpresaId();
            var establecimientoId = NewEstablecimientoId();
            var usuarioId = NewUsuarioId();

            // Act
            var n = new Notificacion
            {
                Id = id,
                IndicadorId = indicadorId,
                EmpresaId = empresaId,
                EstablecimientoId = establecimientoId,
                UsuarioId = usuarioId,
                Medio = "Email",
                Destinatario = "jane.doe@acme.com",
                Horario = "08:00-18:00",
                Activo = true,
                    FechaCreacion = now,
                FechaUltimaModificacion = null
            };

            // Assert
            Assert.That(n.Id, Is.EqualTo(id));
            Assert.That(n.IndicadorId, Is.EqualTo(indicadorId));
            Assert.That(n.EmpresaId, Is.EqualTo(empresaId));
            Assert.That(n.EstablecimientoId, Is.EqualTo(establecimientoId));
            Assert.That(n.UsuarioId, Is.EqualTo(usuarioId));
            Assert.That(n.Medio, Is.EqualTo("Email"));
            Assert.That(n.Destinatario, Is.EqualTo("jane.doe@acme.com"));
            Assert.That(n.Horario, Is.EqualTo("08:00-18:00"));
            Assert.That(n.Activo, Is.True);
            Assert.That(n.FechaCreacion, Is.EqualTo(now));
            Assert.That(n.FechaUltimaModificacion, Is.Null);
        }

        [Test]
        public void Defaults_Deberian_Ser_CadenasVacias_Y_FechaUltimaModificacion_Nula()
        {
            // Arrange
            var n = new Notificacion
            {
                // required
                EmpresaId = NewEmpresaId(),
                EstablecimientoId = NewEstablecimientoId(),
                UsuarioId = NewUsuarioId()
                // El resto queda con defaults
            };

            // Assert
            Assert.That(n.Medio, Is.EqualTo(string.Empty));
            Assert.That(n.Destinatario, Is.EqualTo(string.Empty));
            Assert.That(n.Horario, Is.EqualTo(string.Empty));
            Assert.That(n.FechaUltimaModificacion, Is.Null);

            // FechaCreacion es el default de DateTime si no se setea; validamos mutabilidad
                var now = IndicadoresNegocioBC.Tests.TestUtils.TestTime.BaseUtc();
            n.FechaCreacion = now;
            Assert.That(n.FechaCreacion, Is.EqualTo(now));
        }

        [Test]
        public void Actualizar_Campos_Deberia_Modificar_Valores_Sin_Restricciones_De_Dominio()
        {
            // Arrange
            var n = new Notificacion
            {
                EmpresaId = NewEmpresaId(),
                EstablecimientoId = NewEstablecimientoId(),
                UsuarioId = NewUsuarioId(),
                Medio = "SMS",
                Destinatario = "+51999999999",
                Horario = "09:00-17:00",
                Activo = false,
                    FechaCreacion = IndicadoresNegocioBC.Tests.TestUtils.TestTime.BaseUtc(),
            };

            // Act
            n.Medio = "Email";
            n.Destinatario = "user@example.org";
            n.Horario = "07:00-19:00";
            n.Activo = true;
            n.FechaUltimaModificacion = new DateTime(2025, 02, 02, 12, 0, 0, DateTimeKind.Utc);

            // Assert
            Assert.That(n.Medio, Is.EqualTo("Email"));
            Assert.That(n.Destinatario, Is.EqualTo("user@example.org"));
            Assert.That(n.Horario, Is.EqualTo("07:00-19:00"));
            Assert.That(n.Activo, Is.True);
            Assert.That(n.FechaUltimaModificacion.HasValue, Is.True);
        }

        [Test]
        public void Instancias_Distintas_Deberian_Mantener_Estado_Aislado()
        {
            // Arrange
            var a = new Notificacion
            {
                Id = Guid.NewGuid(),
                IndicadorId = Guid.NewGuid(),
                EmpresaId = NewEmpresaId(),
                EstablecimientoId = NewEstablecimientoId(),
                UsuarioId = NewUsuarioId(),
                Medio = "Email",
                Destinatario = "a@acme.com",
                Horario = "08:00-18:00",
                Activo = true,
                FechaCreacion = IndicadoresNegocioBC.Tests.TestUtils.TestTime.BaseUtc()
            };

            var b = new Notificacion
            {
                Id = Guid.NewGuid(),
                IndicadorId = Guid.NewGuid(),
                EmpresaId = NewEmpresaId(),
                EstablecimientoId = NewEstablecimientoId(),
                UsuarioId = NewUsuarioId(),
                Medio = "SMS",
                Destinatario = "+51 900000000",
                Horario = "10:00-16:00",
                Activo = false,
                    FechaCreacion = IndicadoresNegocioBC.Tests.TestUtils.TestTime.BaseUtc()
            };

            // Assert
            Assert.That(a.Id, Is.Not.EqualTo(b.Id));
            Assert.That(a.IndicadorId, Is.Not.EqualTo(b.IndicadorId));
            Assert.That(a.Medio, Is.Not.EqualTo(b.Medio));
            Assert.That(a.Destinatario, Is.Not.EqualTo(b.Destinatario));
            Assert.That(a.Horario, Is.Not.EqualTo(b.Horario));
            Assert.That(a.Activo, Is.Not.EqualTo(b.Activo));
        }
    }
}
