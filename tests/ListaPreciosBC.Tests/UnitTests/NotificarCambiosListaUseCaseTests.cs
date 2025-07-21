using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ListaPreciosBC.Application.DTOs;
using ListaPreciosBC.Application.Interfaces;
using ListaPreciosBC.Application.UseCases;
using Moq;
using NUnit.Framework;

namespace ListaPreciosBC.Tests.UnitTests
{
    [TestFixture]
    public class NotificarCambiosListaUseCaseTests
    {
        [Test]
        public async Task HandleAsync_EnvioCorrecto_NotificaUsuarios()
        {
            // Arrange
            var evento = new ListaPrecioEventoDto
            {
                ListaNombre = "Lista Comercial",
                TipoEvento = "Creada",
                Usuario = "admin",
                Fecha = DateTime.UtcNow
            };

            var usuarios = new List<string> { "comercial1@empresa.com", "comercial2@empresa.com" };

            var notificacionMock = new Mock<INotificacionService>();
            var usuarioRepoMock = new Mock<IUsuarioRepository>();
            var reintentosMock = new Mock<IReintentosService>();

            usuarioRepoMock
                .Setup(r => r.GetUsuariosConPermisoAsync("VER_PRECIOS"))
                .ReturnsAsync(usuarios);

            var useCase = new NotificarCambiosListaUseCase(
                notificacionMock.Object,
                usuarioRepoMock.Object,
                reintentosMock.Object);

            // Act
            await useCase.HandleAsync(evento);

            // Assert
            notificacionMock.Verify(n => n.EnviarCorreoAsync(usuarios, It.IsAny<MensajeNotificacionDto>()), Times.Once);
            reintentosMock.Verify(r => r.RegistrarReintentoAsync(It.IsAny<ListaPrecioEventoDto>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task HandleAsync_FallaEnvio_SeRegistraReintento()
        {
            // Arrange
            var evento = new ListaPrecioEventoDto
            {
                ListaNombre = "Lista Comercial",
                TipoEvento = "Inhabilitada",
                Usuario = "admin",
                Fecha = DateTime.UtcNow
            };

            var usuarios = new List<string> { "comercial1@empresa.com" };

            var notificacionMock = new Mock<INotificacionService>();
            var usuarioRepoMock = new Mock<IUsuarioRepository>();
            var reintentosMock = new Mock<IReintentosService>();

            usuarioRepoMock
                .Setup(r => r.GetUsuariosConPermisoAsync("VER_PRECIOS"))
                .ReturnsAsync(usuarios);

            notificacionMock
                .Setup(n => n.EnviarCorreoAsync(usuarios, It.IsAny<MensajeNotificacionDto>()))
                .ThrowsAsync(new Exception("SMTP error"));

            var useCase = new NotificarCambiosListaUseCase(
                notificacionMock.Object,
                usuarioRepoMock.Object,
                reintentosMock.Object);

            // Act
            await useCase.HandleAsync(evento);

            // Assert
            notificacionMock.Verify(n => n.EnviarCorreoAsync(usuarios, It.IsAny<MensajeNotificacionDto>()), Times.Once);
            reintentosMock.Verify(r => r.RegistrarReintentoAsync(evento, It.IsAny<string>()), Times.Once);
        }
    }
}