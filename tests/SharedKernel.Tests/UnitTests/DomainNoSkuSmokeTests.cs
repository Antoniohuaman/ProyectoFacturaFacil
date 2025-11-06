using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace SharedKernel.Tests.UnitTests
{
    [TestFixture]
    public class DomainNoSkuSmokeTests
    {
        private static string FindSolutionRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var sln = Path.Combine(dir.FullName, "ProyectoFacturaFacil.sln");
                if (File.Exists(sln)) return dir.FullName;
                dir = dir.Parent;
            }
            throw new DirectoryNotFoundException("No se encontró la raíz de la solución (ProyectoFacturaFacil.sln).");
        }

        [Test]
        public void DomainAssemblies_ShouldNotReference_SkuVO_WithAllowlist()
        {
            var root = FindSolutionRoot();
            var domainDlls = Directory.GetFiles(Path.Combine(root, "src"), "*.Domain.dll", SearchOption.AllDirectories)
                                       .Where(p => p.Contains(Path.Combine("bin", "Debug")))
                                       .Where(p => string.Equals(Path.GetFileName(p), "ListaPreciosBC.Domain.dll", StringComparison.Ordinal))
                                       .Where(p => p.Replace('\\','/').Contains("/src/ListaPreciosBC/Domain/", StringComparison.Ordinal))
                                       .ToArray();
            Assert.That(domainDlls.Length, Is.EqualTo(1), $"Se esperó 1 ensamblado ListaPreciosBC.Domain, encontrados: {domainDlls.Length}:\n{string.Join("\n", domainDlls)}");

            var violations = new List<string>();

            // Allowlist: transitoria, hasta remover SKU del dominio de ListaPreciosBC.
            // Permitimos sólo la interfaz IPrecioProductoRepository de ListaPreciosBC.Domain y sus métodos ObtenerPorSkuAsync/EliminarAsync
            var allowType = "ListaPreciosBC.Domain.Repositories.IPrecioProductoRepository";
            var allowMembers = new HashSet<string>(StringComparer.Ordinal)
            {
                "ObtenerPorSkuAsync",
                "EliminarAsync"
            };

            foreach (var dll in domainDlls)
            {
                Assembly asm;
                try
                {
                    asm = Assembly.LoadFrom(dll);
                }
                catch (FileLoadException)
                {
                    // Ya cargado: reusar el que coincide por FullName
                    var name = AssemblyName.GetAssemblyName(dll);
                    asm = AppDomain.CurrentDomain.GetAssemblies().First(a => a.FullName == name.FullName);
                }
                foreach (var t in asm.GetTypes())
                {
                    // fields
                    foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                    {
                        if (TypeOrGenericContainsSku(f.FieldType))
                            violations.Add($"{t.FullName}::{f.Name} (field) -> {f.FieldType.FullName}");
                    }

                    // properties
                    foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                    {
                        if (TypeOrGenericContainsSku(p.PropertyType))
                            violations.Add($"{t.FullName}::{p.Name} (prop) -> {p.PropertyType.FullName}");
                    }

                    // methods (params + return)
                    foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                    {
                        bool match = TypeOrGenericContainsSku(m.ReturnType) || m.GetParameters().Any(pa => TypeOrGenericContainsSku(pa.ParameterType));
                        if (!match) continue;

                        // Allowlist filter
                        if (t.FullName == allowType && allowMembers.Contains(m.Name))
                            continue;

                        // If it's other type in same namespace (Repositories), but not the allowed type, still count as violation
                        violations.Add($"{t.FullName}::{m.Name} (method)");
                    }
                }
            }

            if (violations.Count > 0)
            {
                Assert.Fail("Se detectó referencia a Sku en Dominios: \n" + string.Join("\n", violations));
            }

            // Local function: checks a type and its generic arguments recursively
            static bool TypeOrGenericContainsSku(Type t)
            {
                if (t.FullName == null) return false;
                if (t.FullName.Equals("SharedKernel.ValueObjects.Sku", StringComparison.Ordinal)) return true;
                if (t.IsArray) return TypeOrGenericContainsSku(t.GetElementType()!);
                if (t.IsGenericType)
                {
                    foreach (var ga in t.GetGenericArguments())
                        if (TypeOrGenericContainsSku(ga)) return true;
                }
                return false;
            }
        }
    }
}
