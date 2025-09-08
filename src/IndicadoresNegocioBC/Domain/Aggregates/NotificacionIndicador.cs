
using System;
using SharedKernel.ValueObjects;
using IndicadoresNegocioBC.Domain.ValueObjects;
using IndicadoresNegocioBC.Domain.Entities;

namespace IndicadoresNegocioBC.Domain.Aggregates
{
	/// <summary>
	/// Entidad que representa la notificación configurada para un indicador de negocio.
	/// Permite definir a qué hora, por qué medio y a quién se enviará el resumen diario de ventas.
	/// Incluye soporte para notificación por establecimiento y usuario.
	/// </summary>
	public class NotificacionIndicador
	{
	public Guid Id { get; }
	public EmpresaId EmpresaId { get; }
	public Guid IndicadorId { get; }
	public EstablecimientoId EstablecimientoId { get; }
	public UsuarioId UsuarioId { get; }
	public string Asunto { get; private set; }
	/// <summary>
	/// Texto interno opcional, visible solo para administración o lógica interna.
	/// </summary>
	public string? TextoInterno { get; internal set; }
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
	/// Días de la semana en que se enviará la notificación (opcional, ej: lunes a viernes).
	/// </summary>
	public DayOfWeek[]? DiasSemana { get; private set; }
	public MedioNotificacion Medio { get; private set; }
	public DestinatarioNotificacion Destinatario { get; private set; }
	public bool Activo { get; private set; }
	public DateTimeOffset FechaCreacion { get; }
	public DateTimeOffset? FechaUltimaModificacion { get; private set; }

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
			Asunto = string.IsNullOrWhiteSpace(asunto) ? throw new ArgumentNullException(nameof(asunto)) : asunto;
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
		}
		public void CambiarAsunto(string nuevoAsunto)
		{
			if (string.IsNullOrWhiteSpace(nuevoAsunto))
				throw new ArgumentNullException(nameof(nuevoAsunto));
			Asunto = nuevoAsunto;
			FechaUltimaModificacion = DateTimeOffset.UtcNow;
		}

		public void CambiarHorario(HorarioNotificacion nuevoHorario)
		{
			HorarioEnvio = nuevoHorario ?? throw new ArgumentNullException(nameof(nuevoHorario));
			FechaUltimaModificacion = DateTimeOffset.UtcNow;
			if (Activo) ValidarCompletitud();
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
		}

		// Colección de notificaciones asociadas a este indicador
		private readonly List<Notificacion> _notificaciones = new();

		// Exponer notificaciones como solo lectura
		public IReadOnlyCollection<Notificacion> Notificaciones => _notificaciones.AsReadOnly();

		// Método para agregar una notificación
		public void AgregarNotificacion(Notificacion notificacion)
		{
			if (notificacion == null) throw new ArgumentNullException(nameof(notificacion));
			// Aquí puedes agregar validaciones de dominio si aplica
			_notificaciones.Add(notificacion);
		}
		
		/// <summary>
		/// Cambia los días de la semana en que se enviará la notificación.
		/// </summary>
		public void CambiarDiasSemana(DayOfWeek[]? diasSemana)
		{
			DiasSemana = diasSemana;
			FechaUltimaModificacion = DateTimeOffset.UtcNow;
		}

		internal void CambiarMedio(MedioNotificacion nuevoMedio)
		{
			Medio = nuevoMedio ?? throw new ArgumentNullException(nameof(nuevoMedio));
			FechaUltimaModificacion = DateTimeOffset.UtcNow;
			if (Activo) ValidarCompletitud();
		}

		internal void CambiarDestinatario(DestinatarioNotificacion nuevoDestinatario)
		{
			Destinatario = nuevoDestinatario ?? throw new ArgumentNullException(nameof(nuevoDestinatario));
			FechaUltimaModificacion = DateTimeOffset.UtcNow;
			if (Activo) ValidarCompletitud();
		}

		public void Activar()
		{
			ValidarCompletitud();
			Activo = true;
			FechaUltimaModificacion = DateTimeOffset.UtcNow;
		}

		public void Desactivar()
		{
			Activo = false;
			FechaUltimaModificacion = DateTimeOffset.UtcNow;
		}

		/// <summary>
		/// Valida que todos los campos requeridos estén completos y válidos.
		/// Lanza excepción si falta alguno.
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

			// Validar que el destinatario tenga al menos un email o teléfono
			var tieneEmail = Destinatario.Email != null;
			var tieneTelefono = Destinatario.Telefono != null;
			if (!tieneEmail && !tieneTelefono)
				throw new InvalidOperationException("El destinatario debe tener al menos un correo electrónico o un número de teléfono.");
		}
	}
}
