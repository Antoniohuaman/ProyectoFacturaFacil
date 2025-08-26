using NUnit.Framework;
using GestionClientesBC.Domain.ValueObjects;
using SharedKernel.Exceptions;

namespace GestionClientesBC.Tests.ValueObjects
{
    [TestFixture]
    public class NombreClienteTests
    {
        // MANUAL: capitaliza y fuerza siglas societarias a MAYÚSCULAS en ParaMostrar
        [TestCase("juan pérez", "JUAN PÉREZ", "Juan Pérez")]
        [TestCase("  EMPRESA   SAC ", "EMPRESA SAC", "Empresa SAC")]                   // colapso de espacios
        [TestCase("ALFA S.A.C.", "ALFA S.A.C.", "ALFA S.A.C.")]                        // S.A.C. se conserva en MAYÚS
        [TestCase("tecnologías & soluciones srl", "TECNOLOGÍAS & SOLUCIONES SRL", "Tecnologías & Soluciones SRL")] // SRL forzado
        [TestCase("panadería 'la esquina'", "PANADERÍA 'LA ESQUINA'", "Panadería 'La Esquina'")]                   // apóstrofe permitido
        [TestCase("Comercial, Andina - Norte", "COMERCIAL, ANDINA - NORTE", "Comercial, Andina - Norte")]          // puntuación común
        [TestCase("Inversiones ABC S.A.", "INVERSIONES ABC S.A.", "Inversiones ABC S.A.")]                          // S.A.
        [TestCase("Consultoría EIRL", "CONSULTORÍA EIRL", "Consultoría EIRL")]                                      // EIRL
        [TestCase("Servicios Integrales s.a.a.", "SERVICIOS INTEGRALES S.A.A.", "Servicios Integrales S.A.A.")]     // S.A.A.
        [TestCase("Innovación sacs", "INNOVACIÓN SACS", "Innovación SACS")]                                         // SACS
        public void Crear_Manual_NormalizaYFormateaDisplay(string input, string canonico, string display)
        {
            var n = NombreCliente.Crear(input);

            Assert.That(n.Valor, Is.EqualTo(canonico), "Valor canónico debe ser upper + espacios colapsados.");
            Assert.That(n.ParaMostrar, Is.EqualTo(display), "ParaMostrar debe aplicar TitleCase y siglas societarias en MAYÚSCULAS.");
            Assert.That(n.ToString(), Is.EqualTo(display));
        }

        // OFICIAL: conserva literal (sin capitalizar), solo trim+colapso y longitud
        [Test]
        public void CrearDesdeFuenteOficial_ConservaLiteral_ValidaLongitud()
        {
            var oficial = NombreCliente.CrearDesdeFuenteOficial("  Alfa @ S.A.C.  ");
            Assert.That(oficial.ParaMostrar, Is.EqualTo("Alfa @ S.A.C."));   // conserva casing y '@'
            Assert.That(oficial.Valor, Is.EqualTo("ALFA @ S.A.C."));         // canónico para igualdad
        }

        // Longitud SUNAT: 1..100
        [Test]
        public void Crear_Manual_Longitud_Max100_OK_101_Falla()
        {
            var cien = new string('a', 100);
            var ok = NombreCliente.Crear(cien);
            Assert.That(ok, Is.Not.Null);
            Assert.That(ok.ParaMostrar.Length, Is.EqualTo(100));
            Assert.That(ok.Valor.Length, Is.EqualTo(100));

            var cientoUno = new string('a', 101);
            Assert.That(() => NombreCliente.Crear(cientoUno),
                Throws.TypeOf<BusinessRuleException>());
        }

        [Test]
        public void CrearDesdeFuenteOficial_Longitud_Max100_OK_101_Falla()
        {
            var cien = new string('b', 100);
            var ok = NombreCliente.CrearDesdeFuenteOficial(cien);
            Assert.That(ok, Is.Not.Null);
            Assert.That(ok.ParaMostrar.Length, Is.EqualTo(100));

            var cientoUno = new string('b', 101);
            Assert.That(() => NombreCliente.CrearDesdeFuenteOficial(cientoUno),
                Throws.TypeOf<BusinessRuleException>());
        }

        // MANUAL inválidos
        [TestCase("",       TestName = "Vacio_Lanza")]
        [TestCase(" ",      TestName = "SoloEspacios_Lanza")]
        [TestCase("# SAC",  TestName = "CaracterNoPermitido_Numeral_Lanza")] // '#' no permitido en MANUAL
        [TestCase("* S.A.C.", TestName = "CaracterNoPermitido_Asterisco_Lanza")]
        [TestCase("= S.A.", TestName = "CaracterNoPermitido_Igual_Lanza")]
        [TestCase("& , - . ", TestName = "SinAlfanumericos_Lanza")]          // solo símbolos válidos pero sin letras/dígitos
        public void Crear_Manual_Invalido_Lanza(string input)
        {
            Assert.That(() => NombreCliente.Crear(input),
                Throws.TypeOf<BusinessRuleException>());
        }

        // TryCrear (MANUAL)
        [Test]
        public void TryCrear_Ok_y_Fail_Manual()
        {
            var ok = NombreCliente.TryCrear("Empresa S.A.C.", out var n1);
            Assert.That(ok, Is.True);
            Assert.That(n1, Is.Not.Null);
            Assert.That(n1!.Valor, Is.EqualTo("EMPRESA S.A.C."));
            Assert.That(n1.ParaMostrar, Is.EqualTo("Empresa S.A.C."));

            var fail = NombreCliente.TryCrear("#@@", out var n2);
            Assert.That(fail, Is.False);
            Assert.That(n2, Is.Null);
        }

        // Igualdad por valor: ignora mayúsculas/minúsculas y espacios extra (canónico ya es upper+colapsado)
        [Test]
        public void Igualdad_IgnoraCaseYEspacios()
        {
            var a = NombreCliente.Crear("empresa   s.a.c.");
            var b = NombreCliente.Crear("EMPRESA S.A.C.");
            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }
    }
}
