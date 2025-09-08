using System;
using SharedKernel.ValueObjects;

namespace IndicadoresNegocioBC.Domain.ValueObjects
{
    /// <summary>
    /// Value Object que representa el segmento/filtro con el que se consultan los indicadores.
    /// 
    /// En IndicadoresNegocioBC los usuarios NO escriben datos: el segmento se arma con
    /// valores ya existentes (catálogos/tenant). Este VO solo encapsula y valida el filtro.
    ///
    /// Componentes:
    ///  - EmpresaId (obligatorio): ámbito de la consulta.
    ///  - Establecimiento (opcional): cuando se desea filtrar a una tienda/sucursal específica.
    ///    Si está presente, DEBE pertenecer a la misma empresa.
    ///  - Moneda (obligatoria): divisa del filtro (PEN, USD, ...).
    ///
    /// Casos de uso:
    ///  - "Empresa completa + Moneda"  -> todos los establecimientos de la empresa en esa moneda.
    ///  - "Establecimiento + Moneda"   -> un establecimiento concreto en esa moneda.
    ///
    /// Igualdad por valor. Inmutable.
    /// </summary>
    public sealed record SegmentoIndicador
    {
        /// <summary>Identificador de la empresa (tenant) a la que pertenece el segmento.</summary>
        public Guid EmpresaId { get; }

    /// <summary>
    /// Identificador de establecimiento específico (opcional). Si es null, el segmento aplica a TODOS
    /// los establecimientos de la empresa.
    /// </summary>
    public EstablecimientoId? EstablecimientoId { get; }

        /// <summary>Moneda de trabajo (VO Moneda). Obligatoria.</summary>
    public SharedKernel.ValueObjects.Moneda Moneda { get; }

        /// <summary>Indica si el segmento abarca a toda la empresa (sin establecimiento específico).</summary>
    public bool EsEmpresaCompleta => EstablecimientoId is null;

    private SegmentoIndicador(Guid empresaId, EstablecimientoId? establecimientoId, SharedKernel.ValueObjects.Moneda moneda)
        {
            if (empresaId == Guid.Empty)
                throw new ArgumentException("EmpresaId no puede ser vacío.", nameof(empresaId));

            Moneda = moneda ?? throw new ArgumentNullException(nameof(moneda));

            EmpresaId = empresaId;
            EstablecimientoId = establecimientoId;
        }

        /// <summary>
        /// Crea un segmento para TODOS los establecimientos de la empresa en la moneda indicada.
        /// </summary>
        public static SegmentoIndicador ParaEmpresa(Guid empresaId, SharedKernel.ValueObjects.Moneda moneda) =>
            new(empresaId, establecimientoId: null, moneda);

        /// <summary>
        /// Crea un segmento para un establecimiento específico y la moneda indicada.
        /// </summary>
        public static SegmentoIndicador ParaEstablecimiento(Guid empresaId, EstablecimientoId establecimientoId, SharedKernel.ValueObjects.Moneda moneda)
        {
            if (establecimientoId is null) throw new ArgumentNullException(nameof(establecimientoId));
            return new(empresaId, establecimientoId, moneda);
        }

        /// <summary>
        /// Devuelve una copia del segmento fijando/actualizando el EstablecimientoId.
        /// </summary>
        public SegmentoIndicador ConEstablecimiento(EstablecimientoId establecimientoId)
        {
            if (establecimientoId is null) throw new ArgumentNullException(nameof(establecimientoId));
            return new SegmentoIndicador(EmpresaId, establecimientoId, Moneda);
        }

        /// <summary>
        /// Devuelve una copia del segmento sin Establecimiento (empresa completa).
        /// </summary>
        public SegmentoIndicador ParaTodaLaEmpresa() =>
            new SegmentoIndicador(EmpresaId, null, Moneda);

        public override string ToString()
        {
            var scope = EsEmpresaCompleta
                ? "Empresa"
                : $"Establecimiento:{EstablecimientoId}";
            return $"{scope} | {Moneda.Codigo}";
        }
    }
}