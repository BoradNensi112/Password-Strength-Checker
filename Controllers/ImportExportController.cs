using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecurePass.Data;
using SecurePass.Models;
using SecurePass.Models.ViewModels;
using SecurePass.Services;

namespace SecurePass.Controllers
{
    [Authorize]
    public class ImportExportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEncryptionService _encryption;
        private readonly IPasswordAnalyzerService _analyzer;

        public ImportExportController(ApplicationDbContext context, IEncryptionService encryption, IPasswordAnalyzerService analyzer)
        {
            _context = context;
            _encryption = encryption;
            _analyzer = analyzer;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new ImportExportViewModel());
        }

        [HttpGet]
        public async Task<IActionResult> ExportCsv()
        {
            var userId = GetCurrentUserId();
            var passwords = await _context.PasswordVaults
                .Where(p => p.UserId == userId)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Title,Website,Username,Password,Category,Notes,CreatedDate");

            foreach (var p in passwords)
            {
                var decrypted = _encryption.Decrypt(p.EncryptedPassword);
                sb.AppendLine($"\"{EscapeCsv(p.Title)}\",\"{EscapeCsv(p.Website)}\",\"{EscapeCsv(p.Username)}\",\"{EscapeCsv(decrypted)}\",\"{EscapeCsv(p.Category)}\",\"{EscapeCsv(p.Notes)}\",\"{p.CreatedAt:yyyy-MM-dd HH:mm:ss}\"");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"SecurePass_Export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        }

        [HttpGet]
        public async Task<IActionResult> ExportJson()
        {
            var userId = GetCurrentUserId();
            var passwords = await _context.PasswordVaults
                .Where(p => p.UserId == userId)
                .ToListAsync();

            var exportList = passwords.Select(p => new
            {
                p.Title,
                p.Website,
                p.Username,
                Password = _encryption.Decrypt(p.EncryptedPassword),
                p.Category,
                p.Notes,
                p.PasswordStrength,
                p.StrengthScore,
                p.CreatedAt,
                p.UpdatedAt
            }).ToList();

            var json = JsonSerializer.Serialize(exportList, new JsonSerializerOptions { WriteIndented = true });
            var bytes = Encoding.UTF8.GetBytes(json);
            return File(bytes, "application/json", $"SecurePass_Backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportCsv(ImportExportViewModel model)
        {
            if (model.CsvFile == null || model.CsvFile.Length == 0)
            {
                ModelState.AddModelError("CsvFile", "Please upload a valid CSV file.");
                return View("Index", model);
            }

            var userId = GetCurrentUserId();
            int imported = 0;
            int skipped = 0;
            var messages = new List<string>();

            using (var stream = model.CsvFile.OpenReadStream())
            using (var reader = new StreamReader(stream))
            {
                string? header = await reader.ReadLineAsync();
                int lineNum = 1;
                string? line;

                while ((line = await reader.ReadLineAsync()) != null)
                {
                    lineNum++;
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var parts = ParseCsvLine(line);
                    if (parts.Length < 4 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[3]))
                    {
                        skipped++;
                        messages.Add($"Line {lineNum}: Skipped due to missing title or password.");
                        continue;
                    }

                    string title = parts[0].Trim();
                    string website = parts.Length > 1 ? parts[1].Trim() : string.Empty;
                    string username = parts.Length > 2 ? parts[2].Trim() : string.Empty;
                    string password = parts[3].Trim();
                    string category = parts.Length > 4 && !string.IsNullOrWhiteSpace(parts[4]) ? parts[4].Trim() : "Others";
                    string notes = parts.Length > 5 ? parts[5].Trim() : string.Empty;

                    // Check if title exists
                    var exists = await _context.PasswordVaults.AnyAsync(p => p.UserId == userId && p.Title.ToLower() == title.ToLower());
                    if (exists)
                    {
                        title = $"{title} (Imported {DateTime.UtcNow:MMdd})";
                    }

                    var analysis = _analyzer.Analyze(password);
                    var entry = new PasswordVault
                    {
                        UserId = userId,
                        Title = title,
                        Website = string.IsNullOrWhiteSpace(website) ? null : website,
                        Username = string.IsNullOrWhiteSpace(username) ? null : username,
                        EncryptedPassword = _encryption.Encrypt(password),
                        PasswordStrength = analysis.StrengthLevel,
                        StrengthScore = analysis.Score,
                        Category = category,
                        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.PasswordVaults.Add(entry);
                    imported++;
                }

                await _context.SaveChangesAsync();
            }

            model.ImportedCount = imported;
            model.SkippedCount = skipped;
            model.ImportMessages = messages;

            TempData["SuccessMessage"] = $"Successfully imported {imported} passwords! (Skipped: {skipped})";
            return View("Index", model);
        }

        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("\"", "\"\"");
        }

        private static string[] ParseCsvLine(string line)
        {
            var list = new List<string>();
            bool inQuotes = false;
            var current = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '\"')
                {
                    if (inQuotes && i < line.Length - 1 && line[i + 1] == '\"')
                    {
                        current.Append('\"');
                        i++; // Skip escaped quote
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    list.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            list.Add(current.ToString());
            return list.ToArray();
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out int id) ? id : 0;
        }
    }
}


