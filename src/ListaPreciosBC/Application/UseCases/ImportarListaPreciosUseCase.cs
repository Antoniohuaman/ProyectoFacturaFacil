using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ExcelDataReader;
using ListaPreciosBC.Application.DTOs;
using ListaPreciosBC.Domain.Aggregates;
using ListaPreciosBC.Domain.Entities;
using ListaPreciosBC.Domain.Events;
using ListaPreciosBC.Domain.ValueObjects;
using ListaPreciosBC.Application.Interfaces;

namespace ListaPreciosBC.Application.UseCases
{
    public class ImportarListaPreciosUseCase
    {
        private readonly IListaPrecioRepository _repo;
        private readonly IEventBus _eventBus;

        public ImportarListaPreciosUseCase(IListaPrecioRepository repo, IEventBus eventBus)
        {
            _repo = repo;
            _eventBus = eventBus;
        }

        public async Task<ImportarListaPreciosDto> HandleAsync(byte[] archivo, string nombreArchivo)
        {
            var resumen = new ImportarListaPreciosDto();
            var filas = LeerArchivo(archivo, nombreArchivo, out var erroresFormato);

            if (erroresFormato.Count > 0)
            {
                resumen.Errores.AddRange(erroresFormato);
                return resumen;
            }

            for (int i = 0; i < filas.Count; i++)
            {
                var fila = filas[i];
                int filaNumero = i + 2; // Asumiendo cabecera en la fila 1

                if (string.IsNullOrWhiteSpace(fila.TipoLista) ||
                    string.IsNullOrWhiteSpace(fila.CanalVenta) ||
                    string.IsNullOrWhiteSpace(fila.Moneda) ||
                    string.IsNullOrWhiteSpace(fila.Vigencia))
                {
                    resumen.Errores.Add($"Fila {filaNumero}: Faltan campos obligatorios.");
                    continue;
                }

                try
                {
                    var tipoLista = Enum.Parse<TipoLista>(fila.TipoLista, ignoreCase: true);
                    var moneda = Enum.Parse<Moneda>(fila.Moneda, ignoreCase: true);
                    var valor = decimal.Parse(fila.Valor);
                    var fechas = fila.Vigencia.Split(':');
                    var vigencia = new PeriodoVigencia(DateTime.Parse(fechas[0]), DateTime.Parse(fechas[1]));
                    var criterio = new CriterioLista(null, fila.CanalVenta, null, null);

                    var lista = await _repo.GetVigentePorCriterioAsync(tipoLista, criterio, Guid.Empty, vigencia.FechaInicio);

                    if (lista == null)
                    {
                        var listaNueva = new ListaPrecio(
                            Guid.NewGuid(),
                            tipoLista,
                            criterio,
                            moneda,
                            Prioridad.Alta,
                            vigencia,
                            DateTime.UtcNow
                        );
                        var precio = new PrecioEspecifico(
                            Guid.NewGuid(),
                            listaNueva.ListaPrecioId,
                            valor,
                            moneda,
                            Prioridad.Alta,
                            vigencia,
                            null
                        );
                        listaNueva.AgregarPrecioEspecifico(precio);
                        await _repo.AddAsync(listaNueva);
                        await _eventBus.PublishAsync(new ListaPrecioCreada(
                            listaNueva.ListaPrecioId,
                            tipoLista,
                            criterio,
                            "IMPORTACION",
                            listaNueva.FechaCreacion
                        ));
                        await _eventBus.PublishAsync(new PrecioEspecificoAgregado(
                            precio.PrecioEspecificoId,
                            listaNueva.ListaPrecioId,
                            valor,
                            criterio,
                            "IMPORTACION",
                            precio.FechaCreacion
                        ));
                        resumen.Exitos++;
                    }
                    else
                    {
                        var precio = lista.Precios.FirstOrDefault();
                        if (precio == null || precio.Valor != valor)
                        {
                            var nuevoPrecio = new PrecioEspecifico(
                                Guid.NewGuid(),
                                lista.ListaPrecioId,
                                valor,
                                moneda,
                                Prioridad.Alta,
                                vigencia,
                                null
                            );
                            lista.AgregarPrecioEspecifico(nuevoPrecio);
                            await _repo.UpdateAsync(lista);
                            await _eventBus.PublishAsync(new PrecioEspecificoAgregado(
                                nuevoPrecio.PrecioEspecificoId,
                                lista.ListaPrecioId,
                                valor,
                                criterio,
                                "IMPORTACION",
                                nuevoPrecio.FechaCreacion
                            ));
                            resumen.Exitos++;
                        }
                        else
                        {
                            resumen.Omitidos++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    resumen.Errores.Add($"Fila {filaNumero}: {ex.Message}");
                }
            }

            return resumen;
        }

        // Lee el archivo Excel y devuelve una lista de filas
        private List<FilaListaDto> LeerArchivo(byte[] archivo, string nombreArchivo, out List<string> errores)
        {
            errores = new List<string>();
            var filas = new List<FilaListaDto>();

            try
            {
                using var stream = new MemoryStream(archivo);
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                using var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(stream);

                var isFirstRow = true;
                var colMap = new Dictionary<string, int>();

                while (reader.Read())
                {
                    if (isFirstRow)
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var col = reader.GetValue(i)?.ToString()?.Trim().ToLowerInvariant();
                            if (!string.IsNullOrEmpty(col))
                                colMap[col] = i;
                        }
                        var requeridas = new[] { "tipolista", "canalventa", "valor", "moneda", "vigencia" };
                        foreach (var req in requeridas)
                        {
                            if (!colMap.ContainsKey(req))
                                errores.Add($"Falta columna obligatoria: {req}");
                        }
                        if (errores.Count > 0) return filas;
                        isFirstRow = false;
                        continue;
                    }

                    var fila = new FilaListaDto
                    {
                        TipoLista = reader.GetValue(colMap["tipolista"])?.ToString() ?? "",
                        CanalVenta = reader.GetValue(colMap["canalventa"])?.ToString() ?? "",
                        Valor = reader.GetValue(colMap["valor"])?.ToString() ?? "",
                        Moneda = reader.GetValue(colMap["moneda"])?.ToString() ?? "",
                        Vigencia = reader.GetValue(colMap["vigencia"])?.ToString() ?? ""
                    };
                    filas.Add(fila);
                }
            }
            catch (Exception ex)
            {
                errores.Add($"Error al leer archivo: {ex.Message}");
            }

            return filas;
        }

        // DTO interno para cada fila
        private class FilaListaDto
        {
            public string TipoLista { get; set; } = string.Empty;
            public string CanalVenta { get; set; } = string.Empty;
            public string Valor { get; set; } = string.Empty;
            public string Moneda { get; set; } = string.Empty;
            public string Vigencia { get; set; } = string.Empty;
        }
    }
}