using System;
using ListaPreciosBC.Domain.ValueObjects;
using SharedKernel.Exceptions;

namespace ListaPreciosBC.Application.UseCases
{
    /// <summary>
    /// Utilidades reutilizables para mapear datos primitivos de columnas hacia los value objects
    /// del dominio y viceversa.
    /// </summary>
    internal static class ColumnaPrecioApplicationMapper
    {
        public static ConfiguracionColumnaPrecio CrearConfiguracionColumna(
            byte numeroColumna,
            string nombre,
            ModoValorizacionColumna modo,
            bool esBaseDeclarado,
            bool visible,
            byte orden,
            string tipoColumnaCodigo,
            string? tipoReglaGlobalCodigo,
            decimal? valorReglaGlobal)
        {
            if (string.IsNullOrWhiteSpace(tipoColumnaCodigo))
                throw new BusinessRuleException("El tipo de columna es obligatorio.");

            var tipo = TipoColumnaPrecio.DesdeCodigo(tipoColumnaCodigo);
            ValidarBanderaBase(esBaseDeclarado, tipo);

            var regla = CrearRegla(tipo, tipoReglaGlobalCodigo, valorReglaGlobal);

            return ConfiguracionColumnaPrecio.Crear(
                id: IdentificadorColumnaPrecio.DesdeNumero(numeroColumna),
                nombre: NombreColumnaPrecio.Crear(nombre),
                modo: modo ?? throw new ArgumentNullException(nameof(modo)),
                tipo: tipo,
                reglaGlobal: regla,
                esBase: tipo.EsBase,
                visible: visible,
                orden: orden);
        }

        public static (string? TipoCodigo, decimal? Valor) ExtraerRegla(ReglaGlobalColumnaPrecio? regla)
            => regla is null ? (null, null) : (regla.Tipo.Codigo, regla.Valor);

        private static void ValidarBanderaBase(bool esBaseDeclarado, TipoColumnaPrecio tipo)
        {
            if (esBaseDeclarado && !tipo.EsBase)
                throw new BusinessRuleException("Solo las columnas de tipo Base pueden marcarse como base.");
            if (!esBaseDeclarado && tipo.EsBase)
                throw new BusinessRuleException("Las columnas de tipo Base deben marcarse como base.");
        }

        private static ReglaGlobalColumnaPrecio? CrearRegla(
            TipoColumnaPrecio tipo,
            string? tipoReglaGlobalCodigo,
            decimal? valorReglaGlobal)
        {
            if (tipo.EsGlobal)
            {
                if (string.IsNullOrWhiteSpace(tipoReglaGlobalCodigo) || valorReglaGlobal is null)
                    throw new BusinessRuleException("Las columnas globales requieren tipo y valor de regla.");

                var tipoRegla = TipoReglaGlobalColumnaPrecio.DesdeCodigo(tipoReglaGlobalCodigo);
                return ReglaGlobalColumnaPrecio.Crear(tipoRegla, valorReglaGlobal.Value);
            }

            if ((tipoReglaGlobalCodigo is not null && tipoReglaGlobalCodigo.Trim().Length > 0)
                || valorReglaGlobal is not null)
            {
                if (!tipo.EsMinimoPermitido)
                    throw new BusinessRuleException("Solo las columnas globales o de mínimo permitido pueden tener regla global.");

                if (string.IsNullOrWhiteSpace(tipoReglaGlobalCodigo) || valorReglaGlobal is null)
                    throw new BusinessRuleException("Tipo y valor de la regla global deben especificarse juntos.");

                var tipoRegla = TipoReglaGlobalColumnaPrecio.DesdeCodigo(tipoReglaGlobalCodigo);
                return ReglaGlobalColumnaPrecio.Crear(tipoRegla, valorReglaGlobal.Value);
            }

            return null;
        }
    }
}
