using NUnit.Framework;
using ConfiguracionSistemaBC.Domain.ValueObjects;

namespace ConfiguracionSistemaBC.Tests.Domain.ValueObjects
{
    [TestFixture]
    public class PermisosTests
    {
        [Test]
        public void SoloLeer_CreaPermisoConSoloAccionVer()
        {
            var p = Permiso.SoloLeer(Recurso.Clientes);

            Assert.That(p.Recurso, Is.EqualTo(Recurso.Clientes));
            Assert.That(p.Acciones, Is.EqualTo(Accion.Ver));
            Assert.That(p.Contiene(Accion.Ver), Is.True);
            Assert.That(p.Contiene(Accion.Editar), Is.False);
        }

        [Test]
        public void CRUD_IncluyeVerCrearEditarEliminar_PeroNoOtrasAcciones()
        {
            var p = Permiso.CRUD(Recurso.Articulos);

            Assert.That(p.Contiene(Accion.Ver), Is.True);
            Assert.That(p.Contiene(Accion.Crear), Is.True);
            Assert.That(p.Contiene(Accion.Editar), Is.True);
            Assert.That(p.Contiene(Accion.Eliminar), Is.True);

            Assert.That(p.Contiene(Accion.Emitir), Is.False);
            Assert.That(p.Contiene(Accion.Anular), Is.False);
        }

        [Test]
        public void Permiso_Combinado_ContieneTodasLasAccionesIndicadas()
        {
            var p = new Permiso(Recurso.Comprobantes, Accion.Ver | Accion.Emitir | Accion.Anular);

            Assert.That(p.Recurso, Is.EqualTo(Recurso.Comprobantes));
            Assert.That(p.Contiene(Accion.Ver), Is.True);
            Assert.That(p.Contiene(Accion.Emitir), Is.True);
            Assert.That(p.Contiene(Accion.Anular), Is.True);
            Assert.That(p.Contiene(Accion.Editar), Is.False);
        }

        [Test]
        public void Contiene_DevuelveFalseCuandoNoTieneLaAccion()
        {
            var p = new Permiso(Recurso.Caja, Accion.Ver);

            Assert.That(p.Contiene(Accion.Editar), Is.False);
            Assert.That(p.Contiene(Accion.Aprobar), Is.False);
        }
    }
}
