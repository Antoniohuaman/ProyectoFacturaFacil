using System;
using System.Collections.Generic;
using System.Linq;
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
    public class ActualizarNotificacionIndicadorUseCaseTests
    {
        private static EmpresaId EmpresaDemo() => EmpresaId.From("empresa-demo");
        private static EmpresaId OtraEmpresa() => EmpresaId.From("otra-empresa");
        private static EstablecimientoId EstablecimientoDemo() => EstablecimientoId.New();
        private static UsuarioId UsuarioDemo() => UsuarioId.New();
        private static HorarioNotificacion Horario08() => HorarioNotificacion.FromHorasMinutos(8, 0);
        private static HorarioNotificacion Horario09() => HorarioNotificacion.FromHorasMinutos(9, 30);
        private static MedioNotificacion MedioCorreo() => MedioNotificacion.Correo;
        private static DestinatarioNotificacion DestinatarioInicial() => new DestinatarioNotificacion(Email.Create("a@demo.com"), Telefono.FromTexto("999999999"));
        private static DestinatarioNotificacion DestinatarioNuevo() => new DestinatarioNotificacion(Email.Create("nuevo@demo.com"), Telefono.FromTexto("888888888"));

        private NotificacionIndicador CrearAgregadoInicial(EmpresaId empresa)
        {
            return new NotificacionIndicador(
                empresaId: empresa,
                indicadorId: Guid.NewGuid(),
                establecimientoId: EstablecimientoDemo(),
                usuarioId: UsuarioDemo(),
                asunto: "Asunto Inicial",
                horarioEnvio: Horario08(),
                medio: MedioCorreo(),
                destinatario: DestinatarioInicial(),
                activo: true);
        }

        [Test]
        public async Task Actualizar_CambiaVariosCampos_DeberiaPublicarEventosDeCambios()
        {
            var repo = new Mock<INotificacionIndicadorRepository>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            var publisher = new Mock<IEventPublisher>(MockBehavior.Strict);
            var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var empresa = EmpresaDemo();
            tenant.SetupGet(t => t.EmpresaId).Returns(empresa);
            var agregado = CrearAgregadoInicial(empresa);
            // DomainEvents contiene 1 (Creada)
            repo.Setup(r => r.GetByIdAsync(agregado.Id)).ReturnsAsync(agregado);
            repo.Setup(r => r.UpdateAsync(agregado)).Returns(Task.CompletedTask);
            uow.Setup(u => u.CommitAsync(default)).Returns(Task.CompletedTask);
            var publicados = new List<IDomainEvent>();
            publisher.Setup(p => p.PublishAsync(It.IsAny<IDomainEvent>(), default))
                .Callback<IDomainEvent, System.Threading.CancellationToken>((e, _) => publicados.Add(e))
                .Returns(Task.CompletedTask);

            var useCase = new ActualizarNotificacionIndicadorUseCase(repo.Object, tenant.Object, publisher.Object, uow.Object);
            var input = new ActualizarNotificacionIndicadorInputDto(
                agregado.Id,
                nuevoAsunto: "Asunto Cambiado",
                nuevoHorario: Horario09(),
                nuevaFechaInicio: DateTimeOffset.UtcNow.Date,
                nuevaFechaFin: DateTimeOffset.UtcNow.Date.AddDays(10),
                nuevosDiasSemana: new[] { DayOfWeek.Monday, DayOfWeek.Friday },
                nuevoDestinatario: DestinatarioNuevo());

            var output = await useCase.ExecuteAsync(input);

            Assert.That(output.HuboCambios, Is.True);
            // Se esperan 5 eventos nuevos (Asunto, Horario, RangoFechas, DiasSemana, Destinatario)
            Assert.That(publicados.Count, Is.EqualTo(5));
            var tipos = publicados.Select(e => e.GetType().Name).ToList();
            Assert.That(tipos, Does.Contain("NotificacionIndicadorAsuntoCambiado"));
            Assert.That(tipos, Does.Contain("NotificacionIndicadorHorarioCambiado"));
            Assert.That(tipos, Does.Contain("NotificacionIndicadorRangoFechasCambiado"));
            Assert.That(tipos, Does.Contain("NotificacionIndicadorDiasSemanaCambiados"));
            Assert.That(tipos, Does.Contain("NotificacionIndicadorDestinatarioCambiado"));
            repo.Verify(r => r.UpdateAsync(agregado), Times.Once);
            uow.Verify(u => u.CommitAsync(default), Times.Once);
        }

        [Test]
        public async Task Actualizar_SinCambios_DeberiaSerIdempotente_SinEventos()
        {
            var repo = new Mock<INotificacionIndicadorRepository>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            var publisher = new Mock<IEventPublisher>(MockBehavior.Strict);
            var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var empresa = EmpresaDemo();
            tenant.SetupGet(t => t.EmpresaId).Returns(empresa);
            var agregado = CrearAgregadoInicial(empresa);
            repo.Setup(r => r.GetByIdAsync(agregado.Id)).ReturnsAsync(agregado);
            var publicados = new List<IDomainEvent>();
            publisher.Setup(p => p.PublishAsync(It.IsAny<IDomainEvent>(), default))
                .Callback<IDomainEvent, System.Threading.CancellationToken>((e, _) => publicados.Add(e))
                .Returns(Task.CompletedTask);

            var useCase = new ActualizarNotificacionIndicadorUseCase(repo.Object, tenant.Object, publisher.Object, uow.Object);
            var input = new ActualizarNotificacionIndicadorInputDto(agregado.Id); // sin campos
            var output = await useCase.ExecuteAsync(input);
            Assert.That(output.HuboCambios, Is.False);
            Assert.That(publicados.Count, Is.EqualTo(0));
            repo.Verify(r => r.UpdateAsync(It.IsAny<NotificacionIndicador>()), Times.Never);
            uow.Verify(u => u.CommitAsync(default), Times.Never);
        }

        [Test]
        public void Actualizar_MultiEmpresaDiferente_DeberiaLanzarNotFound()
        {
            var repo = new Mock<INotificacionIndicadorRepository>(MockBehavior.Strict);
            var tenant = new Mock<ITenantContext>(MockBehavior.Strict);
            var publisher = new Mock<IEventPublisher>(MockBehavior.Strict);
            var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            tenant.SetupGet(t => t.EmpresaId).Returns(EmpresaDemo());
            var agregado = CrearAgregadoInicial(OtraEmpresa());
            repo.Setup(r => r.GetByIdAsync(agregado.Id)).ReturnsAsync(agregado);
            var useCase = new ActualizarNotificacionIndicadorUseCase(repo.Object, tenant.Object, publisher.Object, uow.Object);
            var input = new ActualizarNotificacionIndicadorInputDto(agregado.Id, nuevoAsunto: "X");
            Assert.That(async () => await useCase.ExecuteAsync(input), Throws.TypeOf<NotFoundException>());
        }
    }
}
