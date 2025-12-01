using System;
using GestionCobranzasBC.Domain.Entities;
using NUnit.Framework;
using SharedKernel.Exceptions;

namespace GestionCobranzasBC.Tests.Domain.EntitieTests;

[TestFixture]
public class DocumentoOrigenTests
{
    [Test]
    public void Crear_con_datos_validos_inicializa_correctamente()
    {
        // Arrange
        var fecha = new DateOnly(2025, 3, 10);

        // Act
        var doc = DocumentoOrigen.Crear(
            "FE01-00000033",
            "01",
            "FE01",
            "00000033",
            fecha,
            "pen",
            2500m);

        // Assert
        Assert.That(doc.ComprobanteId, Is.EqualTo("FE01-00000033"));
        Assert.That(doc.TipoDocumento, Is.EqualTo("01"));
        Assert.That(doc.Serie, Is.EqualTo("FE01"));
        Assert.That(doc.Numero, Is.EqualTo("00000033"));
        Assert.That(doc.FechaEmision, Is.EqualTo(fecha));
        Assert.That(doc.MonedaCodigo, Is.EqualTo("PEN"));
        Assert.That(doc.ImporteTotal, Is.EqualTo(2500m));
    }

    [Test]
    public void Crear_con_importe_no_valido_lanza_excepcion()
    {
        // Act & Assert
        var ex = Assert.Throws<BusinessRuleException>(() =>
        {
            _ = DocumentoOrigen.Crear(
                "FE01-00000033",
                "01",
                "FE01",
                "00000033",
                new DateOnly(2025, 3, 10),
                "PEN",
                0m);
        });

        Assert.That(ex!.Message, Does.Contain("importe total del documento"));
    }

    [Test]
    public void Crear_con_campos_obligatorios_vacios_lanza_excepcion()
    {
        // Act & Assert
        var ex = Assert.Throws<BusinessRuleException>(() =>
        {
            _ = DocumentoOrigen.Crear(
                "",
                "",
                "",
                "",
                new DateOnly(2025, 3, 10),
                "",
                100m);
        });

        Assert.That(ex!.Message, Does.Contain("obligatorio"));
    }
}
