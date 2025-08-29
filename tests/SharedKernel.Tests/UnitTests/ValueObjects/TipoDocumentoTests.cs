using NUnit.Framework;
using SharedKernel.ValueObjects;

namespace SharedKernel.Tests.UnitTests.ValueObjects;

[TestFixture]
public class TipoDocumentoTests
{
	[Test]
	public void Enum_Valores_Cubren_Catalogo06YInterno()
	{
		var values = (TipoDocumento[])System.Enum.GetValues(typeof(TipoDocumento));
		Assert.That(values, Has.Exactly(9).Items);
		Assert.That(values, Does.Contain(TipoDocumento.Ruc));
		Assert.That(values, Does.Contain(TipoDocumento.Dni));
		Assert.That(values, Does.Contain(TipoDocumento.CarnetExtranjeria));
		Assert.That(values, Does.Contain(TipoDocumento.Pasaporte));
		Assert.That(values, Does.Contain(TipoDocumento.CedulaDiplomatica));
		Assert.That(values, Does.Contain(TipoDocumento.DocIdentidadPaisResidenciaNoDomiciliado));
		Assert.That(values, Does.Contain(TipoDocumento.TinPersonaNatural));
		Assert.That(values, Does.Contain(TipoDocumento.InPersonaJuridica));
		Assert.That(values, Does.Contain(TipoDocumento.SinDocumento));
	}

	[TestCase(TipoDocumento.Ruc, "6")]
	[TestCase(TipoDocumento.Dni, "1")]
	[TestCase(TipoDocumento.CarnetExtranjeria, "4")]
	[TestCase(TipoDocumento.Pasaporte, "7")]
	[TestCase(TipoDocumento.CedulaDiplomatica, "A")]
	[TestCase(TipoDocumento.DocIdentidadPaisResidenciaNoDomiciliado, "B")]
	[TestCase(TipoDocumento.TinPersonaNatural, "C")]
	[TestCase(TipoDocumento.InPersonaJuridica, "D")]
	public void Mapeo_SchemeId_Correcto(TipoDocumento tipo, string esperado)
	{
		string numero = tipo switch
		{
			TipoDocumento.Ruc => "20600893409", // RUC válido
			TipoDocumento.Dni => "08661899",     // DNI válido
			_ => "ABC123"
		};
		var doc = DocumentoIdentidad.Crear(tipo, numero);
		Assert.That(doc.SchemeId, Is.EqualTo(esperado));
	}

	[Test]
	public void SinDocumento_Lanza_SchemeId()
	{
		var doc = DocumentoIdentidad.Crear(TipoDocumento.SinDocumento, null);
		Assert.That(() => { var _ = doc.SchemeId; }, Throws.Exception);
	}

	[TestCase(TipoDocumento.Ruc, "RUC")]
	[TestCase(TipoDocumento.Dni, "DNI")]
	[TestCase(TipoDocumento.CarnetExtranjeria, "Carnet Extranjería")]
	[TestCase(TipoDocumento.Pasaporte, "Pasaporte")]
	[TestCase(TipoDocumento.CedulaDiplomatica, "Cédula Diplomática")]
	[TestCase(TipoDocumento.DocIdentidadPaisResidenciaNoDomiciliado, "Doc. identidad país residencia")]
	[TestCase(TipoDocumento.TinPersonaNatural, "TIN")]
	[TestCase(TipoDocumento.InPersonaJuridica, "IN")]
	[TestCase(TipoDocumento.SinDocumento, "Sin documento")]
	public void ToString_Contiene_Etiqueta(TipoDocumento tipo, string esperado)
	{
		string numero = tipo switch
		{
			TipoDocumento.Ruc => "20600893409", // RUC válido
			TipoDocumento.Dni => "08661899",     // DNI válido
			_ => "ABC123"
		};
		var doc = DocumentoIdentidad.Crear(tipo, numero);
		Assert.That(doc.ToString(), Does.Contain(esperado));
	}
}
