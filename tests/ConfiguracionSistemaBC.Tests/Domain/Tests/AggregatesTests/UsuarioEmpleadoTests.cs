using System;
using System.Linq;
using NUnit.Framework;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using ConfiguracionSistemaBC.Domain.Interfaces;
using SharedKernel.Exceptions;

namespace ConfiguracionSistemaBC.Tests.Domain
{
    [TestFixture]
    public class UsuarioEmpleadoTests
    {
        private UnicidadServiceFake? _unicidad;
        private ActividadServiceFake? _actividad;

        [SetUp]
        public void SetUp()
        {
            _unicidad = new UnicidadServiceFake(isUnique: true);
            _actividad = new ActividadServiceFake(hasActivity: false);
        }

    private static EmpresaId Emp(string v = "EMP01") => EmpresaId.Desde(v);
    private static EstablecimientoId Est(string v = "EST01") => EstablecimientoId.Desde(v);
    private static SharedKernel.ValueObjects.Email Mail(string v = "vendedor@demo.com") => SharedKernel.ValueObjects.Email.Create(v);
        private static NombrePersona Nom(string n = "Juan", string a = "Pérez") => new NombrePersona(n, a);
        private static PasswordHash Hash(string v = "hash-1") => new PasswordHash(v);

        private UsuarioEmpleado CrearDefault(RolUsuario rol = RolUsuario.Cajero, string? perfil = null)
        {
            return UsuarioEmpleado.Crear(
                empresaId: Emp(),
                establecimientos: new[] { Est() },
                email: Mail(),
                nombre: Nom(),
                rol: rol,
                passwordHash: Hash(),
                nombrePerfilPersonalizado: perfil,
                unicidad: _unicidad!
            );
        }

        [Test]
        public void Crear_debe_iniciar_inhabilitado_y_emitir_evento()
        {
            var agg = CrearDefault();

            Assert.That(agg.Estado, Is.EqualTo(EstadoUsuarioEmpleado.Inhabilitado));
            Assert.That(agg.EmpresaId.Valor, Is.EqualTo("EMP01"));
            Assert.That(agg.Establecimientos.Single().Valor, Is.EqualTo("EST01"));
            Assert.That(agg.Email.Value, Is.EqualTo("vendedor@demo.com"));
            Assert.That(agg.Rol, Is.EqualTo(RolUsuario.Cajero));

            Assert.That(agg.DomainEvents.Any(e => e is UsuarioEmpleado.UsuarioEmpleadoCreado), Is.True);
        }

        [Test]
        public void Crear_con_email_duplicado_debe_fallar()
        {
            _unicidad = new UnicidadServiceFake(isUnique: false);

            Assert.That(() =>
            {
                _ = UsuarioEmpleado.Crear(Emp(), new[] { Est() }, Mail(), Nom(), RolUsuario.Cajero, Hash(), null, _unicidad);
            }, Throws.TypeOf<BusinessRuleException>().With.Message.Contains("ya existe"));
        }

        [Test]
        public void GenerarInvitacion_debe_asignar_token_y_emitir_evento()
        {
            var agg = CrearDefault();
            agg.ClearDomainEvents();

            var ahora = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var expira = ahora.AddHours(24);

            agg.GenerarInvitacion("TOKEN123", expira, ahora);

            Assert.That(agg.TokenInvitacion, Is.EqualTo("TOKEN123"));
            Assert.That(agg.TokenExpiraElUtc, Is.EqualTo(expira));
            // El evento correcto es InvitacionUsuarioEmpleadoEnviada
            Assert.That(agg.DomainEvents.Any(e => e is UsuarioEmpleado.InvitacionUsuarioEmpleadoEnviada), Is.True);
        }

        [Test]
        public void AceptarInvitacion_valida_debe_habilitar_y_limpiar_token_y_emitir_eventos()
        {
            var agg = CrearDefault();
            agg.ClearDomainEvents();

            var ahora = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var expira = ahora.AddHours(24);

            agg.GenerarInvitacion("ABC", expira, ahora);
            agg.ClearDomainEvents();

            agg.AceptarInvitacion("ABC", ahora.AddHours(1));

            Assert.That(agg.Estado, Is.EqualTo(EstadoUsuarioEmpleado.Habilitado));
            Assert.That(agg.InvitacionAceptadaElUtc, Is.EqualTo(ahora.AddHours(1)));
            Assert.That(agg.TokenInvitacion, Is.Null);
            Assert.That(agg.TokenExpiraElUtc, Is.Null);

            Assert.That(agg.DomainEvents.Any(e => e is UsuarioEmpleado.InvitacionUsuarioEmpleadoAceptada), Is.True);
            Assert.That(agg.DomainEvents.Any(e => e is UsuarioEmpleado.SolicitarProvisionEnIdentidad), Is.True);
        }

        [Test]
        public void AceptarInvitacion_con_token_incorrecto_debe_fallar()
        {
            var agg = CrearDefault();
            var baseTime = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            agg.GenerarInvitacion("OK", baseTime.AddHours(2), baseTime);

            Assert.That(() => agg.AceptarInvitacion("NOPE", baseTime.AddMinutes(10)),
                Throws.TypeOf<BusinessRuleException>().With.Message.Contains("no válido"));
        }

        [Test]
        public void AceptarInvitacion_expirada_debe_fallar()
        {
            var agg = CrearDefault();
            var baseTime = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            agg.GenerarInvitacion("OK", baseTime.AddMinutes(30), baseTime);

            Assert.That(() => agg.AceptarInvitacion("OK", baseTime.AddHours(1)),
                Throws.TypeOf<BusinessRuleException>().With.Message.Contains("expirado"));
        }

