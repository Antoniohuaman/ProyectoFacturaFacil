// SharedKernel.Tests/ValueObjects/TenantIdTests.cs
using System;
using NUnit.Framework;
using SharedKernel.ValueObjects;

namespace SharedKernel.Tests.ValueObjects;

[TestFixture]
public class TenantIdTests
{
    [Test]
    public void New_Genera_Id_NoVacio_Y_Distinto()
    {
        var a = TenantId.New();
        var b = TenantId.New();

        Assert.That(a.Value, Is.Not.EqualTo(Guid.Empty));
        Assert.That(b.Value, Is.Not.EqualTo(Guid.Empty));
        Assert.That(a, Is.Not.EqualTo(b));
    }

    [Test]
    public void From_ConGuidVacio_LanzaExcepcion()
    {
        Assert.That(() => TenantId.From(Guid.Empty), Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void From_ConGuidValido_CreaTenantId()
    {
        var g = Guid.NewGuid();
        var id = TenantId.From(g);

        Assert.That(id.Value, Is.EqualTo(g));
        Assert.That(id.IsEmpty, Is.False);
    }

    [TestCase("d2719b07-6c3a-4b6e-86a1-5a6f3f3c7f10")]
    [TestCase("D2719B07-6C3A-4B6E-86A1-5A6F3F3C7F10")]
    public void FromString_Valido_CreaTenantId(string s)
    {
        var id = TenantId.FromString(s);

        Assert.That(id.IsEmpty, Is.False);
        Assert.That(id.Value, Is.EqualTo(Guid.Parse(s)));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("no-guid")]
    public void FromString_Invalido_LanzaExcepcion(string? s)
    {
        Assert.That(() => TenantId.FromString(s!), Throws.Exception);
    }

    [Test]
    public void TryParse_Valido_True_Y_Asignado()
    {
        var g = Guid.NewGuid().ToString();

        var ok = TenantId.TryParse(g, out var id);

        Assert.That(ok, Is.True);
        Assert.That(id.Value, Is.EqualTo(Guid.Parse(g)));
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("not-a-guid")]
    [TestCase("00000000-0000-0000-0000-000000000000")]
    public void TryParse_Invalido_False(string s)
    {
        var ok = TenantId.TryParse(s, out var id);

        Assert.That(ok, Is.False);
        Assert.That(id.IsEmpty, Is.True); // default struct
    }

    [Test]
    public void ToString_Devuelve_Guid_String()
    {
        var g = Guid.NewGuid();
        var id = TenantId.From(g);

        Assert.That(id.ToString(), Is.EqualTo(g.ToString()));
    }

    [Test]
    public void Casts_Explicitos_Ok()
    {
        var g = Guid.NewGuid();

        var id = (TenantId)g;
        var round = (Guid)id;

        Assert.That(round, Is.EqualTo(g));
    }

    [Test]
    public void Igualdad_PorValor()
    {
        var g = Guid.NewGuid();
        var a = TenantId.From(g);
        var b = TenantId.From(g);
        var c = TenantId.New();

        Assert.That(a, Is.EqualTo(b));
        Assert.That(a, Is.Not.EqualTo(c));
    }
}
