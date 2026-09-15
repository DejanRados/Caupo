using System;
using System.Collections.Generic;
namespace Caupo.ArticleImport
{
    public sealed class ArticleImportTargetField
    {
        public string Name { get; set; } = string.Empty;
        public bool Required { get; set; }
        public bool CanBeAutomatic { get; set; }
        public bool CanHaveFixedValue { get; set; } = true;
    }

    public static class ArticleImportTargetFields
    {
        public static IReadOnlyList<ArticleImportTargetField> All { get; } =
        [
            new() { Name = "ID", Required = false, CanBeAutomatic = true },
        new() { Name = "Šifra", Required = true, CanBeAutomatic = true },
        new() { Name = "Interna šifra", Required = true, CanBeAutomatic = true },
        new() { Name = "Naziv", Required = true },
        new() { Name = "Cijena", Required = true },
        new() { Name = "Vrsta", Required = true },
        new() { Name = "Kategorija", Required = true },
        new() { Name = "Jedinica", Required = true },
        new() { Name = "Porez", Required = true },
        new() { Name = "Normativ", Required = true, CanBeAutomatic = true },
        new() { Name = "Pozicija", Required = true, CanBeAutomatic = true },
        new() { Name = "Aktivan", Required = true },
        new() { Name = "Porez na potrošnju", Required = false },
        new() { Name = "Slika", Required = false }
        ];
    }
}