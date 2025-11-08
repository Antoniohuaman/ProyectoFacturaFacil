#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using ComprobantesElectronicosBC.Application.DTOs;
using ComprobantesElectronicosBC.Application.UseCases;
using ComprobantesElectronicosBC.Application.Interfaces;
using SharedKernel.Events;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;
using ComprobantesElectronicosBC.Domain.Aggregates;
using ComprobantesElectronicosBC.Domain.ValueObjects;


namespace ComprobantesElectronicosBC.Application.Tests.UseCases
{
    [TestFixture]
    public class EmitirComprobanteUseCaseTests
    {
        private Mock<INumeracionService> _numeracion = null!;
        private Mock<IComprobanteEmitidoPersister> _persister = null!;
        private Mock<IEventBus> _eventBus = null!;
    private Guid _establecimientoGuid;
    private string _empresaId = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _numeracion = new Mock<INumeracionService>(MockBehavior.Strict);
            _persister = new Mock<IComprobanteEmitidoPersister>(MockBehavior.Strict);
            _eventBus = new Mock<IEventBus>(MockBehavior.Strict);

            _establecimientoGuid = Guid.Parse("11111111-2222-3333-4444-555555555555");
            _empresaId = "20600893409";
        }

        // --------------------------- helpers ---------------------------

        private EmitirComprobanteInputDto BuildInput(bool incluirGratuita = false)
        {
            var items = new List<EmitirComprobanteInputDto.ItemDto>
            {
                // Gravado 18%: 2 x 100 = 200 (impuesto 36)
                new()
                {
                    Sku = "A-001",
                    Descripcion = "Producto A",
                    UnidadMedidaCodigo = "NIU",
                    Cantidad = 2m,
                    PrecioUnitario = 100m,
                    AfectacionCodigo = "10"
                },
                // Exonerado: 1 x 50 = 50 (impuesto 0)
                new()
                {
                    Sku = "B-002",
                    Descripcion = "Servicio B",
                    UnidadMedidaCodigo = "ZZ",
                    Cantidad = 1m,
                    PrecioUnitario = 50m,
                    AfectacionCodigo = "20"
                }
            };

            if (incluirGratuita)
            {
                // Gratuita 21 no suma base/impuesto
                items.Add(new()
                {
                    Sku = "C-003",
                    Descripcion = "Promoción C",
                    UnidadMedidaCodigo = "NIU",
                    Cantidad = 1m,
                    PrecioUnitario = 999m,
                    AfectacionCodigo = "21"
                });
            }

            return new EmitirComprobanteInputDto
            {
                EmpresaId = _empresaId,
                EstablecimientoId = _establecimientoGuid.ToString(),
                TipoComprobante = "FACTURA",
                SeriePreferida = "F001",
                FechaEmision = new DateOnly(2025, 1, 15),
                MonedaCodigo = "PEN",
                TasaImpuestoPorcentaje = 18m,
                Observaciones = "Obs",
                Cliente = new EmitirComprobanteInputDto.ClienteDto
                {
                    TipoDocumento = TipoDocumento.Ruc,
                    NumeroDocumento = "20600893409",
                    RazonSocial = "ACME S.A.C.",
                    PaisCodigoIso = "PE",
                    DomicilioLinea = "Av. Principal 123",
                    Ubigeo = "150101",
                    Departamento = "LIMA",
                    Provincia = "LIMA",
                    Distrito = "LIMA",
                    AddressTypeCode = "0000",
                    Emails = "facturacion@acme.com; contabilidad@acme.com",
                    Telefonos = "999 888 777 / (01) 234-5678"
                },
                Items = items
            };
        }

        private static EmitirComprobanteUseCase CreateSut(
            Mock<INumeracionService> numeracion,
            Mock<IComprobanteEmitidoPersister> persister,
            Mock<IEventBus> eventBus)
            => new(numeracion.Object, persister.Object, eventBus.Object);

        // --------------------------- tests ---------------------------

