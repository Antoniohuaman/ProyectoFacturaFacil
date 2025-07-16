using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GestionClientesBC.Application.DTOs;
using GestionClientesBC.Application.Interfaces;
using GestionClientesBC.Domain.Entities;
using GestionClientesBC.Domain.ValueObjects;
using ExcelDataReader;
using GestionClientesBC.Domain.Aggregates;



namespace GestionClientesBC.Application.UseCases
{
    public class ImportarClientesUseCase
    {
        private readonly IGestionClientesRepository _repo;
        // Puedes inyectar un publicador de eventos si lo tienes

        public ImportarClientesUseCase(IGestionClientesRepository repo)
        {
            _repo = repo;
        }

        public async Task<ImportarClientesResultadoDto> HandleAsync(ImportarClientesDto dto)
        {
            var resultado = new ImportarClientesResultadoDto();
            var filas = LeerArchivo(dto.Archivo, dto.NombreArchivo, out var erroresFormato);

            if (erroresFormato.Count > 0)
            {
                resultado.Errores.AddRange(erroresFormato);
                return resultado;
            }

            resultado.TotalFilas = filas.Count;

            // Para manejar duplicados dentro del mismo archivo
            var documentosProcesados = new HashSet<string>();

            for (int i = 0; i < filas.Count; i++)
            {
                var fila = filas[i];
                int filaNumero = i + 2; // Asumiendo cabecera en la fila 1

                // Validación básica de campos obligatorios
                if (string.IsNullOrWhiteSpace(fila.NumeroDocumento) || string.IsNullOrWhiteSpace(fila.TipoDocumento) || string.IsNullOrWhiteSpace(fila.Nombre))
                {
                    resultado.Errores.Add(new ImportarClientesErrorDto { Fila = filaNumero, Mensaje = "Faltan campos obligatorios." });
                    continue;
                }

                var tipoDoc = Enum.Parse<TipoDocumento>(fila.TipoDocumento, ignoreCase: true);
                var docId = new DocumentoIdentidad(tipoDoc, fila.NumeroDocumento);

                // Duplicado en el mismo archivo
                if (!documentosProcesados.Add(docId.ToString()))
                {
                    resultado.Errores.Add(new ImportarClientesErrorDto { Fila = filaNumero, Mensaje = "Documento duplicado en archivo." });
                    continue;
                }

                try
                {
                    var clienteExistente = await _repo.GetByDocumentoIdentidadAsync(docId);

                    if (clienteExistente != null)
                    {
                        // Actualizar cliente
                        clienteExistente.ActualizarDatosContacto(fila.Email, fila.Celular);
                        clienteExistente.ActualizarDireccion(fila.Direccion);
                        clienteExistente.ActualizarNombre(fila.Nombre);
                        await _repo.UpdateAsync(clienteExistente);
                        resultado.Actualizados++;
                        // Publicar evento ClienteModificado si aplica
                    }
                    else
                    {
                        // Crear cliente
                        var nuevoCliente = new Cliente(
                            Guid.NewGuid(),
                            docId,
                            fila.Nombre,
                            fila.Email,
                            fila.Celular,
                            fila.Direccion,
                            TipoCliente.SinDefinir, // O según lógica
                            EstadoCliente.Activo
                        );
                        await _repo.AddAsync(nuevoCliente);
                        resultado.Creados++;
                        // Publicar evento ClienteCreado si aplica
                    }
                }
                catch (Exception ex)
                {
                    resultado.Errores.Add(new ImportarClientesErrorDto { Fila = filaNumero, Mensaje = ex.Message });
                }
            }

            return resultado;
        }

        // Este método debe parsear el archivo y devolver una lista de filas (puedes usar CsvHelper, ExcelDataReader, etc.)
        private List<FilaClienteDto> LeerArchivo(byte[] archivo, string nombreArchivo, out List<ImportarClientesErrorDto> errores)
{
    errores = new List<ImportarClientesErrorDto>();
    var filas = new List<FilaClienteDto>();

    try
    {
        using var stream = new MemoryStream(archivo);
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        var isFirstRow = true;
        var colMap = new Dictionary<string, int>();

        while (reader.Read())
        {
            if (isFirstRow)
            {
                // Mapear columnas por nombre
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var col = reader.GetValue(i)?.ToString()?.Trim().ToLowerInvariant();
                    if (!string.IsNullOrEmpty(col))
                        colMap[col] = i;
                }
                // Validar columnas obligatorias
                var requeridas = new[] { "tipodocumento", "numerodocumento", "nombre" };
                foreach (var req in requeridas)
                {
                    if (!colMap.ContainsKey(req))
                    {
                        errores.Add(new ImportarClientesErrorDto { Fila = 1, Mensaje = $"Falta columna obligatoria: {req}" });
                    }
                }
                if (errores.Count > 0) return filas;
                isFirstRow = false;
                continue;
            }

            // Leer datos de la fila
            var fila = new FilaClienteDto
            {
                TipoDocumento = reader.GetValue(colMap["tipodocumento"])?.ToString() ?? "",
                NumeroDocumento = reader.GetValue(colMap["numerodocumento"])?.ToString() ?? "",
                Nombre = reader.GetValue(colMap["nombre"])?.ToString() ?? "",
                Email = colMap.ContainsKey("email") ? reader.GetValue(colMap["email"])?.ToString() ?? "" : "",
                Celular = colMap.ContainsKey("celular") ? reader.GetValue(colMap["celular"])?.ToString() ?? "" : "",
                Direccion = colMap.ContainsKey("direccion") ? reader.GetValue(colMap["direccion"])?.ToString() ?? "" : ""
            };
            filas.Add(fila);
        }
    }
    catch (Exception ex)
    {
        errores.Add(new ImportarClientesErrorDto { Fila = 0, Mensaje = $"Error al leer archivo: {ex.Message}" });
    }

    return filas;
}

        // DTO interno para cada fila
        private class FilaClienteDto
        {
            public string TipoDocumento { get; set; } = string.Empty;
            public string NumeroDocumento { get; set; } = string.Empty;
            public string Nombre { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Celular { get; set; } = string.Empty;
            public string Direccion { get; set; } = string.Empty;
        }
    }
}