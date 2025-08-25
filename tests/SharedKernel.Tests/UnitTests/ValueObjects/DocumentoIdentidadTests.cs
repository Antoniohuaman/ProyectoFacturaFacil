using NUnit.Framework;
using SharedKernel.Domain.ValueObjects;
using SharedKernel.Exceptions;
using static SharedKernel.Domain.ValueObjects.DocumentoIdentidad;

namespace SharedKernel.Tests.ValueObjects;

[TestFixture]
public class DocumentoIdentidadTests
{
    // ===== Helpers para construir RUC válido =====
    private static string BuildRuc(string prefix10Digits)
    {
        Assert.That(prefix10Digits, Has.Length.EqualTo(10));
        Assert.That(prefix10Digits.All(char.IsDigit), Is.True);

        int[] pesos = { 5, 4, 3, 2, 7, 6, 5, 4, 3, 2 };
        int suma = 0;
        for (int i = 0; i < 10; i++)
            suma += (prefix10Digits[i] - '0') * pesos[i];
        int resto = suma % 11;
        int dv = 11 - resto;
        if (dv == 10) dv = 0;
        else if (dv == 11) dv = 1;
        return prefix10Digits + dv.ToString();
    }

    // ===== RUC =====

    [Test]
    public void Crear_Ruc_valido_normaliza_y_valida_digito()
    {
        // Empieza con 20 → heurística PJ
        var ruc = BuildRuc("2012345678");

        var doc = DocumentoIdentidad.Crear(TipoDocumento.Ruc, ruc);
        Assert.That(doc.EsRuc, Is.True);
        Assert.That(doc.Numero, Is.EqualTo(ruc));
        Assert.That(doc.EsRuc20, Is.True);
        Assert.That(doc.SchemeId, Is.EqualTo("6"));
        Assert.That(doc.ToString(), Is.EqualTo($"RUC {ruc}"));
    }

    [Test]
    public void Crear_Ruc_con_formato_varios_caracteres_no_numericos_se_normaliza()
    {
        var ruc = BuildRuc("2012345678");
        var conFormato = $"20.123.456-78 {ruc[^1]}"; // miscelánea de separadores

        var doc = DocumentoIdentidad.Crear(TipoDocumento.Ruc, conFormato);
        Assert.That(doc.Numero, Is.EqualTo(ruc));
    }

    [Test]
    public void Crear_Ruc_longitud_incorrecta_lanza()
    {
        Assert.That(() => DocumentoIdentidad.Crear(TipoDocumento.Ruc, "20123456789X"),
            Throws.TypeOf<BusinessRuleException>()); // > 11 después de limpiar
        Assert.That(() => DocumentoIdentidad.Crear(TipoDocumento.Ruc, "201234567"),
            Throws.TypeOf<BusinessRuleException>()); // < 11
    }

    [Test]
    public void Crear_Ruc_digito_verificador_invalido_lanza()
    {
        var ruc = BuildRuc("2012345678");
        var rucInvalido = ruc[..10] + ((ruc[^1] - '0' + 1) % 10).ToString(); // cambia DV
        Assert.That(() => DocumentoIdentidad.Crear(TipoDocumento.Ruc, rucInvalido),
            Throws.TypeOf<BusinessRuleException>());
    }

    // ===== DNI =====

    [Test]
    public void Crear_Dni_valido()
    {
        var doc = DocumentoIdentidad.Crear(TipoDocumento.Dni, "08661899");
        Assert.That(doc.EsDni, Is.True);
        Assert.That(doc.Numero, Is.EqualTo("08661899"));
        Assert.That(doc.SchemeId, Is.EqualTo("1"));
        Assert.That(doc.ToString(), Is.EqualTo("DNI 08661899"));
    }

    [Test]
    public void Crear_Dni_longitud_incorrecta_lanza()
    {
        Assert.That(() => DocumentoIdentidad.Crear(TipoDocumento.Dni, "1234567"),
            Throws.TypeOf<BusinessRuleException>());
        Assert.That(() => DocumentoIdentidad.Crear(TipoDocumento.Dni, "123456789"),
            Throws.TypeOf<BusinessRuleException>());
    }

    // ===== Tipos alfanuméricos (4/7/A/B/C/D) =====

    [TestCase(TipoDocumento.CarnetExtranjeria, "4")]
    [TestCase(TipoDocumento.Pasaporte, "7")]
    [TestCase(TipoDocumento.CedulaDiplomatica, "A")]
    [TestCase(TipoDocumento.DocIdentidadPaisResidenciaNoDomiciliado, "B")]
    [TestCase(TipoDocumento.TinPersonaNatural, "C")]
    [TestCase(TipoDocumento.InPersonaJuridica, "D")]
    public void Crear_Alfanumerico_valido_normaliza_y_mapea_schemeId(TipoDocumento tipo, string esperadoScheme)
    {
        var doc = DocumentoIdentidad.Crear(tipo, "ab-123");
        Assert.That(doc.Numero, Is.EqualTo("AB-123")); // normaliza a mayúsculas
        Assert.That(doc.SchemeId, Is.EqualTo(esperadoScheme));
    }

