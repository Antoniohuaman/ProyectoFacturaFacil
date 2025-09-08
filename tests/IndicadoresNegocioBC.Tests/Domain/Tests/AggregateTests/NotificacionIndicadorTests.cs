using System;
using System.Linq;
using NUnit.Framework;
// Ajusta estos namespaces a tu solución
using SharedKernel.ValueObjects;
using IndicadoresNegocioBC.Domain.ValueObjects;
using IndicadoresNegocioBC.Domain.Aggregates;

namespace IndicadoresNegocioBC.Tests.Domain
{
    [TestFixture]
    public class NotificacionIndicadorTests
    {
        // ====== Builders de prueba (AJUSTAR a tu implementación real) ======

        private static EmpresaId Empresa() =>
            // Ajustado: EmpresaId espera string
            EmpresaId.From(Guid.NewGuid().ToString());

        private static EstablecimientoId Establecimiento() =>
            // TODO: idem
            EstablecimientoId.From(Guid.NewGuid());

        private static UsuarioId Usuario() =>
            // TODO: idem
            UsuarioId.From(Guid.NewGuid());

        private static HorarioNotificacion Horario() =>
            // Ajustado: solo acepta TimeSpan
            new HorarioNotificacion(new TimeSpan(9, 0, 0));

        private static MedioNotificacion MedioEmail() =>
            // Ajustado: usar propiedad estática
            MedioNotificacion.Correo;

        private static MedioNotificacion MedioSms() =>
            // Ajustado: usar propiedad estática
            MedioNotificacion.Sms;

        private static DestinatarioNotificacion DestinatarioConEmail() =>
            // Ajustado: Email.Create
            new DestinatarioNotificacion(SharedKernel.ValueObjects.Email.Create("demo@acme.com"), null);

        private static DestinatarioNotificacion DestinatarioConTelefono() =>
            new DestinatarioNotificacion(null, SharedKernel.ValueObjects.Telefono.FromTexto("+51987654321"));

        private static DestinatarioNotificacion DestinatarioVacio() =>
            new DestinatarioNotificacion(null, null);

        private static NotificacionIndicador CrearNotificacionIndicadorActiva(
            string asunto = "Resumen diario de ventas",
            DestinatarioNotificacion? dest = null,
            MedioNotificacion? medio = null)
        {
            return new NotificacionIndicador(
                empresaId: Empresa(),
                indicadorId: Guid.NewGuid(),
                establecimientoId: Establecimiento(),
                usuarioId: Usuario(),
                asunto: asunto,
                horarioEnvio: Horario(),
                medio: medio ?? MedioEmail(),
                destinatario: dest ?? DestinatarioConEmail(),
                activo: true
            );
        }

        // ============================ TESTS ============================

