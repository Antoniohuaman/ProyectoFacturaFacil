using SharedKernel.Exceptions;

namespace GestionCobranzasBC.Domain.ValueObjects;

/// <summary>
/// Destino del cobro: caja interna o cuenta/banco externo.
/// </summary>
public sealed record CajaDestino
{
    public string Nombre { get; }
    public bool EsCajaInterna { get; }

    private CajaDestino(string nombre, bool esCajaInterna)
    {
        Nombre = nombre;
        EsCajaInterna = esCajaInterna;
    }

    public static CajaDestino Caja(string nombre)
        => Crear(nombre, true);

    public static CajaDestino Banco(string nombre)
        => Crear(nombre, false);

    public static CajaDestino Crear(string nombre)
        => Caja(nombre);

    private static CajaDestino Crear(string nombre, bool esCajaInterna)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new BusinessRuleException("El nombre de la caja o banco de destino es obligatorio.");
        }

        var normalizado = nombre.Trim();

        if (normalizado.Length > 100)
        {
            throw new BusinessRuleException("El nombre de la caja o banco de destino no puede superar los 100 caracteres.");
        }

        return new CajaDestino(normalizado, esCajaInterna);
    }

    public override string ToString() => Nombre;
}
