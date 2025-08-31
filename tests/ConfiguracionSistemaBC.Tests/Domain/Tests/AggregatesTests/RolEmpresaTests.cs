using System.Linq;
using NUnit.Framework;
using ConfiguracionSistemaBC.Domain.Aggregates;
using SharedKernel.ValueObjects;
using SharedKernel.Exceptions;
using ConfiguracionSistemaBC.Domain.ValueObjects;

namespace ConfiguracionSistemaBC.Tests.Domain.Aggregates
{
    [TestFixture]
    public class RolEmpresaTests
    {
        private static EmpresaId EID() => EmpresaId.From("20000000001");

        [Test]
        public void RolDeSistema_EsInmutable()
        {
            var sys = RolEmpresa.CatalogoSistema.Cajero();
            Assert.That(sys.EsSistema, Is.True);
            Assert.That(sys.EmpresaId, Is.Null);
            Assert.That(sys.Permisos.Any(), Is.True);

            Assert.Throws<BusinessRuleException>(() => sys.Renombrar("Caja Pro"));
            Assert.Throws<BusinessRuleException>(() =>
                sys.ReemplazarPermisos(new[] { new Permiso(Recurso.Caja, Accion.Ver) }));
        }

        [Test]
        public void ClonarParaEmpresa_CreaRolEditable_ConMismosPermisos()
        {
            var baseRol = RolEmpresa.CatalogoSistema.Vendedor();
            var clon = baseRol.ClonarParaEmpresa(EID(), "Vendedor General");

            Assert.That(clon.EsSistema, Is.False);
            Assert.That(clon.EmpresaId, Is.EqualTo(EID()));
            Assert.That(clon.Nombre, Is.EqualTo("Vendedor General"));
            Assert.That(clon.Permisos.Count, Is.EqualTo(baseRol.Permisos.Count));

            Assert.DoesNotThrow(() => clon.Renombrar("Vendedor Zonal"));
            Assert.DoesNotThrow(() => clon.ReemplazarPermisos(new[]
            {
                new Permiso(Recurso.Comprobantes, Accion.Ver | Accion.Emitir | Accion.Anular)
            }));
        }

        [Test]
        public void CrearPersonalizado_RequierePermisosYConsolidaFlags()
        {
            var rol = RolEmpresa.CrearPersonalizado(EID(), "Rol Caja Local", new[]
            {
                new Permiso(Recurso.Caja, Accion.Ver),
                new Permiso(Recurso.Caja, Accion.Crear),
                new Permiso(Recurso.Comprobantes, Accion.Ver | Accion.Emitir)
            });

            var pCaja = rol.Permisos.First(p => p.Recurso == Recurso.Caja);
            Assert.That(pCaja.Acciones.HasFlag(Accion.Ver), Is.True);
            Assert.That(pCaja.Acciones.HasFlag(Accion.Crear), Is.True);
            Assert.That(pCaja.Acciones.HasFlag(Accion.Editar), Is.False);
        }

        [Test]
        public void TienePermiso_Funciona()
        {
            var admin = RolEmpresa.CatalogoSistema.Administrador();
            Assert.That(admin.TienePermiso(Recurso.Configuracion, Accion.Configurar), Is.True);
            Assert.That(admin.TienePermiso(Recurso.Clientes, Accion.Eliminar), Is.False); // ejemplo
        }

        [Test]
        public void Eventos_DeCreacionYRenombrado_SeEmiten()
        {
            var baseRol = RolEmpresa.CatalogoSistema.Asistente();
            var clon = baseRol.ClonarParaEmpresa(EID(), "Asistente Base");
            clon.ClearDomainEvents(); // limpiar eventos previos

            clon.Renombrar("Asistente TI");
            Assert.That(clon.DomainEvents.OfType<RolEmpresaRenombrado>().Any(), Is.True);
        }

        [Test]
        public void Validaciones_Basicas()
        {
            Assert.Throws<BusinessRuleException>(() =>
                RolEmpresa.CrearPersonalizado(EID(), "   ", new[] { new Permiso(Recurso.Caja, Accion.Ver) }));

            Assert.Throws<BusinessRuleException>(() =>
                RolEmpresa.CrearPersonalizado(EID(), "Caja", Enumerable.Empty<Permiso>()));
        }
    }
}