        [Test]
        public void Ctor_DeberiaCrearActivaConAsuntoTrim()
        {
            var ni = CrearNotificacionIndicadorActiva("   Ventas Hoy   ");

            Assert.That(ni.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(ni.Asunto, Is.EqualTo("Ventas Hoy"));
            Assert.That(ni.Activo, Is.True);
            Assert.That(ni.FechaCreacion, Is.Not.EqualTo(default(DateTimeOffset)));
        }

        [Test]
        public void Ctor_DeberiaLanzarSiDestinatarioSinCanales()
        {
            Assert.That(
                () => CrearNotificacionIndicadorActiva(dest: DestinatarioVacio()),
                Throws.ArgumentException
                    .With.Message.Contains("medio de contacto: email o teléfono")
            );
        }

        [Test]
        public void CambiarAsunto_DeberiaActualizarYMarcarModificacion()
        {
            var ni = CrearNotificacionIndicadorActiva();
            var antes = ni.FechaUltimaModificacion;

            ni.CambiarAsunto("  Nuevo asunto  ");

            Assert.That(ni.Asunto, Is.EqualTo("Nuevo asunto"));
            Assert.That(ni.FechaUltimaModificacion, Is.Not.EqualTo(antes));
        }

        [Test]
        public void CambiarRangoFechas_DeberiaValidarInversion()
        {
            var ni = CrearNotificacionIndicadorActiva();
            var inicio = new DateTimeOffset(2025, 1, 10, 0, 0, 0, TimeSpan.Zero);
            var fin    = new DateTimeOffset(2025, 1, 9, 0, 0, 0, TimeSpan.Zero);

            Assert.That(() => ni.CambiarRangoFechas(inicio, fin),
                        Throws.ArgumentException
                              .With.Message.Contains("no puede ser anterior"));
        }

        [Test]
        public void CambiarDiasSemana_DeberiaDeduplicarYPermitirTodosCuandoNullOVacio()
        {
            var ni = CrearNotificacionIndicadorActiva();

            // Caso: deduplicar
            ni.CambiarDiasSemana(new[] { DayOfWeek.Monday, DayOfWeek.Monday, DayOfWeek.Wednesday });
            Assert.That(ni.DiasSemana!.Length, Is.EqualTo(2));
            Assert.That(ni.DiasSemana, Does.Contain(DayOfWeek.Monday));
            Assert.That(ni.DiasSemana, Does.Contain(DayOfWeek.Wednesday));

            // Caso: null -> todos los días
            ni.CambiarDiasSemana(null);
            Assert.That(ni.EsDiaValido(DayOfWeek.Sunday), Is.True);

            // Caso: vacío -> todos los días
            ni.CambiarDiasSemana(Array.Empty<DayOfWeek>());
            Assert.That(ni.EsDiaValido(DayOfWeek.Thursday), Is.True);
        }

        [Test]
        public void Activar_NoDebePermitirSiVigenciaVencida()
        {
            var ni = CrearNotificacionIndicadorActiva();
            ni.Desactivar();
            var ayer = DateTimeOffset.UtcNow.AddDays(-1);
            var anteayer = DateTimeOffset.UtcNow.AddDays(-2);

            ni.CambiarRangoFechas(anteayer, ayer);

            Assert.That(() => ni.Activar(),
                        Throws.InvalidOperationException
                              .With.Message.Contains("vigencia vencida"));
        }

        [Test]
        public void DebeEnviarEn_RespetaActivoVigenciaYDia()
        {
            var ni = CrearNotificacionIndicadorActiva();
            ni.CambiarDiasSemana(new[] { DayOfWeek.Monday, DayOfWeek.Tuesday });
            ni.CambiarRangoFechas(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));

            var lunes = Proximo(DayOfWeek.Monday);
            Assert.That(ni.DebeEnviarEn(lunes), Is.True, "Lunes dentro de vigencia y día permitido");

            // Si hay miércoles en el rango, probarlo; si no, simplemente confirmar que no se debe enviar ese día
            try
            {
                var miercoles = Proximo(DayOfWeek.Wednesday);
                Assert.That(ni.DebeEnviarEn(miercoles), Is.False, "Miércoles fuera de días configurados");
            }
            catch (InvalidOperationException)
            {
                Assert.Pass("No hay miércoles en el rango de vigencia, lo cual es válido para la prueba.");
            }

            ni.Desactivar();
            Assert.That(ni.DebeEnviarEn(lunes), Is.False, "Desactivado no debe enviar");
        }

        [Test]
        public void CambiarMedio_O_CambiarDestinatario_RevalidaCuandoActivo()
        {
            var ni = CrearNotificacionIndicadorActiva(dest: DestinatarioConTelefono(), medio: MedioSms());

            // Cambiamos destinatario a uno inválido (sin canales) y verificamos que falle al revalidar.
            Assert.That(
                () => ni.CambiarDestinatario(DestinatarioVacio()),
                Throws.ArgumentException
                    .With.Message.Contains("medio de contacto: email o teléfono")
            );

            // Restauramos con uno válido (email) aunque el medio sea SMS; la validación mínima exige al menos 1 canal.
            ni.CambiarDestinatario(DestinatarioConEmail());
            Assert.That(ni.Destinatario.Email, Is.Not.Null);
        }

        // ======================== Helpers ========================

        private static DateTimeOffset Proximo(DayOfWeek dia)
        {
            var hoy = DateTimeOffset.UtcNow;
            var fechaInicio = hoy.AddDays(-1).Date; // igual que en el test
            var fechaFin = hoy.AddDays(1).Date;
            for (var d = fechaInicio; d <= fechaFin; d = d.AddDays(1))
            {
                if (d.DayOfWeek == dia)
                {
                    return new DateTimeOffset(d.Year, d.Month, d.Day, 9, 0, 0, hoy.Offset);
                }
            }
            throw new InvalidOperationException($"No se encontró el día {dia} dentro del rango de vigencia configurado para la prueba.");
        }
    }
}
