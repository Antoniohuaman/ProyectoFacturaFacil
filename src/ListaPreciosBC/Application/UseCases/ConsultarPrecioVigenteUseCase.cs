using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Domain.Repositories;    // IListaPrecioRepository, IPrecioProductoRepository
using ListaPreciosBC.Domain.Aggregates;      // ListaPrecio, PrecioProducto
using ListaPreciosBC.Domain.ValueObjects;    // IdentificadorColumnaPrecio
using SharedKernel.ValueObjects;             // Sku
using SharedKernel.Exceptions;               // NotFoundException

namespace ListaPreciosBC.Application.UseCases
{
    /// <summary>
    /// Consulta el precio vigente para un SKU en una columna dada, a una fecha y para una cantidad específica.
    /// Reglas:
    ///  - Debe existir lista activa.
    ///  - La columna debe existir (el modo lo determina dominio).
    ///  - Debe existir el agregado PrecioProducto para el SKU.
    ///  - Si no hay precio vigente, se lanza NotFoundException.
    /// </summary>
    public sealed class ConsultarPrecioVigenteUseCase
    {
        public readonly record struct Request(
            string Sku,
            byte ColumnaNumero,
            int Cantidad,
            DateTimeOffset? Fecha = null
        );

        public readonly record struct Response(
            string Sku,
            byte ColumnaNumero,
            int Cantidad,
            DateTimeOffset Fecha,
            decimal Monto,
            bool IncluyeImpuesto,
            string Moneda,
            string ModoColumna,   // "Fijo" | "PorVolumen"
            int VersionAgregado   // versión del PrecioProducto consultado
        );

        private readonly IListaPrecioRepository _listaRepo;
        private readonly IPrecioProductoRepository _precioRepo;

        public ConsultarPrecioVigenteUseCase(
            IListaPrecioRepository listaRepo,
            IPrecioProductoRepository precioRepo)
        {
            _listaRepo = listaRepo ?? throw new ArgumentNullException(nameof(listaRepo));
            _precioRepo = precioRepo ?? throw new ArgumentNullException(nameof(precioRepo));
        }

        public async Task<Response> Handle(Request req, CancellationToken ct)
        {
            // 1) Lista activa
            var lista = await _listaRepo.ObtenerActivaAsync(ct);
            if (lista is null)
                throw new NotFoundException("No existe lista de precios activa.");

            // 2) Validar columna existente
            var colId = IdentificadorColumnaPrecio.DesdeNumero(req.ColumnaNumero);
            var columnaCfg = lista.Plantilla.Columnas.SingleOrDefault(c => c.Id.Equals(colId));
            if (columnaCfg is null)
                throw new NotFoundException($"La columna #{req.ColumnaNumero} no existe en la plantilla activa.");

            // 3) Obtener agregado de precio por SKU
            var sku = Sku.Crear(req.Sku);
            var agregado = await _precioRepo.ObtenerPorSkuAsync(sku, ct);
            if (agregado is null)
                throw new NotFoundException($"No existe PrecioProducto para el SKU {req.Sku}.");

            // 4) Resolver precio vigente
            var fecha = req.Fecha ?? DateTimeOffset.UtcNow;
            var resuelto = agregado.ObtenerPrecioVigente(colId, fecha, req.Cantidad);
            if (resuelto is null)
                throw new NotFoundException($"No hay precio vigente para SKU {req.Sku} en la columna {req.ColumnaNumero}.");

            // Asumimos que PrecioResuelto expone un Valor de tipo ValorPrecio con Monto/Moneda/IncluyeImpuesto
            var valor = resuelto.Valor;

            return new Response(
                Sku: sku.Valor,
                ColumnaNumero: req.ColumnaNumero,
                Cantidad: req.Cantidad,
                Fecha: fecha,
                Monto: valor.Monto,
                IncluyeImpuesto: valor.IncluyeImpuesto,
                Moneda: valor.Importe.Moneda.Codigo,  // ADAPTA si tu Moneda expone otra propiedad (p.ej., CodigoIso)
                ModoColumna: columnaCfg.Modo.ToString(),
                VersionAgregado: agregado.Version
            );
        }
    }
}
