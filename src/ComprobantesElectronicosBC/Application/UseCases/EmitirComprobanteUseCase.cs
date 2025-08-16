using SharedKernel.ValueObjects;
using System;
using System.Threading;
using System.Threading.Tasks;
using ComprobantesElectronicosBC.Application.DTOs;
using ComprobantesElectronicosBC.Domain.Aggregates;
using ComprobantesElectronicosBC.Domain.Repositories;
using ComprobantesElectronicosBC.Domain.ValueObjects;

namespace ComprobantesElectronicosBC.Application.UseCases
{
    public sealed class EmitirComprobanteUseCase
    {
        private readonly IComprobanteRepository _repo;
        private readonly IUnitOfWork _uow;

        public EmitirComprobanteUseCase(IComprobanteRepository repo, IUnitOfWork uow)
        {
            _repo = repo;
            _uow  = uow;
        }

        public async Task<EmitirComprobanteOutput> Handle(EmitirComprobanteInput input, CancellationToken ct = default)
        {
            // ---- VOs base ----
            var tipo    = TipoDeComprobante.Create(input.TipoCodigo);
            var moneda  = SharedKernel.ValueObjects.Moneda.Create(input.MonedaCodigo);
            var nowUtc  = DateTime.UtcNow;
            var emision = FechaEmision.Create(input.FechaEmision, tipo.Codigo, nowUtc);

            var formaPago = input.FormaPagoCodigo switch
            {
                "10" => string.IsNullOrWhiteSpace(input.MetodoPagoCodigo)
                        ? FormaDePago.Contado()
                        : FormaDePago.ContadoPredefinido(input.MetodoPagoCodigo!, input.MetodoPagoNombre),
                "20" => FormaDePago.Credito(),
                _    => throw new ArgumentException("Forma de pago inválida (use \"10\" o \"20\").")
            };

            var vencimiento = FechaVencimiento.ParaFormaDePago(formaPago, emision.Fecha, input.DiasCredito);

            // ---- Emisor ----
            var dirEmisor = DireccionPostal.Create(
                input.EmisorUbigeo, input.EmisorDireccion,
                input.EmisorDepartamento, input.EmisorProvincia, input.EmisorDistrito);

            var emisor = EmisorSnapshot.Create(input.EmisorRuc, input.EmisorRazonSocial, dirEmisor);

            // ---- Cliente ----
            var docCli = DocumentoIdentidad.Create(input.ClienteDocTipo, input.ClienteDocNumero);

            DireccionPostal? dirCli = null;
            if (!string.IsNullOrWhiteSpace(input.ClienteDireccion) || !string.IsNullOrWhiteSpace(input.ClienteUbigeo))
            {
                dirCli = DireccionPostal.FromCliente(
                    docCli, input.ClienteUbigeo, input.ClienteDireccion,
                    input.ClienteDepartamento, input.ClienteProvincia, input.ClienteDistrito);
            }

            var cliente = ClienteSnapshot.Create(docCli, input.ClienteNombre, dirCli);

            // ---- Crear agregado (Borrador) ----
            var agg = ComprobanteElectronico.CrearBorrador(
                tipo, emisor, cliente, moneda, emision, formaPago, vencimiento, nowUtc);

            // ---- Agregar líneas ----
            foreach (var l in input.Lineas)
            {
                var descripcion = DescripcionProducto.Create(l.Nombre, l.Detalle);
                var um          = UnidadDeMedida.Create(l.UmCodigo, l.UmNombre);
                var qty         = Cantidad.Create(l.Cantidad);
                var precio      = ImporteMonetario.Create(l.PrecioUnitario, moneda);
                var impuesto    = ImpuestoIGV.Create(l.AfectacionCode, l.IgvRate);

                var descLinea =
                    l.DescuentoPorcentaje is not null ? DescuentoLinea.FromPorcentaje(l.DescuentoPorcentaje.Value) :
                    l.DescuentoMonto      is not null ? DescuentoLinea.FromMonto(l.DescuentoMonto.Value) :
                                                        DescuentoLinea.None;

                agg.AgregarLinea(descripcion, um, qty, precio, impuesto, l.PrecioIncluyeIgv, descLinea);
            }

            // ---- Descuento global (opcional) ----
            if (input.DescuentoGlobalPorcentaje is not null)
                agg.CambiarDescuentoGlobal(DescuentoGlobal.FromPorcentaje(input.DescuentoGlobalPorcentaje.Value));
            else if (input.DescuentoGlobalMonto is not null)
                agg.CambiarDescuentoGlobal(DescuentoGlobal.FromMonto(input.DescuentoGlobalMonto.Value));

            // ---- Serie / Número + Emitir ----
            agg.AsignarSerieYNumero(input.Serie, input.Numero);
            agg.Emitir(); // Draft/Corregir → Enviado

            // ---- Guardar ----
            await _repo.AddAsync(agg, ct);
            await _uow.SaveChangesAsync(ct);

            // ---- Salida ----
            return new EmitirComprobanteOutput(
                agg.ComprobanteId,
                agg.Tipo.Codigo,
                agg.SerieNumero!.Serie, agg.SerieNumero.Numero,
                agg.Emision.Fecha,
                agg.EstadoCodigo,
                agg.SubtotalBase,
                agg.DescuentoGlobalMonto,
                agg.IgvTotal,
                agg.Total
            );
        }
    }
}
