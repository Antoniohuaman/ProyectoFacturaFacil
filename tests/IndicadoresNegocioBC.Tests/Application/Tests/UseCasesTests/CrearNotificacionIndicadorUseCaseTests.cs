using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using IndicadoresNegocioBC.Application.UseCases.Notificaciones;
using IndicadoresNegocioBC.Application.DTOs;
using IndicadoresNegocioBC.Domain.Repositories;
using IndicadoresNegocioBC.Domain.ValueObjects;
using IndicadoresNegocioBC.Domain.Aggregates;
using SharedKernel.Application.Interfaces;
using SharedKernel.ValueObjects;
using IndicadoresNegocioBC.Application.Interfaces;
using SharedKernel.Events;
using SharedKernel.Exceptions;

namespace IndicadoresNegocioBC.Tests.Application.UseCases.Notificaciones
{
    [TestFixture]
    public class CrearNotificacionIndicadorUseCaseTests
    {
        private static EmpresaId EmpresaDemo() => EmpresaId.From("empresa-demo");
        private static EstablecimientoId EstablecimientoDemo() => EstablecimientoId.New();
        private static UsuarioId UsuarioDemo() => UsuarioId.New();
        private static HorarioNotificacion Horario20() => HorarioNotificacion.FromHorasMinutos(20, 0);
        private static MedioNotificacion MedioCorreo() => MedioNotificacion.Correo;
        private static DestinatarioNotificacion DestinatarioEmail() => new DestinatarioNotificacion(Email.Create("test@demo.com"), null);

        [Test]
        public async Task Crear_FlujoFeliz_DeberiaPersistir_PublicarEvento_YRetornarDto()
        {
            var repo = new Mock<INotificacionIndicadorRepository>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            var publisher = new Mock<IEventPublisher>(MockBehavior.Strict);
            var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);

            var empresa = EmpresaDemo();
            tenant.SetupGet(t => t.EmpresaId).Returns(empresa);

            repo.Setup(r => r.AddAsync(It.IsAny<NotificacionIndicador>()))
                .Returns(Task.CompletedTask);
            uow.Setup(u => u.CommitAsync(default)).Returns(Task.CompletedTask);

            var publicados = new List<IDomainEvent>();
            publisher.Setup(p => p.PublishAsync(It.IsAny<IDomainEvent>(), default))
                .Callback<IDomainEvent, System.Threading.CancellationToken>((e, _) => publicados.Add(e))
                .Returns(Task.CompletedTask);

            var useCase = new CrearNotificacionIndicadorUseCase(repo.Object, tenant.Object, publisher.Object, uow.Object);
            var input = new CrearNotificacionIndicadorInputDto(Guid.NewGuid(), EstablecimientoDemo(), UsuarioDemo(), "Resumen Diario", Horario20(), MedioCorreo(), DestinatarioEmail(), true);

            var output = await useCase.ExecuteAsync(input);

            Assert.That(output, Is.Not.Null);
            Assert.That(output.EmpresaId, Is.EqualTo(empresa));
            Assert.That(output.Activo, Is.True);
            Assert.That(publicados.Count, Is.EqualTo(1)); // Solo evento de creación
            Assert.That(publicados[0].GetType().Name, Is.EqualTo("NotificacionIndicadorCreada"));
            repo.Verify(r => r.AddAsync(It.IsAny<NotificacionIndicador>()), Times.Once);
            uow.Verify(u => u.CommitAsync(default), Times.Once);
        }

        [Test]
        public async Task Crear_InactivoInicial_DeberiaCrearInactivo_PublicarEventoCreacionUnico()
        {
            var repo = new Mock<INotificacionIndicadorRepository>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            var publisher = new Mock<IEventPublisher>(MockBehavior.Strict);
            var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var empresa = EmpresaDemo();
            tenant.SetupGet(t => t.EmpresaId).Returns(empresa);
            repo.Setup(r => r.AddAsync(It.IsAny<NotificacionIndicador>())).Returns(Task.CompletedTask);
            uow.Setup(u => u.CommitAsync(default)).Returns(Task.CompletedTask);
            var publicados = new List<IDomainEvent>();
            publisher.Setup(p => p.PublishAsync(It.IsAny<IDomainEvent>(), default))
                .Callback<IDomainEvent, System.Threading.CancellationToken>((e, _) => publicados.Add(e))
                .Returns(Task.CompletedTask);

            var useCase = new CrearNotificacionIndicadorUseCase(repo.Object, tenant.Object, publisher.Object, uow.Object);
            var input = new CrearNotificacionIndicadorInputDto(Guid.NewGuid(), EstablecimientoDemo(), UsuarioDemo(), "Resumen Diario", Horario20(), MedioCorreo(), DestinatarioEmail(), false);
            var output = await useCase.ExecuteAsync(input);
            Assert.That(output.Activo, Is.False);
            Assert.That(publicados.Count, Is.EqualTo(1));
            Assert.That(publicados[0].GetType().Name, Is.EqualTo("NotificacionIndicadorCreada"));
        }
    }
}
