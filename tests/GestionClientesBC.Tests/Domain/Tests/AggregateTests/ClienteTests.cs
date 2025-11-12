using System;
using System.Collections.Generic;
using System.Linq;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.Entities;
using GestionClientesBC.Domain.Events;
using GestionClientesBC.Domain.ValueObjects;
using Moq;
using NUnit.Framework;
using SharedKernel.Events;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionClientesBC.Tests.Domain
{
    [TestFixture]
    public class ClienteAggregateTests
    {
        // -------- Helpers de datos válidos (RUC) --------

        private static EmpresaId EmpresaDemo() => EmpresaId.From("EMPRESA-DEMO-001");

        // RUCs válidos según tu algoritmo (módulo 11)
        // Puedes cambiarlos si deseas, pero estos pasan la validación:
        private const string RucValido1 = "20661287099";
        private const string RucValido2 = "20239867198";

        private static DocumentoIdentidad Ruc(string ruc) =>
            DocumentoIdentidad.Crear(TipoDocumento.Ruc, ruc);

        private static RazonSocial RS(string texto) =>
            RazonSocial.Crear(texto);

        private static Email Correo(string v) => Email.Create(v);

        private static Telefono Celular(string v) => Telefono.FromTexto(v);

        private static DomicilioFiscal DireccionPeruOk() =>
            DomicilioFiscal.FromPeru(
                linea: "Av. Siempre Viva 742",
                ubigeo: "150101",            // Lima Cercado
                departamento: "Lima",
                provincia: "Lima",
                distrito: "Lima",
                addressTypeCode: "0000"
            );

        private static Cliente NuevoClienteRuc(
            string ruc = RucValido1,
            string razon = "ACME S.A.C.",
            string? correo = "contacto@acme.com",
            string? celular = "999 888 777",
            bool conDireccion = true
        )
        {
            return new Cliente(
                clienteId: Guid.NewGuid(),
                empresaId: EmpresaDemo(),
                documento: Ruc(ruc),
                razonSocial: RS(razon),
                nombres: null,
                correo: correo is null ? null : Correo(correo),
                telefono: celular is null ? null : Celular(celular),
                domicilioFiscal: conDireccion ? DireccionPeruOk() : null,
                tipoCliente: TipoCliente.Cliente,
                rolCliente: null,
                estado: EstadoCliente.Habilitado
            );
        }

        // ============ CONSTRUCCIÓN ============

        [Test]
        public void Ctor_Ruc_Valido_CreaCliente_Publica_ClienteCreado()
        {
            var c = NuevoClienteRuc();

            Assert.That(c.ClienteId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(c.EmpresaId.IsEmpty, Is.False);
            Assert.That(c.Documento.EsRuc, Is.True);
            Assert.That(c.RazonSocial, Is.Not.Null);
            Assert.That(c.Nombres, Is.Null);
            Assert.That(c.Estado, Is.SameAs(EstadoCliente.Habilitado));
            Assert.That(c.FechaRegistro.Kind, Is.EqualTo(DateTimeKind.Utc).Or.EqualTo(DateTimeKind.Unspecified)); // depende de cómo lo generes en tests locales

            // Evento ClienteCreado
            var evCreado = c.DomainEvents.OfType<ClienteCreado>().SingleOrDefault();
            Assert.That(evCreado, Is.Not.Null);
            Assert.That(evCreado!.ClienteId, Is.EqualTo(c.ClienteId));
            Assert.That(evCreado.EmpresaId, Is.EqualTo(c.EmpresaId));
            Assert.That(evCreado.TipoDocumento, Is.EqualTo(TipoDocumento.Ruc.ToString()));
            Assert.That(evCreado.NumeroDocumento, Is.EqualTo(RucValido1));
            Assert.That(evCreado.RazonSocial, Is.EqualTo(c.RazonSocial!.Valor));
            Assert.That(evCreado.Nombres, Is.EqualTo(string.Empty));
        }

        [Test]
        public void Ctor_Ruc_SinRazonSocial_Lanza()
        {
            Assert.That(
                () => new Cliente(
                    Guid.NewGuid(),
                    EmpresaDemo(),
                    Ruc(RucValido1),
                    razonSocial: null,
                    nombres: null
                ),
                Throws.Exception.TypeOf<ArgumentNullException>()
                     .With.Message.Contains("La razón social es obligatoria para RUC.")
            );
        }

        // ============ ESTADO (Habilitar / Deshabilitar) ============

        [Test]
        public void Habilitar_CuandoYaEstaHabilitado_Lanza()
        {
            var c = NuevoClienteRuc();
            Assert.That(() => c.Habilitar(),
                Throws.TypeOf<BusinessRuleException>()
                      .With.Message.Contains("ya está habilitado"));
        }

        [Test]
        public void Deshabilitar_EstableceEstadoYFechas_PublicaEvento()
        {
            var c = NuevoClienteRuc();
            var inicioEventos = c.DomainEvents.Count;

            var fechaLocal = new DateTime(2025, 9, 7, 10, 30, 0, DateTimeKind.Local);
            c.Deshabilitar("Prueba", fechaLocal);

            Assert.That(c.Estado, Is.SameAs(EstadoCliente.Deshabilitado));
            Assert.That(c.MotivoDeshabilitacion, Is.EqualTo("Prueba"));
            Assert.That(c.FechaDeshabilitacion.HasValue, Is.True);
            Assert.That(c.FechaUltimaModificacion.HasValue, Is.True);

            var ev = c.DomainEvents.OfType<ClienteDeshabilitado>().LastOrDefault();
            Assert.That(ev, Is.Not.Null);
            Assert.That(ev!.ClienteId, Is.EqualTo(c.ClienteId));
            Assert.That(ev.EmpresaId, Is.EqualTo(c.EmpresaId));
            Assert.That(ev.Motivo, Is.EqualTo("Prueba"));
            // La fecha debe estar en UTC en el evento/aggregate
            var expectedUtc = fechaLocal.ToUniversalTime();
            Assert.That(ev.Fecha, Is.EqualTo(expectedUtc));

            Assert.That(c.DomainEvents.Count, Is.EqualTo(inicioEventos + 1));
        }

        [Test]
        public void ReHabilitar_LuegoDeDeshabilitar_Publica_ClienteHabilitado()
        {
            var c = NuevoClienteRuc();
            c.Deshabilitar("x", DateTime.UtcNow);

            var prevCount = c.DomainEvents.Count;
            c.Habilitar();

            Assert.That(c.Estado, Is.SameAs(EstadoCliente.Habilitado));

            var ev = c.DomainEvents.OfType<ClienteHabilitado>().LastOrDefault();
            Assert.That(ev, Is.Not.Null);
            Assert.That(ev!.ClienteId, Is.EqualTo(c.ClienteId));
            Assert.That(ev.EmpresaId, Is.EqualTo(c.EmpresaId));
            Assert.That(c.DomainEvents.Count, Is.EqualTo(prevCount + 1));
        }

        // ============ ACTUALIZACIONES BÁSICAS ============

        [Test]
        public void ActualizarDireccion_ModificaVO_Y_Timestamp()
        {
            var c = NuevoClienteRuc(conDireccion: false);
            Assert.That(c.DomicilioFiscal, Is.Null);
            Assert.That(c.FechaUltimaModificacion, Is.Null);

            c.ActualizarDireccion(DireccionPeruOk());

            Assert.That(c.DomicilioFiscal, Is.Not.Null);
            Assert.That(c.FechaUltimaModificacion, Is.Not.Null);
        }

        [Test]
        public void ActualizarNombre_Ruc_Requiere_RazonSocial()
        {
            var c = NuevoClienteRuc();
            var antes = c.RazonSocial;
            c.ActualizarNombre(RS("ACME PERU S.A."));
            Assert.That(c.RazonSocial!.Valor, Is.EqualTo("ACME PERU S.A."));
            Assert.That(c.Nombres, Is.Null);
            Assert.That(c.FechaUltimaModificacion, Is.Not.Null);
            Assert.That(c.RazonSocial, Is.Not.SameAs(antes));
        }

        [Test]
        public void ActualizarTipoCliente_y_RolCliente_ModificaAmbos()
        {
            var c = NuevoClienteRuc();
            c.ActualizarTipoCliente(TipoCliente.ClienteProveedor);
            c.ActualizarRolCliente(RolCliente.Mayorista);

            Assert.That(c.TipoCliente, Is.SameAs(TipoCliente.ClienteProveedor));
            Assert.That(c.RolCliente, Is.SameAs(RolCliente.Mayorista));
            Assert.That(c.FechaUltimaModificacion, Is.Not.Null);
        }

        [Test]
        public void ActualizarDatosContacto_Set_Correo_y_Telefono()
        {
            var c = NuevoClienteRuc(correo: null, celular: null);
            c.ActualizarDatosContacto(Correo("ventas@acme.com"), "912 345 678");

            Assert.That(c.Correo!.Value, Is.EqualTo("ventas@acme.com"));
            Assert.That(c.Telefono, Is.Not.Null);
            Assert.That(c.Telefono!.Numeros.Count, Is.EqualTo(1));
            Assert.That(c.Telefono.Numeros[0].Canonico, Is.EqualTo("912345678"));
        }

        // ============ DOCUMENTO ============

        [Test]
        public void ActualizarDocumentoIdentidad_MismoValor_NoCambiaTimestamp()
        {
            var c = NuevoClienteRuc(RucValido1);
            Assert.That(c.FechaUltimaModificacion, Is.Null);

            c.ActualizarDocumentoIdentidad(Ruc(RucValido1)); // igual
            Assert.That(c.Documento.Numero, Is.EqualTo(RucValido1));
            Assert.That(c.FechaUltimaModificacion, Is.Null);
        }

        [Test]
        public void ActualizarDocumentoIdentidad_RUC_Distinto_RevalidaYActualiza()
        {
            var c = NuevoClienteRuc(RucValido1);
            c.ActualizarDocumentoIdentidad(Ruc(RucValido2));

            Assert.That(c.Documento.Numero, Is.EqualTo(RucValido2));
            Assert.That(c.FechaUltimaModificacion, Is.Not.Null);
        }

        // ============ MODIFICACIÓN (Evento) ============

        [Test]
        public void RegistrarModificacion_Publica_ClienteActualizado()
        {
            var c = NuevoClienteRuc();
            var prev = c.DomainEvents.Count;

            var cambios = new Dictionary<string, (object? anterior, object? nuevo)>
            {
                ["Telefono"] = (c.Telefono, Celular("911 222 333"))
            };

            c.RegistrarModificacion(cambios);

            Assert.That(c.DomainEvents.Count, Is.EqualTo(prev + 1));

            var ev = c.DomainEvents.OfType<ClienteActualizado>().LastOrDefault();
            Assert.That(ev, Is.Not.Null);
            Assert.That(ev!.ClienteId, Is.EqualTo(c.ClienteId));
            Assert.That(ev.EmpresaId, Is.EqualTo(c.EmpresaId));
            Assert.That(ev.TipoDocumento, Is.EqualTo(TipoDocumento.Ruc.ToString()));
            Assert.That(ev.NumeroDocumento, Is.EqualTo(c.Documento.Numero));
            Assert.That(ev.RazonSocial, Is.EqualTo(c.RazonSocial!.Valor));
            Assert.That(ev.Nombres, Is.EqualTo(string.Empty));
        }

        // ============ ADJUNTOS ============

        [Test]
        public void AgregarYEliminarAdjunto_ModificaColeccion_PublicaEventos()
        {
            var c = NuevoClienteRuc();
            var adj = new AdjuntoCliente(
                adjuntoId: Guid.NewGuid(),
                nombreArchivo: "ruc.pdf",
                ruta: "/files/ruc.pdf",
                fechaSubida: DateTime.UtcNow,
                comentario: "Constancia"
            );

            var eventosPrevios = c.DomainEvents.Count;
            c.AgregarAdjunto(adjunto: adj);

            Assert.That(c.Adjuntos.Count, Is.EqualTo(1));
            var evAdded = c.DomainEvents.OfType<AdjuntoAgregado>().LastOrDefault();
            Assert.That(evAdded, Is.Not.Null);
            Assert.That(evAdded!.ClienteId, Is.EqualTo(c.ClienteId));
            Assert.That(evAdded.EmpresaId, Is.EqualTo(c.EmpresaId));
            Assert.That(evAdded.AdjuntoId, Is.EqualTo(adj.AdjuntoId));
            Assert.That(c.DomainEvents.Count, Is.EqualTo(eventosPrevios + 1));

            // Eliminar
            var eventosPrev2 = c.DomainEvents.Count;
            c.EliminarAdjunto(adj.AdjuntoId);

            Assert.That(c.Adjuntos.Count, Is.EqualTo(0));
            var evRemoved = c.DomainEvents.OfType<AdjuntoEliminado>().LastOrDefault();
            Assert.That(evRemoved, Is.Not.Null);
            Assert.That(evRemoved!.AdjuntoId, Is.EqualTo(adj.AdjuntoId));
            Assert.That(evRemoved.ClienteId, Is.EqualTo(c.ClienteId));
            Assert.That(evRemoved.EmpresaId, Is.EqualTo(c.EmpresaId));
            Assert.That(c.DomainEvents.Count, Is.EqualTo(eventosPrev2 + 1));
        }

        // ============ ELIMINACIÓN LÓGICA (Evento) ============

        [Test]
        public void EliminarCliente_Publica_ClienteEliminado()
        {
            var c = NuevoClienteRuc();
            var prev = c.DomainEvents.Count;

            c.EliminarCliente();

            var ev = c.DomainEvents.OfType<ClienteEliminado>().LastOrDefault();
            Assert.That(ev, Is.Not.Null);
            Assert.That(ev!.ClienteId, Is.EqualTo(c.ClienteId));
            Assert.That(ev.EmpresaId, Is.EqualTo(c.EmpresaId));
            Assert.That(c.DomainEvents.Count, Is.EqualTo(prev + 1));
        }

        // ============ Ejemplo de uso con Moq (IEventBus) ============

        [Test]
        public void Publicar_DomainEvents_En_EventBus_ConMoq()
        {
            var c = NuevoClienteRuc();
            // Simular nuevas modificaciones para tener más eventos
            c.RegistrarModificacion(new Dictionary<string, (object? anterior, object? nuevo)>
            {
                ["Correo"] = (c.Correo, Correo("nuevo@acme.com"))
            });
            c.EliminarCliente();

            var eventos = c.DomainEvents.ToList(); // 1 creado + 1 actualizado + 1 eliminado = 3
            var mockBus = new Mock<IEventBus>(MockBehavior.Strict);
            foreach (var _ in eventos)
            {
                mockBus.Setup(b => b.PublishAsync(It.IsAny<IDomainEvent>(), default))
                       .Returns(System.Threading.Tasks.Task.CompletedTask)
                       .Verifiable();
            }

            // "Publicación"
            foreach (var e in eventos)
                mockBus.Object.PublishAsync(e);

            mockBus.Verify(b => b.PublishAsync(It.IsAny<IDomainEvent>(), default), Times.Exactly(eventos.Count));
        }
    }
}
