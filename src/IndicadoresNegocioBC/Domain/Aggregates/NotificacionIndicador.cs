using System;
using System.Collections.Generic; // necesario por List<>
using System.Linq;
using SharedKernel.ValueObjects;
using IndicadoresNegocioBC.Domain.ValueObjects;
using IndicadoresNegocioBC.Domain.Entities;
using IndicadoresNegocioBC.Domain.Events;
using SharedKernel.Events;

namespace IndicadoresNegocioBC.Domain.Aggregates
{
    /// <summary>
    /// Entidad que representa la notificación configurada para un indicador de negocio.
    /// Permite definir a qué hora, por qué medio y a quién se enviará el resumen diario de ventas.
    /// Incluye soporte para notificación por establecimiento y usuario.
    /// </summary>
    
    public class NotificacionIndicador
    {
        // Eventos de dominio generados por este agregado
        private readonly List<IDomainEvent> _domainEvents = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
        // Identidad, alcance y metadatos
        public Guid Id { get; }
        public EmpresaId EmpresaId { get; }
        public Guid IndicadorId { get; }
        public EstablecimientoId EstablecimientoId { get; }
        public UsuarioId UsuarioId { get; }

        // Contenido y campos visibles para usuario
        public string Asunto { get; private set; }

        /// <summary>
        /// Texto interno opcional, visible solo para administración o lógica interna.
        /// </summary>
        public string? TextoInterno { get; internal set; }

        // Programación y filtros
        public HorarioNotificacion HorarioEnvio { get; private set; }

        /// <summary>
        /// Fecha de inicio de vigencia de la notificación (opcional).
        /// </summary>
        public DateTimeOffset? FechaInicio { get; private set; }

        /// <summary>
        /// Fecha de fin de vigencia de la notificación (opcional).
        /// </summary>
        public DateTimeOffset? FechaFin { get; private set; }

        /// <summary>
        /// Días de la semana en que se enviará la notificación.
        /// Si es null o vacío, se asume "todos los días".
        /// </summary>
        public DayOfWeek[]? DiasSemana { get; private set; }

        // Entrega
        public MedioNotificacion Medio { get; private set; }
        public DestinatarioNotificacion Destinatario { get; private set; }

        // Ciclo de vida
        public bool Activo { get; private set; }
        public DateTimeOffset FechaCreacion { get; }
        public DateTimeOffset? FechaUltimaModificacion { get; private set; }

        // Histórico de envíos asociados a este agregado
        private readonly List<Notificacion> _notificaciones = new();
        public IReadOnlyCollection<Notificacion> Notificaciones => _notificaciones.AsReadOnly();

        public NotificacionIndicador(
            EmpresaId empresaId,
            Guid indicadorId,
            EstablecimientoId establecimientoId,
            UsuarioId usuarioId,
            string asunto,
            HorarioNotificacion horarioEnvio,
            MedioNotificacion medio,
            DestinatarioNotificacion destinatario,
            bool activo = true)
        {
            Id = Guid.NewGuid();
            EmpresaId = empresaId ?? throw new ArgumentNullException(nameof(empresaId));
            IndicadorId = indicadorId;
            EstablecimientoId = establecimientoId ?? throw new ArgumentNullException(nameof(establecimientoId));
            UsuarioId = usuarioId ?? throw new ArgumentNullException(nameof(usuarioId));

            if (string.IsNullOrWhiteSpace(asunto))
                throw new ArgumentNullException(nameof(asunto));
            Asunto = asunto.Trim();

            HorarioEnvio = horarioEnvio ?? throw new ArgumentNullException(nameof(horarioEnvio));
            Medio = medio ?? throw new ArgumentNullException(nameof(medio));
            Destinatario = destinatario ?? throw new ArgumentNullException(nameof(destinatario));

            FechaCreacion = DateTimeOffset.UtcNow;

            if (activo)
            {
                ValidarCompletitud();
                Activo = true;
            }
            else
            {
                Activo = false;
            }

            // Registrar evento de creación
            _domainEvents.Add(new IndicadoresNegocioBC.Domain.Events.NotificacionIndicadorCreada(Id, IndicadorId));
        }

