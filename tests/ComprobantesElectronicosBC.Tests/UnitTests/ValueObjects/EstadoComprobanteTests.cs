using System;
using NUnit.Framework;
using ComprobantesElectronicosBC.Domain.ValueObjects;

namespace ComprobantesElectronicosBC.Tests.UnitTests.ValueObjects
{
    [TestFixture]
    public class EstadoComprobanteTests
    {
        [Test]
        public void FromCode_AceptaCodigosValidos_InsensibleAMayusculas()
        {
            var dr = EstadoComprobante.FromCode("DR");
            var pe = EstadoComprobante.FromCode("pe");
            var nc = EstadoComprobante.FromCode("Nc");
            var ac = EstadoComprobante.FromCode("AC");
            var rj = EstadoComprobante.FromCode("RJ");
            var cn = EstadoComprobante.FromCode("CN");

            Assert.Multiple(() =>
            {
                Assert.That(dr.Code, Is.EqualTo("DR"));
                Assert.That(pe.Code, Is.EqualTo("PE"));
                Assert.That(nc.Code, Is.EqualTo("NC"));
                Assert.That(ac.Code, Is.EqualTo("AC"));
                Assert.That(rj.Code, Is.EqualTo("RJ"));
                Assert.That(cn.Code, Is.EqualTo("CN"));
            });
        }

        [Test]
        public void FromCode_LanzaEnCodigoInvalido()
        {
            Assert.Throws<ArgumentException>(() => EstadoComprobante.FromCode(""));
            Assert.Throws<ArgumentException>(() => EstadoComprobante.FromCode("XX"));
        }

        [Test]
        public void Capacidades_Draft()
        {
            var st = EstadoComprobante.Draft;
            Assert.Multiple(() =>
            {
                Assert.That(st.PuedeEditar, Is.True);
                Assert.That(st.PuedeEliminar, Is.True);
                Assert.That(st.PuedeEmitir, Is.True);
                Assert.That(st.PuedeReintentarEnvio, Is.False);
                Assert.That(st.PuedeAnular, Is.False);
                Assert.That(st.EsFinal, Is.False);
            });
        }

        [Test]
        public void Capacidades_PendingValidation()
        {
            var st = EstadoComprobante.PendingValidation;
            Assert.Multiple(() =>
            {
                Assert.That(st.PuedeEditar, Is.False);
                Assert.That(st.PuedeEliminar, Is.False);
                Assert.That(st.PuedeEmitir, Is.False);
                Assert.That(st.PuedeReintentarEnvio, Is.True);
                Assert.That(st.PuedeAnular, Is.False);
                Assert.That(st.EsFinal, Is.False);
            });
        }

        [Test]
        public void Capacidades_NeedsCorrection()
        {
            var st = EstadoComprobante.NeedsCorrection;
            Assert.Multiple(() =>
            {
                Assert.That(st.PuedeEditar, Is.True);
                Assert.That(st.PuedeEliminar, Is.False);
                Assert.That(st.PuedeEmitir, Is.True);
                Assert.That(st.PuedeReintentarEnvio, Is.True);
                Assert.That(st.PuedeAnular, Is.False);
                Assert.That(st.EsFinal, Is.False);
            });
        }

        [Test]
        public void Capacidades_Accepted()
        {
            var st = EstadoComprobante.Accepted;
            Assert.Multiple(() =>
            {
                Assert.That(st.PuedeEditar, Is.False);
                Assert.That(st.PuedeEliminar, Is.False);
                Assert.That(st.PuedeEmitir, Is.False);
                Assert.That(st.PuedeReintentarEnvio, Is.False);
                Assert.That(st.PuedeAnular, Is.True);
                Assert.That(st.EsFinal, Is.True);
            });
        }

        [Test]
        public void Capacidades_Rejected()
        {
            var st = EstadoComprobante.Rejected;
            Assert.Multiple(() =>
            {
                Assert.That(st.PuedeEditar, Is.False);
                Assert.That(st.PuedeEliminar, Is.False);
                Assert.That(st.PuedeEmitir, Is.False);
                Assert.That(st.PuedeReintentarEnvio, Is.False);
                Assert.That(st.PuedeAnular, Is.False);
                Assert.That(st.EsFinal, Is.True);
            });
        }

        [Test]
        public void Capacidades_Cancelled()
        {
            var st = EstadoComprobante.Cancelled;
            Assert.Multiple(() =>
            {
                Assert.That(st.PuedeEditar, Is.False);
                Assert.That(st.PuedeEliminar, Is.False);
                Assert.That(st.PuedeEmitir, Is.False);
                Assert.That(st.PuedeReintentarEnvio, Is.False);
                Assert.That(st.PuedeAnular, Is.False);
                Assert.That(st.EsFinal, Is.True);
            });
        }

        [Test]
        public void Transiciones_DesdeDraft_SoloAPendingValidation()
        {
            var dr = EstadoComprobante.Draft;
            Assert.That(dr.CanTransitionTo(EstadoComprobante.PendingValidation), Is.True);

            Assert.Multiple(() =>
            {
                Assert.That(dr.CanTransitionTo(EstadoComprobante.NeedsCorrection), Is.False);
                Assert.That(dr.CanTransitionTo(EstadoComprobante.Accepted), Is.False);
                Assert.That(dr.CanTransitionTo(EstadoComprobante.Rejected), Is.False);
                Assert.That(dr.CanTransitionTo(EstadoComprobante.Cancelled), Is.False);
            });

            // TransitionTo válido
            var next = dr.TransitionTo(EstadoComprobante.PendingValidation);
            Assert.That(next, Is.EqualTo(EstadoComprobante.PendingValidation));

            // TransitionTo inválido
            Assert.Throws<InvalidOperationException>(() => dr.TransitionTo(EstadoComprobante.Accepted));
        }