        [Test]
        public void Habilitar_sin_aceptar_invitacion_debe_fallar()
        {
            var agg = CrearDefault();

            Assert.That(() => agg.Habilitar(),
                Throws.TypeOf<BusinessRuleException>().With.Message.Contains("aceptar la invitación"));
        }

        [Test]
        public void Inhabilitar_y_luego_habilitar_debe_emitir_eventos_correspondientes()
        {
            var agg = CrearDefault();
            var t0 = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);
            var t1 = t0.AddHours(1);

            agg.GenerarInvitacion("ABC", t0.AddHours(4), t0);
            agg.AceptarInvitacion("ABC", t0.AddHours(2)); // queda habilitado
            agg.ClearDomainEvents();

            agg.Inhabilitar("cierre de periodo");
            Assert.That(agg.Estado, Is.EqualTo(EstadoUsuarioEmpleado.Inhabilitado));
            Assert.That(agg.DomainEvents.Any(e => e is UsuarioEmpleado.UsuarioEmpleadoInhabilitado), Is.True);

            agg.ClearDomainEvents();
            // Re-habilitar exige invitación aceptada (ya la tiene)
            agg.Habilitar();
            Assert.That(agg.Estado, Is.EqualTo(EstadoUsuarioEmpleado.Habilitado));
            Assert.That(agg.DomainEvents.Any(e => e is UsuarioEmpleado.UsuarioEmpleadoHabilitado), Is.True);
        }

        [Test]
        public void ActualizarPassword_solo_habilitado()
        {
            var agg = CrearDefault();

            Assert.That(() => agg.ActualizarPassword(Hash("hash-2")),
                Throws.TypeOf<BusinessRuleException>());

            var t0 = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);
            agg.GenerarInvitacion("ABC", t0.AddHours(4), t0);
            agg.AceptarInvitacion("ABC", t0.AddHours(1));
            agg.ClearDomainEvents();

            agg.ActualizarPassword(Hash("hash-2"));
            Assert.That(agg.PasswordHash.Valor, Is.EqualTo("hash-2"));
            Assert.That(agg.DomainEvents.Any(e => e is UsuarioEmpleado.PasswordDeUsuarioEmpleadoActualizada), Is.True);
        }

        [Test]
        public void ActualizarRol_debe_emitir_evento_solo_si_cambia()
        {
            var agg = CrearDefault(rol: RolUsuario.Cajero);
            agg.ClearDomainEvents();

            agg.ActualizarRol(RolUsuario.Cajero);
            Assert.That(agg.DomainEvents.Any(e => e is UsuarioEmpleado.RolDeUsuarioEmpleadoActualizado), Is.False);

            agg.ActualizarRol(RolUsuario.Contador);
            Assert.That(agg.Rol, Is.EqualTo(RolUsuario.Contador));
            Assert.That(agg.DomainEvents.Any(e => e is UsuarioEmpleado.RolDeUsuarioEmpleadoActualizado), Is.True);
        }

        [Test]
        public void EliminarPorErrorAdministrativo_solo_inhabilitado_sin_actividad()
        {
            var agg = CrearDefault();
            var t0 = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);

            // Caso OK (inhabilitado + sin actividad)
            agg.ClearDomainEvents();
            agg.EliminarPorErrorAdministrativo("creado por error", _actividad!, t0);
            Assert.That(agg.EliminadoElUtc, Is.EqualTo(t0));
            Assert.That(agg.DomainEvents.Any(e => e is UsuarioEmpleado.UsuarioEmpleadoEliminado), Is.True);
        }

        [Test]
        public void EliminarPorErrorAdministrativo_falla_si_hay_actividad_o_si_esta_habilitado()
        {
            // Con actividad
            _actividad = new ActividadServiceFake(hasActivity: true);
            var agg1 = CrearDefault();
            Assert.That(() =>
                agg1.EliminarPorErrorAdministrativo("no procede", _actividad, DateTime.UtcNow),
                Throws.TypeOf<BusinessRuleException>());

            // Si está habilitado
            var agg2 = CrearDefault();
            var t0 = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);
            agg2.GenerarInvitacion("ABC", t0.AddHours(2), t0);
            agg2.AceptarInvitacion("ABC", t0.AddHours(1));
            Assert.That(agg2.Estado, Is.EqualTo(EstadoUsuarioEmpleado.Habilitado));

            Assert.That(() =>
                agg2.EliminarPorErrorAdministrativo("no procede", new ActividadServiceFake(false), DateTime.UtcNow),
                Throws.TypeOf<BusinessRuleException>());
        }

        // ====== Fakes mínimas para pruebas ======

        private sealed class UnicidadServiceFake : IUnicidadUsuarioEmpleadoService
        {
            private readonly bool _isUnique;
            public UnicidadServiceFake(bool isUnique) => _isUnique = isUnique;
            public bool EsEmailUnicoPorEmpresa(EmpresaId empresaId, SharedKernel.ValueObjects.Email email) => _isUnique; // No change needed
        }

        private sealed class ActividadServiceFake : IUsuarioEmpleadoActividadService
        {
            private readonly bool _hasActivity;
            public ActividadServiceFake(bool hasActivity) => _hasActivity = hasActivity;
            public bool TieneAcciones(Guid usuarioEmpleadoId) => _hasActivity;
            public bool TieneAccionesEnEstablecimiento(Guid usuarioEmpleadoId, EstablecimientoId estId) => _hasActivity;
            public bool TieneAccionesEnEstablecimientos(Guid usuarioEmpleadoId, System.Collections.Generic.IEnumerable<EstablecimientoId> estIds) => _hasActivity;
        }
    }
}
