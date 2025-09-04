using System.Collections.Generic;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    public sealed class RegistrarConfiguracionEmpresaOutputDto
    {
        public EmpresaId EmpresaId { get; }
        public Ruc Ruc { get; }
        public string RazonSocial { get; }
        public string? NombreComercial { get; }
        public DomicilioFiscal DireccionFiscal { get; }
        public Moneda MonedaBase { get; }

        public ConfiguracionEmpresa.EstablecimientoRead? EstablecimientoPrincipal { get; }
        public ConfiguracionEmpresa.FormaPagoRead? FormaDePagoPorDefecto { get; }
        public ConfiguracionEmpresa.UnidadMedidaRead? UnidadDeMedidaPorDefecto { get; }

        public IReadOnlyList<ConfiguracionEmpresa.FormaPagoRead> FormasDePago { get; }
        public IReadOnlyList<ConfiguracionEmpresa.UnidadMedidaRead> UnidadesDeMedida { get; }

        public RegistrarConfiguracionEmpresaOutputDto(
            EmpresaId empresaId,
            Ruc ruc,
            string razonSocial,
            string? nombreComercial,
            DomicilioFiscal direccionFiscal,
            Moneda monedaBase,
            ConfiguracionEmpresa.EstablecimientoRead? establecimientoPrincipal,
            ConfiguracionEmpresa.FormaPagoRead? formaDePagoPorDefecto,
            ConfiguracionEmpresa.UnidadMedidaRead? unidadDeMedidaPorDefecto,
            IReadOnlyList<ConfiguracionEmpresa.FormaPagoRead> formasDePago,
            IReadOnlyList<ConfiguracionEmpresa.UnidadMedidaRead> unidadesDeMedida)
        {
            EmpresaId = empresaId;
            Ruc = ruc;
            RazonSocial = razonSocial;
            NombreComercial = nombreComercial;
            DireccionFiscal = direccionFiscal;
            MonedaBase = monedaBase;
            EstablecimientoPrincipal = establecimientoPrincipal;
            FormaDePagoPorDefecto = formaDePagoPorDefecto;
            UnidadDeMedidaPorDefecto = unidadDeMedidaPorDefecto;
            FormasDePago = formasDePago;
            UnidadesDeMedida = unidadesDeMedida;
        }
    }
}