        [Test]
        public async Task Flujo_feliz_calcula_totales_persiste_y_publica_evento()
        {
            // Arrange
            var input = BuildInput();

            var sn = new SerieNumeroDto { Serie = "F001", Numero = 123 };
            _numeracion
                .Setup(s => s.ReservarSiguienteAsync(
                    It.IsAny<EmpresaId>(),
                    It.IsAny<EstablecimientoId>(),
                    "FACTURA",
                    "F001",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(sn);

            var nuevoId = Guid.NewGuid();
            EmitirComprobanteUseCase.ComprobanteParaEmitir? capturado = null;

            _persister
                .Setup(r => r.GuardarEmitidoAsync(
                    It.IsAny<EmitirComprobanteUseCase.ComprobanteParaEmitir>(),
                    It.IsAny<CancellationToken>()))
                .Callback<EmitirComprobanteUseCase.ComprobanteParaEmitir, CancellationToken>((d, _) => capturado = d)
                .ReturnsAsync(new ComprobantePersistido(nuevoId, 1));

            IDomainEvent? publicado = null;
            _eventBus
                .Setup(b => b.PublishAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
                .Callback<IDomainEvent, CancellationToken>((e, _) => publicado = e)
                .Returns(Task.CompletedTask);

            var sut = CreateSut(_numeracion, _persister, _eventBus);

            // Act
            var dto = await sut.HandleAsync(input);

            // Assert
            Assert.Multiple(() =>
            {
                // Totales esperados: baseG=200, baseNG=50, imp=36, valorVenta=250, total=286
                Assert.That(dto.Moneda, Is.EqualTo("PEN"));
                Assert.That(dto.ImporteBaseGravada, Is.EqualTo(200m));
                Assert.That(dto.ImporteBaseNoGravada, Is.EqualTo(50m));
                Assert.That(dto.ImporteImpuesto, Is.EqualTo(36m));
                Assert.That(dto.TotalValorVenta, Is.EqualTo(250m));
                Assert.That(dto.ImporteTotal, Is.EqualTo(286m));

                // Numeración y persistencia
                Assert.That(dto.Serie, Is.EqualTo("F001"));
                Assert.That(dto.Numero, Is.EqualTo(123));
                Assert.That(dto.ComprobanteId, Is.EqualTo(nuevoId));

                // Se persistió con coherencia
                Assert.That(capturado, Is.Not.Null);
                Assert.That(capturado!.moneda.Codigo, Is.EqualTo("PEN"));
                Assert.That(capturado.baseGravada.Monto, Is.EqualTo(200m));
                Assert.That(capturado.baseNoGravada.Monto, Is.EqualTo(50m));
                Assert.That(capturado.impuesto.Monto, Is.EqualTo(36m));
                Assert.That(capturado.total.Monto, Is.EqualTo(286m));
                Assert.That(capturado.receptorEtiqueta, Does.Contain("ACME S.A.C."));

                // Evento publicado (unificado: Enviado)
                Assert.That(publicado, Is.Not.Null);
                Assert.That(publicado, Is.TypeOf<ComprobantesElectronicosBC.Domain.Events.ComprobanteEnviadoDomainEvent>());
                var ev = (ComprobantesElectronicosBC.Domain.Events.ComprobanteEnviadoDomainEvent)publicado!;
                Assert.That(ev.ComprobanteId, Is.EqualTo(nuevoId));
            });

            _numeracion.VerifyAll();
            _persister.Verify(r => r.GuardarEmitidoAsync(It.IsAny<EmitirComprobanteUseCase.ComprobanteParaEmitir>(), It.IsAny<CancellationToken>()), Times.Once);
            _eventBus.Verify(b => b.PublishAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Linea_gratuita_21_no_altera_bases_ni_impuesto()
        {
            var input = BuildInput(incluirGratuita: true);

            _numeracion.Setup(s => s.ReservarSiguienteAsync(
                    It.IsAny<EmpresaId>(),
                    It.IsAny<EstablecimientoId>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SerieNumeroDto { Serie = "F001", Numero = 1 });

            _persister.Setup(r => r.GuardarEmitidoAsync(
                    It.IsAny<EmitirComprobanteUseCase.ComprobanteParaEmitir>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ComprobantePersistido(Guid.NewGuid(), 1));

            _eventBus.Setup(b => b.PublishAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var sut = CreateSut(_numeracion, _persister, _eventBus);

            var dto = await sut.HandleAsync(input);

            Assert.Multiple(() =>
            {
                Assert.That(dto.ImporteBaseGravada, Is.EqualTo(200m));
                Assert.That(dto.ImporteBaseNoGravada, Is.EqualTo(50m));
                Assert.That(dto.ImporteImpuesto, Is.EqualTo(36m));
                Assert.That(dto.ImporteTotal, Is.EqualTo(286m));
            });
        }

        [Test]
        public void Sin_items_lanza_BusinessRuleException()
        {
            var input = BuildInput();
            input.Items.Clear();

            var sut = CreateSut(_numeracion, _persister, _eventBus);

            Assert.That(async () => await sut.HandleAsync(input),
                Throws.TypeOf<BusinessRuleException>());
        }

        [Test]
        public void Numeracion_null_lanza_NotFoundException()
        {
            var input = BuildInput();

            _numeracion.Setup(s => s.ReservarSiguienteAsync(
                    It.IsAny<EmpresaId>(),
                    It.IsAny<EstablecimientoId>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((SerieNumeroDto?)null);

            var sut = CreateSut(_numeracion, _persister, _eventBus);

            Assert.That(async () => await sut.HandleAsync(input),
                Throws.TypeOf<NotFoundException>());
        }

        [Test]
        public void Repo_concurrency_exception_se_propaga()
        {
            var input = BuildInput();

            _numeracion.Setup(s => s.ReservarSiguienteAsync(
                    It.IsAny<EmpresaId>(),
                    It.IsAny<EstablecimientoId>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SerieNumeroDto { Serie = "F001", Numero = 7 });

            _persister.Setup(r => r.GuardarEmitidoAsync(
                    It.IsAny<EmitirComprobanteUseCase.ComprobanteParaEmitir>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ConcurrencyException("Comprobante", "id", 1, 2));

            var sut = CreateSut(_numeracion, _persister, _eventBus);

            Assert.That(async () => await sut.HandleAsync(input),
                Throws.TypeOf<ConcurrencyException>());
        }

        [Test]
        public void Tipo_no_soportado_lanza_BusinessRuleException()
        {
            var input = BuildInput();
            input = new EmitirComprobanteInputDto {
                EmpresaId = input.EmpresaId,
                EstablecimientoId = input.EstablecimientoId,
                TipoComprobante = "TICKET",
                SeriePreferida = input.SeriePreferida,
                FechaEmision = input.FechaEmision,
                MonedaCodigo = input.MonedaCodigo,
                TasaImpuestoPorcentaje = input.TasaImpuestoPorcentaje,
                Observaciones = input.Observaciones,
                Cliente = input.Cliente,
                Items = input.Items
            };

            var sut = CreateSut(_numeracion, _persister, _eventBus);

            Assert.That(async () => await sut.HandleAsync(input),
                Throws.TypeOf<BusinessRuleException>());
        }

        [Test]
        public async Task Cliente_persona_natural_etiqueta_con_nombre_completo()
        {
            var baseInput = BuildInput();
            var input = new EmitirComprobanteInputDto {
                EmpresaId = baseInput.EmpresaId,
                EstablecimientoId = baseInput.EstablecimientoId,
                TipoComprobante = baseInput.TipoComprobante,
                SeriePreferida = baseInput.SeriePreferida,
                FechaEmision = baseInput.FechaEmision,
                MonedaCodigo = baseInput.MonedaCodigo,
                TasaImpuestoPorcentaje = baseInput.TasaImpuestoPorcentaje,
                Observaciones = baseInput.Observaciones,
                Items = baseInput.Items,
                Cliente = new EmitirComprobanteInputDto.ClienteDto {
                    TipoDocumento = TipoDocumento.Dni,
                    NumeroDocumento = "12345678",
                    Nombres = "Juan Carlos",
                    Apellidos = "Pérez López",
                    PaisCodigoIso = "PE",
                    DomicilioLinea = "Calle 1",
                    Ubigeo = "150101",
                    Departamento = "LIMA",
                    Provincia = "LIMA",
                    Distrito = "LIMA",
                    AddressTypeCode = "0000",
                    Emails = "juan@correo.com",
                    Telefonos = "999999999"
                }
            };

            _numeracion.Setup(s => s.ReservarSiguienteAsync(
                    It.IsAny<EmpresaId>(),
                    It.IsAny<EstablecimientoId>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SerieNumeroDto { Serie = "F001", Numero = 10 });

            _persister.Setup(r => r.GuardarEmitidoAsync(
                    It.IsAny<EmitirComprobanteUseCase.ComprobanteParaEmitir>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ComprobantePersistido(Guid.NewGuid(), 1));

            _eventBus.Setup(b => b.PublishAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var sut = CreateSut(_numeracion, _persister, _eventBus);
            var dto = await sut.HandleAsync(input);

            Assert.That(dto.ClienteResumen, Does.Contain("DNI 12345678 - Juan Carlos Pérez López"));
        }
    }
}
