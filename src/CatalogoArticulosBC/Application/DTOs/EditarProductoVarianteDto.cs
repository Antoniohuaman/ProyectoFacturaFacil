using System;
using System.Collections.Generic;

namespace CatalogoArticulosBC.Application.DTOs
{
    public class EditarProductoVarianteDto
    {
        public Guid ProductoVarianteId { get; }
        public string NuevoSku { get; }
        public IEnumerable<KeyValuePair<string, string>> NuevosAtributos { get; }

        public EditarProductoVarianteDto(Guid productoVarianteId, string nuevoSku, IEnumerable<KeyValuePair<string, string>> nuevosAtributos)
        {
            ProductoVarianteId = productoVarianteId;
            NuevoSku = nuevoSku;
            NuevosAtributos = nuevosAtributos;
        }
    }
}