        public void CambiarAsunto(string nuevoAsunto)
        {
            if (string.IsNullOrWhiteSpace(nuevoAsunto))
                throw new ArgumentNullException(nameof(nuevoAsunto));

            Asunto = nuevoAsunto.Trim();
            FechaUltimaModificacion = DateTimeOffset.UtcNow;
            _domainEvents.Add(new NotificacionIndicadorAsuntoCambiado(Id, Asunto));
        }

        public void CambiarHorario(HorarioNotificacion nuevoHorario)
        {
            HorarioEnvio = nuevoHorario ?? throw new ArgumentNullException(nameof(nuevoHorario));
            FechaUltimaModificacion = DateTimeOffset.UtcNow;
            if (Activo) ValidarCompletitud();
            _domainEvents.Add(new NotificacionIndicadorHorarioCambiado(Id, HorarioEnvio.ToString()));
        }

        /// <summary>
        /// Cambia el rango de fechas de vigencia de la notificación.
        /// </summary>
        public void CambiarRangoFechas(DateTimeOffset? fechaInicio, DateTimeOffset? fechaFin)
        {
            if (fechaInicio.HasValue && fechaFin.HasValue && fechaFin < fechaInicio)
                throw new ArgumentException("La fecha de fin no puede ser anterior a la fecha de inicio.");

            FechaInicio = fechaInicio;
            FechaFin = fechaFin;
            FechaUltimaModificacion = DateTimeOffset.UtcNow;
            _domainEvents.Add(new NotificacionIndicadorRangoFechasCambiado(Id, FechaInicio, FechaFin));
        }

        /// <summary>
        /// Cambia los días de la semana en que se enviará la notificación.
        /// null o vacío significa "todos los días".
        /// </summary>
        public void CambiarDiasSemana(DayOfWeek[]? diasSemana)
        {
            if (diasSemana is { Length: > 0 })
                diasSemana = diasSemana.Distinct().ToArray();

            DiasSemana = diasSemana;
            FechaUltimaModificacion = DateTimeOffset.UtcNow;
            _domainEvents.Add(new NotificacionIndicadorDiasSemanaCambiados(Id, DiasSemana ?? Array.Empty<DayOfWeek>()));
        }

        internal void CambiarMedio(MedioNotificacion nuevoMedio)
        {
            Medio = nuevoMedio ?? throw new ArgumentNullException(nameof(nuevoMedio));
            FechaUltimaModificacion = DateTimeOffset.UtcNow;
            if (Activo) ValidarCompletitud();
            _domainEvents.Add(new NotificacionIndicadorMedioCambiado(Id, Medio.Valor));
        }

        public void CambiarDestinatario(DestinatarioNotificacion nuevoDestinatario)
        {
            Destinatario = nuevoDestinatario ?? throw new ArgumentNullException(nameof(nuevoDestinatario));
            FechaUltimaModificacion = DateTimeOffset.UtcNow;
            if (Activo) ValidarCompletitud();
            _domainEvents.Add(new NotificacionIndicadorDestinatarioCambiado(Id, Destinatario.ToString()));
        }

        public void Activar()
        {
            // Evita activar si la vigencia ya terminó de manera clara.
            if (FechaFin.HasValue && FechaFin.Value < DateTimeOffset.UtcNow)
                throw new InvalidOperationException("No se puede activar una notificación con vigencia vencida.");

            ValidarCompletitud();
            Activo = true;
            FechaUltimaModificacion = DateTimeOffset.UtcNow;
            _domainEvents.Add(new NotificacionIndicadorActivado(Id));
        }

        public void Desactivar()
        {
            Activo = false;
            FechaUltimaModificacion = DateTimeOffset.UtcNow;
            _domainEvents.Add(new NotificacionIndicadorDesactivado(Id));
        }

