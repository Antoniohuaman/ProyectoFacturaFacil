			using System;

			namespace IndicadoresNegocioBC.Domain.ValueObjects
			{
				/// <summary>
				/// Value Object para el horario de notificación (por ejemplo, 20:00).
				/// Inmutable, validado y con helpers de formato.
				/// </summary>
				public sealed class HorarioNotificacion : IEquatable<HorarioNotificacion>
				{
					public TimeSpan Hora { get; }

					public HorarioNotificacion(TimeSpan hora)
					{
						if (hora < TimeSpan.Zero || hora >= TimeSpan.FromDays(1))
							throw new ArgumentOutOfRangeException(nameof(hora), "Hora inválida. Debe estar entre 00:00 y 23:59.");
						Hora = hora;
					}

					/// <summary>
					/// Indica si el DateTimeOffset dado coincide exactamente con la hora configurada (ignora segundos).
					/// </summary>
					public bool Coincide(DateTimeOffset fecha)
					{
						return fecha.TimeOfDay.Hours == Hora.Hours && fecha.TimeOfDay.Minutes == Hora.Minutes;
					}

					public static HorarioNotificacion FromHorasMinutos(int horas, int minutos)
					{
						if (horas < 0 || horas > 23)
							throw new ArgumentOutOfRangeException(nameof(horas), "Las horas deben estar entre 0 y 23.");
						if (minutos < 0 || minutos > 59)
							throw new ArgumentOutOfRangeException(nameof(minutos), "Los minutos deben estar entre 0 y 59.");
						return new HorarioNotificacion(new TimeSpan(horas, minutos, 0));
					}

					public override string ToString() => Hora.ToString(@"hh\:mm");

					public override bool Equals(object? obj) => Equals(obj as HorarioNotificacion);

					public bool Equals(HorarioNotificacion? other) => other is not null && Hora.Equals(other.Hora);

					public override int GetHashCode() => Hora.GetHashCode();

					public static bool operator ==(HorarioNotificacion? left, HorarioNotificacion? right) =>
						left is null ? right is null : left.Equals(right);

					public static bool operator !=(HorarioNotificacion? left, HorarioNotificacion? right) => !(left == right);
				}
			}
