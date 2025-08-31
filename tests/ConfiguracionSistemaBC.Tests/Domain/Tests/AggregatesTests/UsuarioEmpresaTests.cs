using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ConfiguracionSistemaBC.Domain.Aggregates;
using SharedKernel.ValueObjects;

namespace ConfiguracionSistemaBC.Tests.Domain.Aggregates
{
    [TestFixture]
    public class UsuarioEmpresaTests
    {
        private static EmpresaId EID() => EmpresaId.From("20000000001");
        private static UsuarioId UID() => UsuarioId.New(); // asume que tu VO tiene New() / From(Guid)
        private static EstablecimientoId EST() => EstablecimientoId.New();

    private static NombrePersona NP() => NombrePersona.Crear("Ana", "Pérez");
    private static Email EM() => Email.Create("ana@acme.com");
        private static Telefono? TEL() => null;
        private static DocumentoIdentidad? DOC() => null;

        [Test]
        public void Crear_Invitado_Y_ConDatosMinimos()
        {
            var u = UsuarioEmpresa.Crear(EID(), UID(), DOC(), NP(), EM(), TEL());
            Assert.That(u.EmpresaId.Value, Is.EqualTo("20000000001"));
            Assert.That(u.Estado, Is.EqualTo(UsuarioEmpresaEstado.Invitado));
            Assert.That(u.EmailContacto.Value, Is.EqualTo("ana@acme.com"));
        }

        [Test]
        public void ActualizarDocumento_PuedeSerNull()
        {
            var u = UsuarioEmpresa.Crear(EID(), UID(), DOC(), NP(), EM(), TEL());
            u.ActualizarDocumento(null); // sigue siendo válido
            Assert.Pass();
        }

        [Test]
        public void RolesEmpresa_Agregar_Quitar_Reemplazar()
        {
            var u = UsuarioEmpresa.Crear(EID(), UID(), DOC(), NP(), EM(), TEL());
            var rolAdmin = Guid.NewGuid();
            var rolAuditor = Guid.NewGuid();

            u.AgregarRolEmpresa(rolAdmin);
            Assert.That(u.RolesEmpresaIds.Contains(rolAdmin), Is.True);

            u.AgregarRolEmpresa(rolAuditor);
            Assert.That(u.RolesEmpresaIds.Contains(rolAuditor), Is.True);

            u.QuitarRolEmpresa(rolAdmin);
            Assert.That(u.RolesEmpresaIds.Contains(rolAdmin), Is.False);

            // Reemplazo total
            u.AsignarRolesEmpresa(new[] { rolAdmin });
            Assert.That(new HashSet<Guid>(u.RolesEmpresaIds).SetEquals(new[] { rolAdmin }), Is.True);
        }

        [Test]
        public void Accesos_PorEstablecimiento_Agregar_Quitar_Roles()
        {
            var u = UsuarioEmpresa.Crear(EID(), UID(), DOC(), NP(), EM(), TEL());
            var est = EST();
            var cajero = Guid.NewGuid();
            var vendedor = Guid.NewGuid();

            // Reemplazar / set inicial
            u.AsignarRolesAlEstablecimiento(est, new[] { cajero });
            Assert.That(u.Accesos.Single().RolIds.Contains(cajero), Is.True);

            // Agregar sin reemplazar
            u.AgregarRolesAlEstablecimiento(est, new[] { vendedor });
            var roles = u.Accesos.Single().RolIds;
            Assert.That(roles.Contains(cajero) && roles.Contains(vendedor), Is.True);

            // Quitar puntual
            u.QuitarRolDeEstablecimiento(est, cajero);
            Assert.That(u.Accesos.Single().RolIds.Contains(cajero), Is.False);
            Assert.That(u.Accesos.Single().RolIds.Contains(vendedor), Is.True);
        }

        [Test]
        public void AsignarRolATodosLosEstablecimientos_AplicaCorrectamente()
        {
            var u = UsuarioEmpresa.Crear(EID(), UID(), DOC(), NP(), EM(), TEL());
            var e1 = EST();
            var e2 = EST();
            var rol = Guid.NewGuid();

            u.AsignarRolATodosLosEstablecimientos(new[] { e1, e2 }, rol, reemplazar: false);

            var r1 = u.ObtenerRolesEfectivos(e1);
            var r2 = u.ObtenerRolesEfectivos(e2);
            Assert.That(r1.Contains(rol) && r2.Contains(rol), Is.True);
        }

        [Test]
        public void ReemplazarAccesos_NoPermiteDuplicadosPorEstablecimiento()
        {
            var u = UsuarioEmpresa.Crear(EID(), UID(), DOC(), NP(), EM(), TEL());
            var e1 = EST();
            var rol = Guid.NewGuid();

            Assert.Throws<SharedKernel.Exceptions.BusinessRuleException>(() =>
                u.ReemplazarAccesos(new[]
                {
                    (e1, (IEnumerable<Guid>)new[] { rol }),
                    (e1, (IEnumerable<Guid>)new[] { rol })
                }));
        }

        [Test]
        public void Estados_Habilitar_Inhabilitar()
        {
            var u = UsuarioEmpresa.Crear(EID(), UID(), DOC(), NP(), EM(), TEL());
            u.MarcarConfirmadoPorIdentidad();
            Assert.That(u.Estado, Is.EqualTo(UsuarioEmpresaEstado.Habilitado));

            u.Inhabilitar("Baja");
            Assert.That(u.Estado, Is.EqualTo(UsuarioEmpresaEstado.Inhabilitado));
        }

        [Test]
        public void RolesEfectivos_Union_Global_Mas_Local()
        {
            var u = UsuarioEmpresa.Crear(EID(), UID(), DOC(), NP(), EM(), TEL());
            var e1 = EST();
            var rolGlobal = Guid.NewGuid();
            var rolLocal = Guid.NewGuid();

            u.AgregarRolEmpresa(rolGlobal);
            u.AsignarRolesAlEstablecimiento(e1, new[] { rolLocal });

            var roles = u.ObtenerRolesEfectivos(e1);
            Assert.That(roles.Contains(rolGlobal) && roles.Contains(rolLocal), Is.True);
        }
    }
}
