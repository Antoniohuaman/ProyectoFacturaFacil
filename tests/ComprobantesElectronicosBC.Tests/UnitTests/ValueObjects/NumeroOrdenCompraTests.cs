using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text.Json;
using ComprobantesElectronicosBC.Domain.ValueObjects;

namespace ComprobantesElectronicosBC.Tests.UnitTests.ValueObjects
{
	[TestFixture]
	public class NumeroOrdenCompraTests
	{
		[TestCase(null)]
		[TestCase("")]
		[TestCase("   ")]
		public void FromOptional_NullOrEmpty_ReturnsNull(string? input)
		{
			var vo = NumeroOrdenCompra.FromOptional(input);
			Assert.That(vo, Is.Null);
		}

		[Test]
		public void Create_NoHyphen_UppercasesAndTrims()
		{
			var vo = NumeroOrdenCompra.Create("  oc001  ");
			Assert.That(vo.Valor, Is.EqualTo("OC001"));
		}

		[Test]
		public void Create_SerieCorrelativo_PadsCorrelativoTo8()
		{
			var vo = NumeroOrdenCompra.Create("oc01-12");
			Assert.That(vo.Valor, Is.EqualTo("OC01-00000012"));
		}

		[Test]
		public void Create_TrimsAroundHyphen_AndPadsNumeric()
		{
			var vo = NumeroOrdenCompra.Create("  oc01 -   345  ");
			Assert.That(vo.Valor, Is.EqualTo("OC01-00000345"));
		}

		[Test]
		public void Create_NotNumericCorrelativo_NoPadding_KeepText()
		{
			var vo = NumeroOrdenCompra.Create("oc-abc123");
			Assert.That(vo.Valor, Is.EqualTo("OC-abc123"));
		}

		[Test]
		public void Create_CorrelativoMoreThan8Digits_NoPadding()
		{
			var vo = NumeroOrdenCompra.Create("oc01-123456789");
			Assert.That(vo.Valor, Is.EqualTo("OC01-123456789"));
		}

		[Test]
		public void Equality_SameCanonicalValue_AreEqual()
		{
			var a = NumeroOrdenCompra.Create("OC01-12");
			var b = NumeroOrdenCompra.Create("  oc01-00000012 ");
			Assert.That(a, Is.EqualTo(b));
			Assert.That(a == b, Is.True);
			Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
		}

		[Test]
		public void Equality_DifferentValues_NotEqual()
		{
			var a = NumeroOrdenCompra.Create("OC01-12");
			var b = NumeroOrdenCompra.Create("OC02-12");
			Assert.That(a, Is.Not.EqualTo(b));
			Assert.That(a != b, Is.True);
		}

		[Test]
		public void CanBeUsedAsDictionaryKey_ByCanonicalValue()
		{
			var dict = new Dictionary<NumeroOrdenCompra, string>();
			dict[NumeroOrdenCompra.Create("oc01-12")] = "primero";
			// Misma clave canónica
			dict[NumeroOrdenCompra.Create("OC01-00000012")] = "segundo";

			Assert.That(dict.Count, Is.EqualTo(1));
			Assert.That(dict[NumeroOrdenCompra.Create("OC01-12")], Is.EqualTo("segundo"));
		}

		[Test]
		public void ToString_ReturnsValor()
		{
			var vo = NumeroOrdenCompra.Create("oc01-7");
			Assert.That(vo.ToString(), Is.EqualTo(vo.Valor));
			Assert.That(vo.ToString(), Is.EqualTo("OC01-00000007"));
		}

		[Test]
		public void Json_Roundtrip_PreservesCanonicalValue()
		{
			var original = NumeroOrdenCompra.Create("oc01-45");
			var json = JsonSerializer.Serialize(original);
			var copy  = JsonSerializer.Deserialize<NumeroOrdenCompra>(json);

			Assert.That(copy, Is.Not.Null);
			Assert.That(copy, Is.EqualTo(original));
			Assert.That(copy!.Valor, Is.EqualTo("OC01-00000045"));
		}

		[Test]
		public void Create_Invalid_Throws()
		{
			Assert.Throws<ArgumentException>(() => NumeroOrdenCompra.Create(null!));
			Assert.Throws<ArgumentException>(() => NumeroOrdenCompra.Create("   "));
		}
	}
}
