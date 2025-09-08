using System;
using System.Linq;
using System.Reflection;
using GestionClientesBC.Domain.Entities;
using NUnit.Framework;

namespace GestionClientesBC.Tests.Domain.Entities
{
    [TestFixture]
    public class AdjuntoClienteTests
    {
        [Test]
        public void Ctor_Asigna_Todas_Las_Propiedades()
        {
            var id = Guid.NewGuid();
            var fecha = new DateTime(2025, 9, 7, 12, 34, 56, DateTimeKind.Utc);

            var adj = new AdjuntoCliente(
                adjuntoId: id,
                nombreArchivo: "ruc.pdf",
                ruta: "/files/ruc.pdf",
                fechaSubida: fecha,
                comentario: "Constancia de RUC"
            );

            Assert.That(adj.AdjuntoId, Is.EqualTo(id));
            Assert.That(adj.NombreArchivo, Is.EqualTo("ruc.pdf"));
            Assert.That(adj.Ruta, Is.EqualTo("/files/ruc.pdf"));
            Assert.That(adj.FechaSubida, Is.EqualTo(fecha));
            Assert.That(adj.Comentario, Is.EqualTo("Constancia de RUC"));
        }

        [Test]
        public void Ctor_Acepta_Comentario_Null()
        {
            var id = Guid.NewGuid();
            var fecha = DateTime.UtcNow;

            var adj = new AdjuntoCliente(
                adjuntoId: id,
                nombreArchivo: "img.png",
                ruta: "/upload/img.png",
                fechaSubida: fecha,
                comentario: null
            );

            Assert.That(adj.Comentario, Is.Null);
            Assert.That(adj.AdjuntoId, Is.EqualTo(id));
            Assert.That(adj.NombreArchivo, Is.EqualTo("img.png"));
            Assert.That(adj.Ruta, Is.EqualTo("/upload/img.png"));
            Assert.That(adj.FechaSubida, Is.EqualTo(fecha));
        }

        [Test]
        public void Ctor_Permite_EmptyGuid_Y_StringsVacios_SinValidacion()
        {
            // Refleja el comportamiento actual: no hay guards en el constructor.
            var fecha = DateTime.SpecifyKind(new DateTime(2020, 1, 1, 0, 0, 0), DateTimeKind.Utc);

            var adj = new AdjuntoCliente(
                adjuntoId: Guid.Empty,
                nombreArchivo: string.Empty,
                ruta: string.Empty,
                fechaSubida: fecha,
                comentario: string.Empty
            );

            Assert.That(adj.AdjuntoId, Is.EqualTo(Guid.Empty));
            Assert.That(adj.NombreArchivo, Is.EqualTo(string.Empty));
            Assert.That(adj.Ruta, Is.EqualTo(string.Empty));
            Assert.That(adj.Comentario, Is.EqualTo(string.Empty));
            Assert.That(adj.FechaSubida, Is.EqualTo(fecha));
        }

        [Test]
        public void Propiedades_Tienen_Setter_Privado_Inmutables_FueraDelCtor()
        {
            var t = typeof(AdjuntoCliente);
            var props = new[]
            {
                "AdjuntoId", "NombreArchivo", "Ruta", "FechaSubida", "Comentario"
            };

            foreach (var name in props)
            {
                var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
                Assert.That(p, Is.Not.Null, $"No se encontró la propiedad {name}.");

                // El setter debe existir pero ser no público (private set;)
                var setter = p!.GetSetMethod(nonPublic: true);
                Assert.That(setter, Is.Not.Null, $"La propiedad {name} debería tener setter no público.");
                Assert.That(setter!.IsPrivate, Is.True, $"La propiedad {name} debería tener setter privado.");
            }
        }
    }
}