        // === Reglas de dominio auxiliares ===

        /// <summary>
        /// Valida que todos los campos requeridos estén completos y sean coherentes.
        /// </summary>
        private void ValidarCompletitud()
        {
            if (string.IsNullOrWhiteSpace(Asunto))
                throw new InvalidOperationException("El asunto debe estar configurado.");

            if (HorarioEnvio == null)
                throw new InvalidOperationException("El horario de envío debe estar configurado.");

            if (Medio == null)
                throw new InvalidOperationException("El medio de notificación debe estar configurado.");

            if (Destinatario == null)
                throw new InvalidOperationException("El destinatario debe estar configurado.");

            ValidarCompatibilidadMedioDestinatario();
        }

        /// <summary>
        /// Verifica que el destinatario tenga canales válidos para el medio seleccionado.
        /// Nota: Si tu VO de MedioNotificacion expone banderas tipo "RequiereEmail/RequiereTelefono",
        /// puedes afinarlas aquí. Se deja una validación mínima segura.
        /// </summary>
        private void ValidarCompatibilidadMedioDestinatario()
        {
            var tieneEmail = Destinatario.Email != null;
            var tieneTelefono = Destinatario.Telefono != null;

            if (!tieneEmail && !tieneTelefono)
                throw new InvalidOperationException("El destinatario debe tener al menos un correo electrónico o un número de teléfono.");

            // Si tu MedioNotificacion diferencia claramente (Email/SMS/WhatsApp),
            // aquí podrías reforzar:
            // if (Medio.EsEmail && !tieneEmail) throw new InvalidOperationException("El medio Email requiere un correo electrónico.");
            // if (Medio.EsSmsLike && !tieneTelefono) throw new InvalidOperationException("El medio SMS/WhatsApp requiere un teléfono.");
        }

        /// <summary>
        /// Retorna true si el instante dado está dentro del rango de vigencia (si lo hubiera).
        /// </summary>
        public bool EsVigenteEn(DateTimeOffset t) =>
            (!FechaInicio.HasValue || t >= FechaInicio.Value) &&
            (!FechaFin.HasValue || t <= FechaFin.Value);

        /// <summary>
        /// Retorna true si el día es válido según la configuración (null/vacío = todos los días).
        /// </summary>
        public bool EsDiaValido(DayOfWeek d) =>
            DiasSemana is null || DiasSemana.Length == 0 || DiasSemana.Contains(d);

        /// <summary>
        /// Evalúa si, desde el punto de vista de reglas del agregado, corresponde enviar en ese instante.
        /// La coincidencia exacta de hora se delega al VO HorarioNotificacion/Scheduler.
        /// </summary>
        public bool DebeEnviarEn(DateTimeOffset ahoraUtc)
        {
            if (!Activo) return false;
            if (!EsVigenteEn(ahoraUtc)) return false;
            if (!EsDiaValido(ahoraUtc.DayOfWeek)) return false;
            if (!HorarioEnvio.Coincide(ahoraUtc)) return false;
            return true;
        }

        /// <summary>
        /// Agrega una notificación (histórico). Ajusta validaciones/idempotencia según tu entidad Notificacion.
        /// </summary>
        public void AgregarNotificacion(Notificacion notificacion)
        {
            if (notificacion == null) throw new ArgumentNullException(nameof(notificacion));

            // Ejemplos a considerar (descomentar/ajustar si tu entidad lo soporta):
            // if (notificacion.EmpresaId != EmpresaId || notificacion.IndicadorId != IndicadorId)
            //     throw new InvalidOperationException("La notificación no pertenece a este agregado.");

            // Idempotencia simple (si tu entidad implementa igualdad o tienes claves de envío):
            // if (_notificaciones.Any(n => n.Equals(notificacion))) return;

            _notificaciones.Add(notificacion);
            _domainEvents.Add(new NotificacionIndicadorNotificacionAgregada(Id, notificacion.Id));
        }
    }
}