        [Test]
        public void Transiciones_DesdePending_3Ramas()
        {
            var pe = EstadoComprobante.PendingValidation;
            Assert.Multiple(() =>
            {
                Assert.That(pe.CanTransitionTo(EstadoComprobante.Accepted), Is.True);
                Assert.That(pe.CanTransitionTo(EstadoComprobante.Rejected), Is.True);
                Assert.That(pe.CanTransitionTo(EstadoComprobante.NeedsCorrection), Is.True);
                Assert.That(pe.CanTransitionTo(EstadoComprobante.Cancelled), Is.False);
                Assert.That(pe.CanTransitionTo(EstadoComprobante.Draft), Is.False);
            });
        }

        [Test]
        public void Transiciones_DesdeNeedsCorrection_SoloAPending()
        {
            var nc = EstadoComprobante.NeedsCorrection;
            Assert.That(nc.CanTransitionTo(EstadoComprobante.PendingValidation), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(nc.CanTransitionTo(EstadoComprobante.Accepted), Is.False);
                Assert.That(nc.CanTransitionTo(EstadoComprobante.Rejected), Is.False);
                Assert.That(nc.CanTransitionTo(EstadoComprobante.Cancelled), Is.False);
            });
        }

        [Test]
        public void Transiciones_DesdeAccepted_SoloACancelled()
        {
            var ac = EstadoComprobante.Accepted;
            Assert.That(ac.CanTransitionTo(EstadoComprobante.Cancelled), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(ac.CanTransitionTo(EstadoComprobante.Rejected), Is.False);
                Assert.That(ac.CanTransitionTo(EstadoComprobante.NeedsCorrection), Is.False);
                Assert.That(ac.CanTransitionTo(EstadoComprobante.Draft), Is.False);
            });
        }

        [Test]
        public void Transiciones_DesdeRejected_Ninguna()
        {
            var rj = EstadoComprobante.Rejected;
            Assert.Multiple(() =>
            {
                Assert.That(rj.CanTransitionTo(EstadoComprobante.Draft), Is.False);
                Assert.That(rj.CanTransitionTo(EstadoComprobante.PendingValidation), Is.False);
                Assert.That(rj.CanTransitionTo(EstadoComprobante.NeedsCorrection), Is.False);
                Assert.That(rj.CanTransitionTo(EstadoComprobante.Accepted), Is.False);
                Assert.That(rj.CanTransitionTo(EstadoComprobante.Cancelled), Is.False);
            });
        }

        [Test]
        public void Transiciones_DesdeCancelled_Ninguna()
        {
            var cn = EstadoComprobante.Cancelled;
            Assert.Multiple(() =>
            {
                Assert.That(cn.CanTransitionTo(EstadoComprobante.Draft), Is.False);
                Assert.That(cn.CanTransitionTo(EstadoComprobante.PendingValidation), Is.False);
                Assert.That(cn.CanTransitionTo(EstadoComprobante.NeedsCorrection), Is.False);
                Assert.That(cn.CanTransitionTo(EstadoComprobante.Accepted), Is.False);
                Assert.That(cn.CanTransitionTo(EstadoComprobante.Rejected), Is.False);
            });
        }

        [Test]
        public void SiguienteDesdeRespuestaSunat_MapeaCorrectamente()
        {
            Assert.Multiple(() =>
            {
                // Aceptado
                Assert.That(EstadoComprobante.SiguienteDesdeRespuestaSunat("0"), Is.EqualTo(EstadoComprobante.Accepted));
                Assert.That(EstadoComprobante.SiguienteDesdeRespuestaSunat("98"), Is.EqualTo(EstadoComprobante.Accepted));

                // Rechazado (2000–3999)
                Assert.That(EstadoComprobante.SiguienteDesdeRespuestaSunat("2000"), Is.EqualTo(EstadoComprobante.Rejected));
                Assert.That(EstadoComprobante.SiguienteDesdeRespuestaSunat("3999"), Is.EqualTo(EstadoComprobante.Rejected));

                // Incidencias comunicación (0100–0199) → permanecer como pendiente/reintento
                Assert.That(EstadoComprobante.SiguienteDesdeRespuestaSunat("100"), Is.EqualTo(EstadoComprobante.PendingValidation));
                Assert.That(EstadoComprobante.SiguienteDesdeRespuestaSunat("199"), Is.EqualTo(EstadoComprobante.PendingValidation));

                // Vacío / null / desconocido → NeedsCorrection
                Assert.That(EstadoComprobante.SiguienteDesdeRespuestaSunat(null), Is.EqualTo(EstadoComprobante.NeedsCorrection));
                Assert.That(EstadoComprobante.SiguienteDesdeRespuestaSunat(""), Is.EqualTo(EstadoComprobante.NeedsCorrection));
                Assert.That(EstadoComprobante.SiguienteDesdeRespuestaSunat("ABC"), Is.EqualTo(EstadoComprobante.NeedsCorrection));
                Assert.That(EstadoComprobante.SiguienteDesdeRespuestaSunat("9999"), Is.EqualTo(EstadoComprobante.NeedsCorrection));
            });
        }

        [Test]
        public void ToString_DevuelveNombreLegible()
        {
            Assert.That(EstadoComprobante.Draft.ToString(), Is.EqualTo("Draft"));
            Assert.That(EstadoComprobante.Accepted.ToString(), Is.EqualTo("Accepted"));
        }
    }
}