    [TestCase(TipoDocumento.CarnetExtranjeria)]
    [TestCase(TipoDocumento.Pasaporte)]
    [TestCase(TipoDocumento.CedulaDiplomatica)]
    [TestCase(TipoDocumento.DocIdentidadPaisResidenciaNoDomiciliado)]
    [TestCase(TipoDocumento.TinPersonaNatural)]
    [TestCase(TipoDocumento.InPersonaJuridica)]
    public void Crear_Alfanumerico_vacio_o_con_caracteres_invalidos_lanza(TipoDocumento tipo)
    {
        Assert.That(() => DocumentoIdentidad.Crear(tipo, " "),
            Throws.TypeOf<BusinessRuleException>());

        Assert.That(() => DocumentoIdentidad.Crear(tipo, "ABC*123"),
            Throws.TypeOf<BusinessRuleException>()); // * no permitido

        // Longitud > 15
        Assert.That(() => DocumentoIdentidad.Crear(tipo, "ABCDEFGHIJKLMNO1"),
            Throws.TypeOf<BusinessRuleException>());
    }

    // ===== SinDocumento (interno) =====

    [Test]
    public void Crear_SinDocumento_numero_vacio_y_schemeId_invalido()
    {
        var doc = DocumentoIdentidad.Crear(TipoDocumento.SinDocumento, null);
        Assert.That(doc.Numero, Is.EqualTo(string.Empty));
        Assert.That(() => { var _ = doc.SchemeId; }, Throws.TypeOf<BusinessRuleException>());
        Assert.That(doc.ToString(), Is.EqualTo("Sin documento"));
    }

    // ===== Detección y Try* =====

    [Test]
    public void FromNumeroDetectandoTipo_detecta_ruc_y_dni()
    {
        var ruc = BuildRuc("1012345678");
        var d1 = DocumentoIdentidad.FromNumeroDetectandoTipo(ruc);
        var d2 = DocumentoIdentidad.FromNumeroDetectandoTipo("12345678");

        Assert.That(d1.EsRuc, Is.True);
        Assert.That(d1.Numero, Is.EqualTo(ruc));
        Assert.That(d2.EsDni, Is.True);
        Assert.That(d2.Numero, Is.EqualTo("12345678"));
    }

    [Test]
    public void FromNumeroDetectandoTipo_no_detecta_otras_longitudes()
    {
        Assert.That(() => DocumentoIdentidad.FromNumeroDetectandoTipo("XYZ"),
            Throws.TypeOf<BusinessRuleException>());
    }

    [Test]
    public void TryCrear_y_TryFrom_funcionan()
    {
        var ruc = BuildRuc("2012345678");
        Assert.That(DocumentoIdentidad.TryCrear(TipoDocumento.Ruc, ruc, out var okRuc), Is.True);
        Assert.That(okRuc, Is.Not.Null);

        Assert.That(DocumentoIdentidad.TryCrear(TipoDocumento.Dni, "1234567", out var badDni), Is.False);
        Assert.That(badDni, Is.Null);

        Assert.That(DocumentoIdentidad.TryFromNumeroDetectandoTipo(ruc, out var okRuc2), Is.True);
        Assert.That(okRuc2!.EsRuc, Is.True);

        Assert.That(DocumentoIdentidad.TryFromNumeroDetectandoTipo("ABC", out var bad), Is.False);
        Assert.That(bad, Is.Null);
    }

    // ===== Heurísticas y banderas =====

    [Test]
    public void EsRuc10_y_EsRuc20()
    {
        var ruc10 = DocumentoIdentidad.Crear(TipoDocumento.Ruc, BuildRuc("1012345678"));
        var ruc20 = DocumentoIdentidad.Crear(TipoDocumento.Ruc, BuildRuc("2012345678"));

        Assert.That(ruc10.EsRuc10, Is.True);
        Assert.That(ruc10.EsRuc20, Is.False);

        Assert.That(ruc20.EsRuc20, Is.True);
        Assert.That(ruc20.EsRuc10, Is.False);
    }

    [Test]
    public void EsNoDomiciliado_true_para_B_C_D()
    {
        Assert.That(DocumentoIdentidad.Crear(TipoDocumento.DocIdentidadPaisResidenciaNoDomiciliado, "X1").EsNoDomiciliado, Is.True);
        Assert.That(DocumentoIdentidad.Crear(TipoDocumento.TinPersonaNatural, "X2").EsNoDomiciliado, Is.True);
        Assert.That(DocumentoIdentidad.Crear(TipoDocumento.InPersonaJuridica, "X3").EsNoDomiciliado, Is.True);

        Assert.That(DocumentoIdentidad.Crear(TipoDocumento.Pasaporte, "P1").EsNoDomiciliado, Is.False);
        Assert.That(DocumentoIdentidad.Crear(TipoDocumento.CarnetExtranjeria, "C1").EsNoDomiciliado, Is.False);
        Assert.That(DocumentoIdentidad.Crear(TipoDocumento.Ruc, BuildRuc("2012345678")).EsNoDomiciliado, Is.False);
        Assert.That(DocumentoIdentidad.Crear(TipoDocumento.Dni, "12345678").EsNoDomiciliado, Is.False);
    }

    // ===== ToString de algunos tipos =====

    [Test]
    public void ToString_humano_para_varios_tipos()
    {
        var ruc = DocumentoIdentidad.Crear(TipoDocumento.Ruc, BuildRuc("2012345678"));
        var dni = DocumentoIdentidad.Crear(TipoDocumento.Dni, "12345678");
        var pas = DocumentoIdentidad.Crear(TipoDocumento.Pasaporte, "ab-123");

        Assert.That(ruc.ToString(), Does.StartWith("RUC "));
        Assert.That(dni.ToString(), Does.StartWith("DNI "));
        Assert.That(pas.ToString(), Is.EqualTo("Pasaporte AB-123"));
    }
}
