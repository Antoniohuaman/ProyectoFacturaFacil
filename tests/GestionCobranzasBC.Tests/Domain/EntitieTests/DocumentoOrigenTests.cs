using System;
using GestionCobranzasBC.Domain.Entities;
using NUnit.Framework;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionCobranzasBC.Tests.Domain.EntitieTests;

[TestFixture]
public class DocumentoOrigenTests
{
    [Test]
    public void Crear_con_datos_validos_inicializa_correctamente()
    {
        // Arrange
        var fecha = new DateOnly(2025, 3, 10);
        var comprobanteId = Guid.NewGuid();

        // Act
        var doc = DocumentoOrigen.Crear(
            comprobanteId,
            "fe01",
            "33",
            fecha,
            Moneda.PEN());

        // Assert
        Assert.That(doc.ComprobanteId, Is.EqualTo(comprobanteId));
        Assert.That(doc.Serie, Is.EqualTo("FE01"));
        Assert.That(doc.Numero, Is.EqualTo("00000033"));
        Assert.That(doc.NumeroCompleto, Is.EqualTo("FE01-00000033"));
        Assert.That(doc.Moneda.Codigo, Is.EqualTo("PEN"));
        Assert.That(doc.FechaEmision, Is.EqualTo(fecha));
    }

    [Test]
    public void Crear_con_moneda_nula_lanza_excepcion()
    {
        Assert.That(
            () => DocumentoOrigen.Crear(
                Guid.NewGuid(),
                "FE01",
                "00000033",
                new DateOnly(2025, 3, 10),
                null!),
            Throws.TypeOf<BusinessRuleException>());
    }

    [Test]
    public void Crear_con_campos_obligatorios_vacios_lanza_excepcion()
    {
        Assert.That(
            () => DocumentoOrigen.Crear(
                Guid.Empty,
                "",
                "",
                new DateOnly(2025, 3, 10),
                Moneda.PEN()),
            Throws.TypeOf<BusinessRuleException>());
    }
}
