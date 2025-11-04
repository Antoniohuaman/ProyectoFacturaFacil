using NUnit.Framework;
using GestionInventarioBC.Domain.ValueObjects;
using SharedKernel.Exceptions;

namespace GestionInventarioBC.Tests.ValueObjects
{
	[TestFixture]
	public class ReferenciaDocumentoTests
	{
		[Test]
		public void Crear_valida_no_vacios_y_formatea()
		{
			var r = ReferenciaDocumento.Crear("FAC", "F001-123");
			Assert.That(r.Tipo, Is.EqualTo("FAC"));
			Assert.That(r.Numero, Is.EqualTo("F001-123"));
			Assert.That(r.ToString(), Is.EqualTo("FAC:F001-123"));

			Assert.That(() => ReferenciaDocumento.Crear("", "X"), Throws.TypeOf<BusinessRuleException>());
			Assert.That(() => ReferenciaDocumento.Crear("FAC", "  "), Throws.TypeOf<BusinessRuleException>());
		}
	}
}

