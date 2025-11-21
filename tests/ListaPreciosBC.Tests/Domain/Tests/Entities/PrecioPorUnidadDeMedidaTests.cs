using ListaPreciosBC.Domain.Entities;
using ListaPreciosBC.Domain.ValueObjects;
using NUnit.Framework;
using SharedKernel.ValueObjects;

namespace ListaPreciosBC.Tests.Domain.Entities;

[TestFixture]
public class PrecioPorUnidadDeMedidaTests
{
    [Test]
    public void Constructor_deberia_inicializar_propiedades_y_habilitar_por_defecto()
    {
        // Arrange
        var columnaId = default(IdentificadorColumnaPrecio)!;
        var unidad = default(UnidadDeMedida)!;

        // Act
        var entidad = new PrecioPorUnidadDeMedida(columnaId, unidad);

        // Assert
        Assert.That(entidad.ColumnaId, Is.EqualTo(columnaId));
        Assert.That(entidad.UnidadDeMedida, Is.EqualTo(unidad));
        Assert.That(entidad.EstaHabilitada, Is.True);
    }

    [Test]
    public void Deshabilitar_deberia_marcar_entidad_como_no_habilitada()
    {
        var entidad = new PrecioPorUnidadDeMedida(
            default!, default!, estaHabilitada: true);

        entidad.Deshabilitar();

        Assert.That(entidad.EstaHabilitada, Is.False);
    }

    [Test]
    public void Habilitar_deberia_marcar_entidad_como_habilitada()
    {
        var entidad = new PrecioPorUnidadDeMedida(
            default!, default!, estaHabilitada: false);

        entidad.Habilitar();

        Assert.That(entidad.EstaHabilitada, Is.True);
    }
}
