using System;
using IndicadoresNegocioBC.Domain.ValueObjects;
using NUnit.Framework;

namespace IndicadoresNegocioBC.Tests.UnitTests.ValueObjects
{
    public class EstablecimientoTests
    {
        private static Guid Empresa() => Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static Guid Estab()   => Guid.Parse("22222222-2222-2222-2222-222222222222");

        [Test]
        public void Crear_EmpresaIdVacio_LanzaArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                Establecimiento.Crear(Guid.Empty, Estab(), "Tienda Centro"));
        }

        [Test]
        public void Crear_EstablecimientoIdVacio_LanzaArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                Establecimiento.Crear(Empresa(), Guid.Empty, "Tienda Centro"));
        }

        [Test]
        public void Crear_NombreNullOBlanco_GuardaNull()
        {
            var e1 = Establecimiento.Crear(Empresa(), Estab(), null);
            var e2 = Establecimiento.Crear(Empresa(), Estab(), "   ");

            Assert.That(e1.Nombre, Is.Null);
            Assert.That(e1.TieneNombre, Is.False);

            Assert.That(e2.Nombre, Is.Null);
            Assert.That(e2.TieneNombre, Is.False);
        }

        [Test]
        public void Crear_NormalizaNombre_Trim()
        {
            var e = Establecimiento.Crear(Empresa(), Estab(), "  Tienda Sur  ");
            Assert.That(e.Nombre, Is.EqualTo("Tienda Sur"));
            Assert.That(e.TieneNombre, Is.True);
        }

        [Test]
        public void Crear_NombreExcedeMaximo_LanzaArgumentException()
        {
            var nombre121 = new string('X', 121);
            Assert.Throws<ArgumentException>(() =>
                Establecimiento.Crear(Empresa(), Estab(), nombre121));
        }

        [Test]
        public void Crear_NombreEnLimiteMaximo_Aceptado()
        {
            var nombre120 = new string('Y', 120);
            var e = Establecimiento.Crear(Empresa(), Estab(), nombre120);
            Assert.That(e.Nombre, Is.EqualTo(nombre120));
        }

        [Test]
        public void ConNombre_ActualizaNombre_DeFormaInmutable()
        {
            var original = Establecimiento.Crear(Empresa(), Estab(), "Tienda A");
            var actualizado = original.ConNombre("Tienda B");

            // inmutabilidad: el original no cambia
            Assert.That(original.Nombre, Is.EqualTo("Tienda A"));
            // nueva instancia con nuevo nombre
            Assert.That(actualizado.Nombre, Is.EqualTo("Tienda B"));
            // son distintos por valor
            Assert.That(actualizado, Is.Not.EqualTo(original));
        }

        [Test]
        public void ConNombre_Null_DejaNombreNull()
        {
            var e = Establecimiento.Crear(Empresa(), Estab(), "Tienda A");
            var e2 = e.ConNombre(null);

            Assert.That(e2.Nombre, Is.Null);
            Assert.That(e2.TieneNombre, Is.False);
        }

        [Test]
        public void Igualdad_PorValor_EmpresaYEstablecimientoYNombreNormalizado()
        {
            var a = Establecimiento.Crear(Empresa(), Estab(), "  Tienda Norte ");
            var b = Establecimiento.Crear(Empresa(), Estab(), "Tienda Norte");

            Assert.That(a, Is.EqualTo(b)); // mismo valor tras normalizar nombre
        }

        [Test]
        public void Desigualdad_CambiaCualquierParteDeLaIdentidad()
        {
            var empresa2 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var estab2   = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

            var baseE = Establecimiento.Crear(Empresa(), Estab(), "Tienda");

            var distintoEmpresa = Establecimiento.Crear(empresa2, Estab(), "Tienda");
            var distintoEstab   = Establecimiento.Crear(Empresa(), estab2, "Tienda");
            var distintoNombre  = Establecimiento.Crear(Empresa(), Estab(), "Tienda 2");

            Assert.That(baseE, Is.Not.EqualTo(distintoEmpresa));
            Assert.That(baseE, Is.Not.EqualTo(distintoEstab));
            Assert.That(baseE, Is.Not.EqualTo(distintoNombre));
        }

        [Test]
        public void ToString_ConNombre_DevuelveNombre()
        {
            var e = Establecimiento.Crear(Empresa(), Estab(), "Sucursal Centro");
            Assert.That(e.ToString(), Is.EqualTo("Sucursal Centro"));
        }

        [Test]
        public void ToString_SinNombre_DevuelveGuidDelEstablecimiento()
        {
            var id = Estab();
            var e = Establecimiento.Crear(Empresa(), id, null);
            Assert.That(e.ToString(), Is.EqualTo(id.ToString("D")));
        }
    }
}