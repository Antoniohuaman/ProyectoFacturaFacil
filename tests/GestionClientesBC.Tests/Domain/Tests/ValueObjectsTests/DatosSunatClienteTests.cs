// tests/GestionClientesBC.Tests/ValueObjects/DatosSunatClienteTests.cs
using System;
using System.Linq;
using GestionClientesBC.Domain.ValueObjects;
using NUnit.Framework;

namespace GestionClientesBC.Tests.ValueObjects
{
    public class DatosSunatClienteTests
    {
        [Test]
        public void Create_WhenAllNull_ReturnsVacioInstance()
        {
            var vo = DatosSunatCliente.Create();

            Assert.That(vo, Is.SameAs(DatosSunatCliente.Vacio));
        }

        [Test]
        public void Create_NormalizesAndTrimsText()
        {
            var vo = DatosSunatCliente.Create(
                tipoContribuyente: "  RÉGIMEN GENERAL  ",
                estadoContribuyente: "  ACTIVO  ");

            Assert.That(vo.TipoContribuyente, Is.EqualTo("RÉGIMEN GENERAL"));
            Assert.That(vo.EstadoContribuyente, Is.EqualTo("ACTIVO"));
        }

        [Test]
        public void Create_TruncatesVeryLongText()
        {
            var largo = new string('A', 300);

            var vo = DatosSunatCliente.Create(tipoContribuyente: largo);

            Assert.That(vo.TipoContribuyente!.Length, Is.EqualTo(120));
        }

        [Test]
        public void Create_NormalizesActivitiesAndRemovesDuplicates()
        {
            var vo = DatosSunatCliente.Create(
                actividadesEconomicas: new[]
                {
                    " Venta al por menor ",
                    "VENTA AL POR MENOR",
                    "  ",
                    "Servicios varios"
                });

            Assert.That(vo.ActividadesEconomicas.Count, Is.EqualTo(2));
            Assert.That(vo.ActividadesEconomicas, Does.Contain("Venta al por menor"));
            Assert.That(vo.ActividadesEconomicas, Does.Contain("Servicios varios"));
        }

        [Test]
        public void Equality_IsValueBased()
        {
            var fecha = new DateTime(2020, 1, 1);

            var a = DatosSunatCliente.Create(
                tipoContribuyente: "Régimen General",
                actividadesEconomicas: new[] { "Act 1", "Act 2" },
                fechaInscripcion: fecha);

            var b = DatosSunatCliente.Create(
                tipoContribuyente: "Régimen General",
                actividadesEconomicas: new[] { "Act 2", "Act 1" }, // diferente orden
                fechaInscripcion: fecha);

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }
    }
}
