using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using IndicadoresNegocioBC.Application.UseCases.Notificaciones;
using IndicadoresNegocioBC.Application.DTOs;
using IndicadoresNegocioBC.Domain.Repositories;
using IndicadoresNegocioBC.Domain.Aggregates;
using IndicadoresNegocioBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;
using SharedKernel.Application.Interfaces;
using IndicadoresNegocioBC.Application.Interfaces;
using SharedKernel.Events;
using SharedKernel.Exceptions;

namespace IndicadoresNegocioBC.Tests.Application.UseCases.Notificaciones
{
    [TestFixture]
    public class ActivarNotificacionIndicadorUseCaseTests
    {
        private static EmpresaId EmpresaDemo() => EmpresaId.From("empresa-demo");
        private static EstablecimientoId EstablecimientoDemo() => EstablecimientoId.New();
        private static UsuarioId UsuarioDemo() => UsuarioId.New();
        private static HorarioNotificacion Horario07() => HorarioNotificacion.FromHorasMinutos(7, 0);
        private static MedioNotificacion MedioCorreo() => MedioNotificacion.Correo;
        private static DestinatarioNotificacion DestinatarioEmail() => new DestinatarioNotificacion(Email.Create("test@demo.com"), Telefono.FromTexto("999999999"));

        private NotificacionIndicador CrearAgregado(bool activo)
        {
            return new NotificacionIndicador(EmpresaDemo(), Guid.NewGuid(), EstablecimientoDemo(), UsuarioDemo(), "A", Horario07(), MedioCorreo(), DestinatarioEmail(), activo);
        }

        [Test]
        public async Task Activar_CuandoInactiva_DeberiaActivar_PublicarEvento()
        {
            var repo = new Mock<INotificacionIndicadorRepository>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            var publisher = new Mock<IEventPublisher>(MockBehavior.Strict);
            var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaDemo());
            var agregado = CrearAgregado(false);
            repo.Setup(r => r.GetByIdAsync(agregado.Id)).ReturnsAsync(agregado);
            repo.Setup(r => r.UpdateAsync(agregado)).Returns(Task.CompletedTask);
            uow.Setup(u => u.CommitAsync(default)).Returns(Task.CompletedTask);
            var publicados = new List<IDomainEvent>();
            publisher.Setup(p => p.PublishAsync(It.IsAny<IDomainEvent>(), default))
                .Callback<IDomainEvent, System.Threading.CancellationToken>((e, _) => publicados.Add(e))
                .Returns(Task.CompletedTask);
            var useCase = new ActivarNotificacionIndicadorUseCase(repo.Object, tenant.Object, publisher.Object, uow.Object);
            var input = new ActivarNotificacionIndicadorInputDto(agregado.Id);
            var output = await useCase.ExecuteAsync(input);
            Assert.That(output.FueIdempotente, Is.False);
            Assert.That(output.Activo, Is.True);
            Assert.That(publicados.Count, Is.EqualTo(1));
            Assert.That(publicados[0].GetType().Name, Is.EqualTo("NotificacionIndicadorActivado"));
        }

        [Test]
        public async Task Activar_YaActiva_DeberiaSerIdempotente_SinEventos()
        {
            var repo = new Mock<INotificacionIndicadorRepository>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            var publisher = new Mock<IEventPublisher>(MockBehavior.Strict);
            var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaDemo());
            var agregado = CrearAgregado(true);
            repo.Setup(r => r.GetByIdAsync(agregado.Id)).ReturnsAsync(agregado);
            var publicados = new List<IDomainEvent>();
            publisher.Setup(p => p.PublishAsync(It.IsAny<IDomainEvent>(), default))
                .Callback<IDomainEvent, System.Threading.CancellationToken>((e, _) => publicados.Add(e))
                .Returns(Task.CompletedTask);
            var useCase = new ActivarNotificacionIndicadorUseCase(repo.Object, tenant.Object, publisher.Object, uow.Object);
            var input = new ActivarNotificacionIndicadorInputDto(agregado.Id);
            var output = await useCase.ExecuteAsync(input);
            Assert.That(output.FueIdempotente, Is.True);
            Assert.That(publicados.Count, Is.EqualTo(0));
            repo.Verify(r => r.UpdateAsync(It.IsAny<NotificacionIndicador>()), Times.Never);
            uow.Verify(u => u.CommitAsync(default), Times.Never);
        }

        [Test]
        public void Activar_EmpresaDistinta_DeberiaLanzarNotFound()
        {
            var repo = new Mock<INotificacionIndicadorRepository>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            var publisher = new Mock<IEventPublisher>(MockBehavior.Strict);
            var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var empresaTenant = EmpresaDemo();
            tenant.SetupGet(t => t.EmpresaId).Returns(empresaTenant);
            // Agregado con empresa diferente
            var agregado = new NotificacionIndicador(EmpresaId.From("otra-empresa"), Guid.NewGuid(), EstablecimientoDemo(), UsuarioDemo(), "A", Horario07(), MedioCorreo(), DestinatarioEmail(), true);
            repo.Setup(r => r.GetByIdAsync(agregado.Id)).ReturnsAsync(agregado);
            var useCase = new ActivarNotificacionIndicadorUseCase(repo.Object, tenant.Object, publisher.Object, uow.Object);
            var input = new ActivarNotificacionIndicadorInputDto(agregado.Id);
            Assert.That(async () => await useCase.ExecuteAsync(input), Throws.TypeOf<NotFoundException>());
        }
    }
}
