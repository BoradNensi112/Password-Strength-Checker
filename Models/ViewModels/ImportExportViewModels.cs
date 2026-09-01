using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace SecurePass.Models.ViewModels
{
    public class ImportExportViewModel
    {
        [Required(ErrorMessage = "Please select a CSV file to import")]
        [Display(Name = "CSV File")]
        public IFormFile? CsvFile { get; set; }

        public int ImportedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<string> ImportMessages { get; set; } = new();
    }

    public class CsvVaultRecord
    {
        public string Title { get; set; } = string.Empty;
        public string? Website { get; set; }
        public string? Username { get; set; }
        public string Password { get; set; } = string.Empty;
        public string Category { get; set; } = "Personal";
        public string? Notes { get; set; }
    }

    public class SettingsViewModel
    {
        public string ThemeMode { get; set; } = "dark";
        public int AutoLockMinutes { get; set; } = 15;
        public int DefaultGeneratorLength { get; set; } = 16;
        public bool IncludeSymbols { get; set; } = true;
        public bool IncludeNumbers { get; set; } = true;
        public bool IncludeUppercase { get; set; } = true;
        public bool IncludeLowercase { get; set; } = true;
        public bool AvoidSimilarChars { get; set; } = true;
    }
}
