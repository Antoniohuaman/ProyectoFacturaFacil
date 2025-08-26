using System;
using System.Text.RegularExpressions;
using SharedKernel.Exceptions; 

namespace ConfiguracionSistemaBC.Domain.ValueObjects
{

    // Datos personales
    public sealed record CorreoElectronico(string Valor)
    {
        private static readonly Regex R = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        public static CorreoElectronico Crear(string v)
        {
            if (string.IsNullOrWhiteSpace(v))
                throw new BusinessRuleException("Email es obligatorio.");
            v = v.Trim();
            if (!R.IsMatch(v))
                throw new BusinessRuleException("Email inválido.");
            return new(v);
        }

        public override string ToString() => Valor;
    }

    public sealed record NombrePersona(string Nombres, string Apellidos)
    {
        public static NombrePersona Crear(string nombres, string apellidos)
        {
            if (string.IsNullOrWhiteSpace(nombres))
                throw new BusinessRuleException("Nombres es obligatorio.");
            if (string.IsNullOrWhiteSpace(apellidos))
                throw new BusinessRuleException("Apellidos es obligatorio.");
            return new(nombres.Trim(), apellidos.Trim());
        }

        public string Completo => $"{Nombres} {Apellidos}";
        public override string ToString() => Completo;
    }

    // Seguridad
    public sealed record PasswordHash(string Valor)
    {
        public static PasswordHash DesdeHash(string hash) =>
            string.IsNullOrWhiteSpace(hash)
                ? throw new BusinessRuleException("Hash de contraseña es obligatorio.")
                : new(hash);

        // Si prefieres recibir texto plano en el UC y hashearlo aquí
        public static PasswordHash DesdeTextoPlano(string passwordPlano, IPasswordHasher hasher)
        {
            if (hasher is null) throw new ArgumentNullException(nameof(hasher));
            if (string.IsNullOrWhiteSpace(passwordPlano))
                throw new BusinessRuleException("La contraseña no puede ser vacía.");
            return new(hasher.Hash(passwordPlano));
        }
    }

    // Servicios de dominio (interfaces)
    public interface IPasswordHasher
    {
        string Hash(string textoPlano);
    }

    public interface IUnicidadUsuarioEmpleadoService
    {
    bool EsEmailUnicoPorEmpresa(EmpresaId empresaId, SharedKernel.ValueObjects.Email email);
    }
}
