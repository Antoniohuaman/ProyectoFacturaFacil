// src/GestionClientesBC/Domain/ValueObjects/DatosSunatCliente.cs
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace GestionClientesBC.Domain.ValueObjects
{
    /// <summary>
    /// Conjunto de datos informativos provenientes de SUNAT para un cliente.
    /// No impone reglas de negocio; solo normaliza texto y listas.
    /// </summary>
    public sealed class DatosSunatCliente : IEquatable<DatosSunatCliente>
    {
        public string? TipoContribuyente { get; }
        public string? EstadoContribuyente { get; }
        public string? CondicionDomicilio { get; }
        public string? SistemaEmision { get; }
        public DateTime? FechaInscripcion { get; }

        public bool? EsEmisorElectronico { get; }
        public bool? EsAgenteRetencion { get; }
        public bool? EsAgentePercepcion { get; }
        public bool? EsBuenContribuyente { get; }
        public bool? ExceptuadaPercepcion { get; }

        /// <summary>
        /// Lista de descripciones de actividades económicas relevantes para el cliente.
        /// </summary>
        public IReadOnlyCollection<string> ActividadesEconomicas { get; }

        public static DatosSunatCliente Vacio { get; } = new DatosSunatCliente(
            tipoContribuyente: null,
            estadoContribuyente: null,
            condicionDomicilio: null,
            sistemaEmision: null,
            fechaInscripcion: null,
            esEmisorElectronico: null,
            esAgenteRetencion: null,
            esAgentePercepcion: null,
            esBuenContribuyente: null,
            exceptuadaPercepcion: null,
            actividadesEconomicas: Array.Empty<string>());

        private DatosSunatCliente(
            string? tipoContribuyente,
            string? estadoContribuyente,
            string? condicionDomicilio,
            string? sistemaEmision,
            DateTime? fechaInscripcion,
            bool? esEmisorElectronico,
            bool? esAgenteRetencion,
            bool? esAgentePercepcion,
            bool? esBuenContribuyente,
            bool? exceptuadaPercepcion,
            IReadOnlyCollection<string> actividadesEconomicas)
        {
            TipoContribuyente = Normalize(tipoContribuyente);
            EstadoContribuyente = Normalize(estadoContribuyente);
            CondicionDomicilio = Normalize(condicionDomicilio);
            SistemaEmision = Normalize(sistemaEmision);
            FechaInscripcion = fechaInscripcion?.Date;

            EsEmisorElectronico = esEmisorElectronico;
            EsAgenteRetencion = esAgenteRetencion;
            EsAgentePercepcion = esAgentePercepcion;
            EsBuenContribuyente = esBuenContribuyente;
            ExceptuadaPercepcion = exceptuadaPercepcion;

            // Construir una lista de actividades sin valores nulos, normalizadas,
            // únicas (ignorando mayúsculas) y en una colección de solo lectura.
            var actividadesLista = (actividadesEconomicas ?? Array.Empty<string>())
                .Select(Normalize)
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(s => s!) // después del filtro, s no es null
                .ToList();

            ActividadesEconomicas = new ReadOnlyCollection<string>(actividadesLista);
        }

        private static string? Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var trimmed = value.Trim();
            return trimmed.Length > 120 ? trimmed[..120] : trimmed;
        }

        public static DatosSunatCliente Create(
            string? tipoContribuyente = null,
            string? estadoContribuyente = null,
            string? condicionDomicilio = null,
            string? sistemaEmision = null,
            DateTime? fechaInscripcion = null,
            bool? esEmisorElectronico = null,
            bool? esAgenteRetencion = null,
            bool? esAgentePercepcion = null,
            bool? esBuenContribuyente = null,
            bool? exceptuadaPercepcion = null,
            IEnumerable<string>? actividadesEconomicas = null)
        {
            var lista = actividadesEconomicas?.ToList()
                        ?? new List<string>();

            if (tipoContribuyente is null &&
                estadoContribuyente is null &&
                condicionDomicilio is null &&
                sistemaEmision is null &&
                fechaInscripcion is null &&
                esEmisorElectronico is null &&
                esAgenteRetencion is null &&
                esAgentePercepcion is null &&
                esBuenContribuyente is null &&
                exceptuadaPercepcion is null &&
                !lista.Any())
            {
                return Vacio;
            }

            return new DatosSunatCliente(
                tipoContribuyente,
                estadoContribuyente,
                condicionDomicilio,
                sistemaEmision,
                fechaInscripcion,
                esEmisorElectronico,
                esAgenteRetencion,
                esAgentePercepcion,
                esBuenContribuyente,
                exceptuadaPercepcion,
                lista);
        }

        public bool Equals(DatosSunatCliente? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            // Comparar actividades como conjuntos (ignorar orden), usando comparación case-insensitive
            var setThis = new HashSet<string>(ActividadesEconomicas, StringComparer.OrdinalIgnoreCase);
            var setOther = new HashSet<string>(other.ActividadesEconomicas, StringComparer.OrdinalIgnoreCase);

            bool actividadesIguales = setThis.SetEquals(setOther);

            return TipoContribuyente == other.TipoContribuyente
                && EstadoContribuyente == other.EstadoContribuyente
                && CondicionDomicilio == other.CondicionDomicilio
                && SistemaEmision == other.SistemaEmision
                && FechaInscripcion == other.FechaInscripcion
                && EsEmisorElectronico == other.EsEmisorElectronico
                && EsAgenteRetencion == other.EsAgenteRetencion
                && EsAgentePercepcion == other.EsAgentePercepcion
                && EsBuenContribuyente == other.EsBuenContribuyente
                && ExceptuadaPercepcion == other.ExceptuadaPercepcion
                && actividadesIguales;
        }

        public override bool Equals(object? obj) => Equals(obj as DatosSunatCliente);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(TipoContribuyente);
            hash.Add(EstadoContribuyente);
            hash.Add(CondicionDomicilio);
            hash.Add(SistemaEmision);
            hash.Add(FechaInscripcion);
            hash.Add(EsEmisorElectronico);
            hash.Add(EsAgenteRetencion);
            hash.Add(EsAgentePercepcion);
            hash.Add(EsBuenContribuyente);
            hash.Add(ExceptuadaPercepcion);

            foreach (var act in ActividadesEconomicas.OrderBy(a => a, StringComparer.OrdinalIgnoreCase))
                hash.Add(act);

            return hash.ToHashCode();
        }
    }
}
