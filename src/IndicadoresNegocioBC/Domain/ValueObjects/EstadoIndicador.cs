using System;
using System.Collections.Generic;

namespace IndicadoresNegocioBC.Domain.ValueObjects
{
    /// <summary>
    /// Value Object que representa el estado del indicador dentro de su ciclo de vida.
    /// Estados soportados:
    ///  - CREADO: el periodo/segmento fue iniciado pero aún no se han aplicado (o confirmado) datos.
    ///  - ACTUALIZADO: el indicador recibió/sincronizó datos; admite nuevas mutaciones.
    ///  - CONSOLIDADO: cierre del periodo/segmento; el indicador queda de solo lectura.
    ///
    /// Invariantes:
    ///  - Solo existen los 3 estados declarados (no se permiten instancias ad hoc).
    ///  - La igualdad es por valor (Codigo + Nombre).
    ///
    /// Reglas de transición:
    ///  - CREADO -> ACTUALIZADO (válida)
    ///  - CREADO -> CONSOLIDADO (válida, p.ej. periodo sin movimientos)
    ///  - ACTUALIZADO -> CONSOLIDADO (válida)
    ///  - Permanecer en el mismo estado es válido (idempotente).
    ///  - Desde CONSOLIDADO no se puede retroceder ni cambiar a otro estado.
    /// </summary>
    public sealed record EstadoIndicador
    {
        /// <summary>Código entero del estado (útil para persistencia).</summary>
        public byte Codigo { get; }

        /// <summary>Nombre normalizado del estado (mayúsculas).</summary>
        public string Nombre { get; }

        private EstadoIndicador(byte codigo, string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre es obligatorio.", nameof(nombre));

            Codigo = codigo;
            Nombre = nombre.Trim().ToUpperInvariant();
        }

        // ----------------- Instancias soportadas (tipo "smart enum") -----------------

        /// <summary>Estado inicial; aún sin datos confirmados.</summary>
        public static readonly EstadoIndicador Creado = new(1, "CREADO");

        /// <summary>Recibió y aplicó datos; aún abierto a cambios.</summary>
        public static readonly EstadoIndicador Actualizado = new(2, "ACTUALIZADO");

        /// <summary>Periodo cerrado; estado final de solo lectura.</summary>
        public static readonly EstadoIndicador Consolidado = new(3, "CONSOLIDADO");

        /// <summary>Listado inmutable de todos los estados válidos.</summary>
        public static IReadOnlyList<EstadoIndicador> Todos { get; } =
            new[] { Creado, Actualizado, Consolidado };

        /// <summary>True si este es un estado final (Consolidado).</summary>
        public bool EsFinal => ReferenceEquals(this, Consolidado);

        /// <summary>True si admite mutaciones (no consolidado).</summary>
        public bool PermiteMutaciones => !EsFinal;

        public override string ToString() => Nombre;

        // ----------------- Fábricas / Parse -----------------

        /// <summary>Obtiene una instancia a partir del código persistido.</summary>
        public static EstadoIndicador DesdeCodigo(byte codigo) => codigo switch
        {
            1 => Creado,
            2 => Actualizado,
            3 => Consolidado,
            _ => throw new ArgumentOutOfRangeException(nameof(codigo), "Código de estado inválido.")
        };

        /// <summary>Obtiene una instancia a partir de texto (case-insensitive).</summary>
        public static EstadoIndicador DesdeTexto(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                throw new ArgumentException("El texto de estado es obligatorio.", nameof(texto));

            var t = texto.Trim().ToUpperInvariant();
            return t switch
            {
                "CREADO" => Creado,
                "ACTUALIZADO" => Actualizado,
                "CONSOLIDADO" => Consolidado,
                _ => throw new ArgumentException($"Estado desconocido: '{texto}'.", nameof(texto))
            };
        }

        // ----------------- Reglas de transición -----------------

        /// <summary>
        /// Indica si pasar de <paramref name="origen"/> a <paramref name="destino"/> es permitido.
        /// </summary>
        public static bool EsTransicionValida(EstadoIndicador origen, EstadoIndicador destino)
        {
            // Idempotencia: mantenerse es válido
            if (ReferenceEquals(origen, destino)) return true;

            // Desde consolidado no se permite ningún cambio
            if (ReferenceEquals(origen, Consolidado)) return false;

            // Reglas permitidas
            if (ReferenceEquals(origen, Creado) && (ReferenceEquals(destino, Actualizado) || ReferenceEquals(destino, Consolidado)))
                return true;

            if (ReferenceEquals(origen, Actualizado) && ReferenceEquals(destino, Consolidado))
                return true;

            // Cualquier otra combinación, inválida (p.ej. retrocesos)
            return false;
        }

        /// <summary>
        /// Valida una transición y lanza excepción si no está permitida.
        /// Útil para ser usada desde el agregado antes de cambiar de estado.
        /// </summary>
        public static void AsegurarTransicionValida(EstadoIndicador origen, EstadoIndicador destino)
        {
            if (!EsTransicionValida(origen, destino))
                throw new InvalidOperationException($"Transición de '{origen}' a '{destino}' no permitida.");
        }
    }
}