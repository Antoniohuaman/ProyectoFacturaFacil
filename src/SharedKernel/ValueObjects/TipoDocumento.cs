namespace SharedKernel.ValueObjects;

/// <summary>
/// Enum centralizado para tipos de documento de identidad (Catálogo 06 SUNAT + uso interno).
/// Usar en DocumentoIdentidad y en todos los modelos/tests que requieran referenciar tipos normativos.
/// </summary>
public enum TipoDocumento
{
	Ruc,                                   // schemeID "6"
	Dni,                                   // schemeID "1"
	CarnetExtranjeria,                     // schemeID "4"
	Pasaporte,                             // schemeID "7"
	CedulaDiplomatica,                     // schemeID "A"
	DocIdentidadPaisResidenciaNoDomiciliado, // schemeID "B"
	TinPersonaNatural,                     // schemeID "C"
	InPersonaJuridica,                     // schemeID "D"
	SinDocumento                           // SOLO interno (no UBL)
}
