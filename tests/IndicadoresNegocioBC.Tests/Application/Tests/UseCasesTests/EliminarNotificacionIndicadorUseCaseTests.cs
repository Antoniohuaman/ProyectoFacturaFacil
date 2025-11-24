using System;
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
using SharedKernel.Exceptions;

namespace IndicadoresNegocioBC.Tests.Application.UseCases.Notificaciones
{
    [TestFixture]
    public class EliminarNotificacionIndicadorUseCaseTests
    {
        private static EmpresaId EmpresaDemo() => EmpresaId.From("empresa-demo");
        private static EstablecimientoId EstablecimientoDemo() => EstablecimientoId.New();
        private static UsuarioId UsuarioDemo() => UsuarioId.New();
        private static HorarioNotificacion Horario06() => HorarioNotificacion.FromHorasMinutos(6, 15);
        private static MedioNotificacion MedioCorreo() => MedioNotificacion.Correo;
        private static DestinatarioNotificacion DestinatarioEmail() => new DestinatarioNotificacion(Email.Create("test@demo.com"), Telefono.FromTexto("999999999"));

        private NotificacionIndicador CrearAgregado() => new NotificacionIndicador(EmpresaDemo(), Guid.NewGuid(), EstablecimientoDemo(), UsuarioDemo(), "A", Horario06(), MedioCorreo(), DestinatarioEmail(), true);

        [Test]
        public async Task Eliminar_FlujoFeliz_DeberiaEliminarYCommit_SinEventos()
        {
            var repo = new Mock<INotificacionIndicadorRepository>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaDemo());
            var agregado = CrearAgregado();
            repo.Setup(r => r.GetByIdAsync(agregado.Id)).ReturnsAsync(agregado);
            repo.Setup(r => r.DeleteAsync(agregado.Id)).Returns(Task.CompletedTask);
            uow.Setup(u => u.CommitAsync(default)).Returns(Task.CompletedTask);
            var useCase = new EliminarNotificacionIndicadorUseCase(repo.Object, tenant.Object, uow.Object);
            var input = new EliminarNotificacionIndicadorInputDto(agregado.Id);
            var output = await useCase.ExecuteAsync(input);
            Assert.That(output.Eliminado, Is.True);
            repo.Verify(r => r.DeleteAsync(agregado.Id), Times.Once);
            uow.Verify(u => u.CommitAsync(default), Times.Once);
        }

        [Test]
        public void Eliminar_NoEncontrada_DeberiaLanzarNotFound()
        {
            var repo = new Mock<INotificacionIndicadorRepository>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaDemo());
            repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((NotificacionIndicador?)null);
            var useCase = new EliminarNotificacionIndicadorUseCase(repo.Object, tenant.Object, uow.Object);
            var input = new EliminarNotificacionIndicadorInputDto(Guid.NewGuid());
            Assert.That(async () => await useCase.ExecuteAsync(input), Throws.TypeOf<NotFoundException>());
        }

        [Test]
        public void Eliminar_EmpresaDistinta_DeberiaLanzarNotFound()
        {
            var repo = new Mock<INotificacionIndicadorRepository>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaDemo());
            var agregado = new NotificacionIndicador(EmpresaId.From("otra-empresa"), Guid.NewGuid(), EstablecimientoDemo(), UsuarioDemo(), "A", Horario06(), MedioCorreo(), DestinatarioEmail(), true);
            repo.Setup(r => r.GetByIdAsync(agregado.Id)).ReturnsAsync(agregado);
            var useCase = new EliminarNotificacionIndicadorUseCase(repo.Object, tenant.Object, uow.Object);
            var input = new EliminarNotificacionIndicadorInputDto(agregado.Id);
            Assert.That(async () => await useCase.ExecuteAsync(input), Throws.TypeOf<NotFoundException>());
        }
    }
}
