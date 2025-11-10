using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.Events;
using SharedKernel.ValueObjects;

namespace ConfiguracionSistemaBC.Tests.Domain.Aggregates
{
    [TestFixture]
    public class UsuarioEmpresa_EventsTests
    {
        private static EmpresaId EID() => EmpresaId.From("20000000001");
        private static UsuarioId UID() => UsuarioId.New();
        private static EstablecimientoId EST() => EstablecimientoId.New();
        private static NombrePersona NP() => NombrePersona.Crear("Ana", "Pérez");
        private static Email EM() => Email.Create("ana@acme.com");
        private static Telefono? TEL() => null;
        private static DocumentoIdentidad? DOC() => null;

        [Test]
        public void Emite_evento_al_crear_usuario_empresa()
        {
            var u = UsuarioEmpresa.Crear(EID(), UID(), DOC(), NP(), EM(), TEL());

            var ev = u.DomainEvents.OfType<UsuarioEmpresaCreado>().SingleOrDefault();
            Assert.That(ev, Is.Not.Null);
            Assert.That(ev!.EmpresaId.Value, Is.EqualTo(EID().Value));
            Assert.That(ev.UsuarioId.Value, Is.EqualTo(u.UsuarioId.Value));
            Assert.That(ev.Email.Value, Is.EqualTo("ana@acme.com"));
        }

        [Test]
        public void Emite_evento_al_habilitar_usuario_empresa()
        {
            var u = UsuarioEmpresa.Crear(EID(), UID(), DOC(), NP(), EM(), TEL());
            u.ClearDomainEvents(); // limpiamos evento de creación para aislar

            u.MarcarConfirmadoPorIdentidad();

            var ev = u.DomainEvents.OfType<UsuarioEmpresaHabilitado>().SingleOrDefault();
            Assert.That(ev, Is.Not.Null);
            Assert.That(ev!.EmpresaId.Value, Is.EqualTo(u.EmpresaId.Value));
            // Establecimientos pueden estar vacíos si aún no hay accesos
            Assert.That(ev.Establecimientos, Is.Not.Null);
        }

        [Test]
        public void Emite_evento_al_inhabilitar_usuario_empresa()
        {
            var u = UsuarioEmpresa.Crear(EID(), UID(), DOC(), NP(), EM(), TEL());
            u.ClearDomainEvents();

            u.Inhabilitar("Baja");

            var ev = u.DomainEvents.OfType<UsuarioEmpresaInhabilitado>().SingleOrDefault();
            Assert.That(ev, Is.Not.Null);
            Assert.That(ev!.EmpresaId.Value, Is.EqualTo(u.EmpresaId.Value));
            Assert.That(ev.UsuarioId.Value, Is.EqualTo(u.UsuarioId.Value));
            Assert.That(ev.Razon, Is.EqualTo("Baja"));
        }

        [Test]
        public void Emite_evento_al_actualizar_roles_y_accesos()
        {
            var u = UsuarioEmpresa.Crear(EID(), UID(), DOC(), NP(), EM(), TEL());
            u.ClearDomainEvents();

            // 1) Asignar roles de empresa
            var rol1 = Guid.NewGuid();
            var rol2 = Guid.NewGuid();
            u.AsignarRolesEmpresa(new[] { rol1, rol2 });

            var evRoles = u.DomainEvents.OfType<RolesEmpresaAsignados>().SingleOrDefault();
            Assert.That(evRoles, Is.Not.Null);
            Assert.That(new HashSet<Guid>(evRoles!.RolesEmpresaIds).IsSupersetOf(new[] { rol1, rol2 }), Is.True);

            // 2) Actualizar accesos por establecimiento
            u.ClearDomainEvents();
            var est = EST();
            u.AsignarRolesAlEstablecimiento(est, new[] { rol1 });

            var evAccesos = u.DomainEvents.OfType<AccesosDeUsuarioEmpresaActualizados>().SingleOrDefault();
            Assert.That(evAccesos, Is.Not.Null);
            Assert.That(evAccesos!.Accesos.Any(a => a.EstablecimientoId == est), Is.True);
            Assert.That(evAccesos.Accesos.First(a => a.EstablecimientoId == est).RolIds.Contains(rol1), Is.True);
        }
    }
}
