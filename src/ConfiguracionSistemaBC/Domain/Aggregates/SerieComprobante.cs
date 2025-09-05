using System;
using System.Collections.Generic;
using SharedKernel.Events;                       // IDomainEvent
using SharedKernel.Exceptions;                  // BusinessRuleException
using SharedKernel.ValueObjects;                // EmpresaId, EstablecimientoId
using ConfiguracionSistemaBC.Domain.ValueObjects; // TipoComprobanteCodigo, SerieCodigo, TipoOperacion, Correlativo

namespace ConfiguracionSistemaBC.Domain.Aggregates
{
    /// <summary>
    /// AGGREGATE ROOT: Serie de Comprobante (catálogo + numerador).
    ///
    /// Responsabilidades:
    /// - Mantener la configuración local de una serie (tipo, serie, operación, establecimiento).
    /// - Exponer banderas de visibilidad (Habilitada) y selección (EsPorDefecto).
    /// - Proteger el invariante del numerador (reservar el siguiente correlativo sin colisiones).
    /// - Restringir edición/eliminación cuando ya fue usada.
    ///
    /// Identidad natural: (EmpresaId, Tipo, Serie).
    /// </summary>
    public sealed class SerieComprobante
    {
        // -------------------- Identidad y contexto --------------------
        public Guid Id { get; }
        public EmpresaId EmpresaId { get; }
        public TipoComprobanteCodigo Tipo { get; }
        public SerieCodigo Serie { get; private set; }
        public EstablecimientoId EstablecimientoId { get; private set; }
        public TipoOperacion TipoOperacion { get; private set; }

        // -------------------- Estado de catálogo / UI --------------------
        /// <summary>True si debe aparecer como opción principal en la UI. 
        /// La unicidad de "una default por tipo" se coordina fuera del aggregate.</summary>
        public bool EsPorDefecto { get; private set; }

        /// <summary>True si está visible/seleccionable en la UI. Si está false no permite emitir.</summary>
        public bool Habilitada { get; private set; }

        // -------------------- Numeración / concurrencia --------------------
        /// <summary>Correlativo que se reservará la próxima vez.</summary>
        public Correlativo Siguiente { get; private set; }

        /// <summary>True si alguna vez se reservó/emitió un correlativo.</summary>
        public bool FueUsada { get; private set; }

        /// <summary>Versión para concurrencia optimista.</summary>
        public int Version { get; private set; }

        // -------------------- Eventos --------------------
        private readonly List<IDomainEvent> _domainEvents = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
        private void AddEvent(IDomainEvent e) => _domainEvents.Add(e);
        public void ClearDomainEvents() => _domainEvents.Clear();

        // -------------------- Ctor privado (ORM) --------------------
    #pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
    private SerieComprobante() { } 

        private SerieComprobante(
            EmpresaId empresaId,
            TipoComprobanteCodigo tipo,
            SerieCodigo serie,
            EstablecimientoId establecimientoId,
            TipoOperacion tipoOperacion,
            Correlativo correlativoInicial,
            bool esPorDefecto,
            bool habilitada)
        {
            EmpresaId = empresaId ?? throw new ArgumentNullException(nameof(empresaId));
            Tipo = tipo ?? throw new ArgumentNullException(nameof(tipo));
            if (serie is null) throw new ArgumentNullException(nameof(serie));
            // Regla: prefijo de serie debe concordar con el tipo (F/B en MVP)
            SerieCodigo.ValidarSegunTipo(serie, tipo);

            Serie = serie;
            EstablecimientoId = establecimientoId ?? throw new ArgumentNullException(nameof(establecimientoId));
            TipoOperacion = tipoOperacion ?? TipoOperacion.Default;
            Siguiente = correlativoInicial ?? throw new ArgumentNullException(nameof(correlativoInicial));
            if (Siguiente.Valor < Correlativo.Min)
                throw new BusinessRuleException("El correlativo inicial debe ser >= 1.");

            EsPorDefecto = esPorDefecto;
            Habilitada = habilitada;

            Id = Guid.NewGuid();
            FueUsada = false;
            Version = 0;

            AddEvent(new Events.SerieComprobanteCreada(
                EmpresaId, Tipo, Serie, EstablecimientoId, TipoOperacion, Siguiente, EsPorDefecto, Habilitada));
        }

        // -------------------- Fábrica --------------------
        public static SerieComprobante Crear(
            EmpresaId empresaId,
            TipoComprobanteCodigo tipo,
            SerieCodigo serie,
            EstablecimientoId establecimientoId,
            TipoOperacion? tipoOperacion,
            Correlativo correlativoInicial,
            bool esPorDefecto = false,
            bool habilitada = true)
            => new(
                empresaId,
                tipo,
                serie,
                establecimientoId,
                tipoOperacion ?? TipoOperacion.Default,
                correlativoInicial,
                esPorDefecto,
                habilitada);

        // -------------------- Reglas de edición --------------------

