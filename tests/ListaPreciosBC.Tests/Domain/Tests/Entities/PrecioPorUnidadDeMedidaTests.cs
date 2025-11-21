using System;
using ListaPreciosBC.Domain.Entities;
using ListaPreciosBC.Domain.ValueObjects;
using NUnit.Framework;
using SharedKernel.ValueObjects;

namespace ListaPreciosBC.Tests.Domain.Entities;

[TestFixture]
public class PrecioPorUnidadDeMedidaTests
{
    private static IdentificadorColumnaPrecio Columna(byte numero = 1) => IdentificadorColumnaPrecio.DesdeNumero(numero);
    private static UnidadDeMedida Unidad(string codigo = "NIU") => SharedKernel.ValueObjects.UnidadDeMedida.From(codigo);
    private static ValorPrecio Precio(decimal monto) => ValorPrecio.DesdeMonto(monto);
    private static PeriodoVigencia VigenciaHoy() => PeriodoVigencia.Crear(DateTimeOffset.UtcNow.Date, null);

    [Test]
    public void Constructor_deberia_inicializar_propiedades_y_habilitar_por_defecto()
    {
        var columnaId = Columna();
        var unidad = Unidad();

        var entidad = new PrecioPorUnidadDeMedida(columnaId, unidad);

        Assert.That(entidad.ColumnaId, Is.EqualTo(columnaId));
        Assert.That(entidad.UnidadDeMedida, Is.EqualTo(unidad));
        Assert.That(entidad.EstaHabilitada, Is.True);
        Assert.That(entidad.EstaVacia, Is.True);
    }

    [Test]
    public void Deshabilitar_deberia_marcar_entidad_como_no_habilitada()
    {
        var entidad = new PrecioPorUnidadDeMedida(
            Columna(), Unidad(), estaHabilitada: true);

        entidad.Deshabilitar();

        Assert.That(entidad.EstaHabilitada, Is.False);
    }

    [Test]
    public void Habilitar_deberia_marcar_entidad_como_habilitada()
    {
        var entidad = new PrecioPorUnidadDeMedida(
            Columna(), Unidad(), estaHabilitada: false);

        entidad.Habilitar();

        Assert.That(entidad.EstaHabilitada, Is.True);
    }

    [Test]
    public void EstablecerPrecioFijo_guarda_valor_y_vigencia_y_exclusividad()
    {
        var entidad = new PrecioPorUnidadDeMedida(Columna(), Unidad());

        entidad.EstablecerPrecioFijo(Precio(10m), VigenciaHoy());

        Assert.Multiple(() =>
        {
            Assert.That(entidad.TienePrecioFijo, Is.True);
            Assert.That(entidad.PrecioFijo!.Monto, Is.EqualTo(10m));
            Assert.That(entidad.TieneMatrizVolumen, Is.False);
        });

        entidad.EliminarPrecioFijo();
        Assert.That(entidad.TienePrecioFijo, Is.False);
        Assert.That(entidad.EstaVacia, Is.True);
    }

    [Test]
    public void EstablecerMatrizVolumen_sustituye_precio_fijo()
    {
        var entidad = new PrecioPorUnidadDeMedida(Columna(), Unidad());
        entidad.EstablecerPrecioFijo(Precio(5m), VigenciaHoy());

        var matriz = MatrizVolumen.Crear(new[] { TramoVolumen.Crear(1, 10, Precio(4m)) });
        entidad.EstablecerMatrizVolumen(matriz);

        Assert.Multiple(() =>
        {
            Assert.That(entidad.TieneMatrizVolumen, Is.True);
            Assert.That(entidad.MatrizVolumen, Is.EqualTo(matriz));
            Assert.That(entidad.TienePrecioFijo, Is.False);
        });

        entidad.EliminarMatrizVolumen();
        Assert.That(entidad.TieneMatrizVolumen, Is.False);
    }
}