        /// <summary>
        /// Cambia la serie (p.ej., FE01 → FE02). Solo permitido si la serie nunca fue usada.
        /// La unicidad (Tipo+Serie) se valida fuera del aggregate.
        /// </summary>
        public void CambiarSerie(SerieCodigo nuevaSerie)
        {
            if (nuevaSerie is null) throw new ArgumentNullException(nameof(nuevaSerie));
            if (FueUsada) throw new BusinessRuleException("No se puede cambiar la serie: ya fue usada en emisión.");
            SerieCodigo.ValidarSegunTipo(nuevaSerie, Tipo);

            if (nuevaSerie == Serie) return;
            Serie = nuevaSerie;
            Version++;
            AddEvent(new Events.SerieComprobanteActualizada(EmpresaId, Tipo, Serie));
        }

        /// <summary>
        /// Cambia el establecimiento asociado. Solo permitido si nunca fue usada.
        /// </summary>
        public void CambiarEstablecimiento(EstablecimientoId nuevoEstablecimientoId)
        {
            if (nuevoEstablecimientoId is null) throw new ArgumentNullException(nameof(nuevoEstablecimientoId));
            if (FueUsada) throw new BusinessRuleException("No se puede cambiar el establecimiento: la serie ya fue usada.");

            if (nuevoEstablecimientoId == EstablecimientoId) return;
            EstablecimientoId = nuevoEstablecimientoId;
            Version++;
            AddEvent(new Events.SerieComprobanteActualizada(EmpresaId, Tipo, Serie));
        }

        /// <summary>
        /// Cambia el Tipo de Operación (Cat.51). Permitido aún si fue usada (es metadata de emisión futura).
        /// </summary>
        public void CambiarTipoOperacion(TipoOperacion nuevaOperacion)
        {
            if (nuevaOperacion is null) throw new ArgumentNullException(nameof(nuevaOperacion));
            if (nuevaOperacion == TipoOperacion) return;

            TipoOperacion = nuevaOperacion;
            Version++;
            AddEvent(new Events.SerieComprobanteActualizada(EmpresaId, Tipo, Serie));
        }

        /// <summary>
        /// Marca o desmarca como "por defecto". Solo si está habilitada.
        /// La exclusividad (una por tipo) es responsabilidad externa.
        /// </summary>
        public void EstablecerPorDefecto(bool porDefecto)
        {
            if (!Habilitada && porDefecto)
                throw new BusinessRuleException("No se puede establecer por defecto una serie inhabilitada.");

            if (EsPorDefecto == porDefecto) return;
            EsPorDefecto = porDefecto;
            Version++;
            AddEvent(new Events.SerieComprobanteMarcadaPorDefecto(EmpresaId, Tipo, Serie, EsPorDefecto));
        }

        /// <summary>Oculta/inhabilita la serie para ser seleccionada en emisión.</summary>
        public void Inhabilitar()
        {
            if (!Habilitada) return;
            if (EsPorDefecto)
                throw new BusinessRuleException("No se puede inhabilitar una serie marcada como por defecto.");

            Habilitada = false;
            Version++;
            AddEvent(new Events.SerieComprobanteInhabilitada(EmpresaId, Tipo, Serie));
        }

        /// <summary>Vuelve a habilitar la serie para emisión.</summary>
        public void Habilitar()
        {
            if (Habilitada) return;
            Habilitada = true;
            Version++;
            AddEvent(new Events.SerieComprobanteHabilitada(EmpresaId, Tipo, Serie));
        }

        /// <summary>
        /// Ajusta manualmente el numerador SOLO hacia adelante (útil para migraciones/regularizaciones).
        /// </summary>
        public void AjustarNumeradorHaciaAdelante(Correlativo nuevoSiguiente)
        {
            if (nuevoSiguiente is null) throw new ArgumentNullException(nameof(nuevoSiguiente));
            if (nuevoSiguiente.Valor <= Siguiente.Valor)
                throw new BusinessRuleException("El ajuste del numerador debe ser hacia adelante.");

            Siguiente = nuevoSiguiente;
            Version++;
            AddEvent(new Events.NumeradorAjustado(EmpresaId, Tipo, Serie, Siguiente));
        }

        /// <summary>
        /// Indica si puede eliminarse físicamente. La política del sistema: 
        /// solo si nunca fue usada.
        /// </summary>
        public bool PuedeEliminar => !FueUsada;

        // -------------------- Emisión / Numeración --------------------

        /// <summary>
        /// Reserva el correlativo actual para emisión y avanza al siguiente.
        /// Falla si está inhabilitada o si alcanzó el máximo normativo.
        /// </summary>
        public Correlativo ReservarSiguiente()
        {
            if (!Habilitada)
                throw new BusinessRuleException("La serie está inhabilitada. No se puede emitir con esta serie.");

            var actual = Siguiente;
            // Esto lanza si ya está en Max
            Siguiente = Siguiente.Siguiente();

            if (!FueUsada)
            {
                FueUsada = true;
                AddEvent(new Events.SerieUsadaPrimeraVez(EmpresaId, Tipo, Serie));
            }

            Version++;
            AddEvent(new Events.CorrelativoReservado(EmpresaId, Tipo, Serie, actual));
            return actual;
        }
    }
}
