using System.Security.Claims;
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
    public class VaultController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEncryptionService _encryption;
        private readonly IPasswordAnalyzerService _analyzer;

        private static readonly List<string> PredefinedCategories = new()
        {
            "Social", "Banking", "Education", "Work", "Personal", "Shopping", "Entertainment", "Others"
        };

        public VaultController(ApplicationDbContext context, IEncryptionService encryption, IPasswordAnalyzerService analyzer)
        {
            _context = context;
            _encryption = encryption;
            _analyzer = analyzer;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            string? category,
            string? strength,
            string? sortBy = "latest",
            int page = 1,
            int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            var query = _context.PasswordVaults.Where(p => p.UserId == userId);

            // Search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(p => p.Title.ToLower().Contains(s) ||
                                         (p.Website != null && p.Website.ToLower().Contains(s)) ||
                                         (p.Username != null && p.Username.ToLower().Contains(s)) ||
                                         (p.Notes != null && p.Notes.ToLower().Contains(s)));
            }

            // Category filter
            if (!string.IsNullOrWhiteSpace(category) && category != "All")
            {
                query = query.Where(p => p.Category == category);
            }

            // Strength filter
            if (!string.IsNullOrWhiteSpace(strength) && strength != "All")
            {
                query = query.Where(p => p.PasswordStrength == strength);
            }

            // Sorting
            query = sortBy switch
            {
                "oldest" => query.OrderBy(p => p.CreatedAt),
                "title" => query.OrderBy(p => p.Title),
                "score_desc" => query.OrderByDescending(p => p.StrengthScore),
                "score_asc" => query.OrderBy(p => p.StrengthScore),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PasswordVaultItemViewModel
                {
                    Id = p.Id,
                    Title = p.Title,
                    Website = p.Website,
                    Username = p.Username,
                    PasswordStrength = p.PasswordStrength,
                    StrengthScore = p.StrengthScore,
                    Category = p.Category,
                    Notes = p.Notes,
                    IsFavorite = p.IsFavorite,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    IsExpired = (DateTime.UtcNow - p.CreatedAt).TotalDays > 90,
                    AgeInDays = (int)(DateTime.UtcNow - p.CreatedAt).TotalDays
                })
                .ToListAsync();

            var model = new VaultIndexViewModel
            {
                Items = items,
                SearchQuery = search,
                CategoryFilter = category,
                StrengthFilter = strength,
                SortBy = sortBy,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalItems = totalItems,
                PageSize = pageSize,
                Categories = PredefinedCategories
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult Create(string? prefill = null)
        {
            ViewBag.Categories = PredefinedCategories;
            var model = new PasswordVaultCreateEditModel();
            if (!string.IsNullOrEmpty(prefill))
            {
                model.Password = prefill;
                model.ConfirmPassword = prefill;
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PasswordVaultCreateEditModel model)
        {
            ViewBag.Categories = PredefinedCategories;

            if (!ModelState.IsValid)
                return View(model);

            var userId = GetCurrentUserId();

            // Duplicate title check
            var titleExists = await _context.PasswordVaults
                .AnyAsync(p => p.UserId == userId && p.Title.ToLower() == model.Title.Trim().ToLower());

            if (titleExists)
            {
                ModelState.AddModelError("Title", $"A password entry with title '{model.Title}' already exists. Use a unique title (e.g., '{model.Title} 2').");
                return View(model);
            }

            // Analyze strength
            var analysis = _analyzer.Analyze(model.Password, $"{model.Username} {model.Title}");

            // Encrypt password
            var encrypted = _encryption.Encrypt(model.Password);

            // Duplicate password check across other user accounts
            var existingDuplicates = await _context.PasswordVaults
                .Where(p => p.UserId == userId)
                .ToListAsync();

            var duplicateTitles = existingDuplicates
                .Where(p => _encryption.Decrypt(p.EncryptedPassword) == model.Password)
                .Select(p => p.Title)
                .ToList();

            var entry = new PasswordVault
            {
                UserId = userId,
                Title = model.Title.Trim(),
                Website = model.Website?.Trim(),
                Username = model.Username?.Trim(),
                EncryptedPassword = encrypted,
                PasswordStrength = analysis.StrengthLevel,
                StrengthScore = analysis.Score,
                Category = model.Category,
                Notes = model.Notes?.Trim(),
                IsFavorite = model.IsFavorite,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.PasswordVaults.Add(entry);
            await _context.SaveChangesAsync();

            if (duplicateTitles.Any())
            {
                TempData["WarningMessage"] = $"Password saved! Note: This exact password is also used for: {string.Join(", ", duplicateTitles)}. Consider using unique passwords.";
            }
            else
            {
                TempData["SuccessMessage"] = $"Password for '{entry.Title}' has been securely encrypted and stored.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Categories = PredefinedCategories;
            var userId = GetCurrentUserId();

            var entry = await _context.PasswordVaults.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
            if (entry == null) return NotFound();

            var decrypted = _encryption.Decrypt(entry.EncryptedPassword);

            var model = new PasswordVaultCreateEditModel
            {
                Id = entry.Id,
                Title = entry.Title,
                Website = entry.Website,
                Username = entry.Username,
                Password = decrypted,
                ConfirmPassword = decrypted,
                Category = entry.Category,
                Notes = entry.Notes,
                IsFavorite = entry.IsFavorite,
                StrengthScore = entry.StrengthScore,
                PasswordStrength = entry.PasswordStrength
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PasswordVaultCreateEditModel model)
        {
            ViewBag.Categories = PredefinedCategories;

            if (!ModelState.IsValid)
                return View(model);

            var userId = GetCurrentUserId();
            var entry = await _context.PasswordVaults.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
            if (entry == null) return NotFound();

            // Check if another entry with same title exists
            var titleExists = await _context.PasswordVaults
                .AnyAsync(p => p.UserId == userId && p.Id != id && p.Title.ToLower() == model.Title.Trim().ToLower());

            if (titleExists)
            {
                ModelState.AddModelError("Title", $"Another password entry with title '{model.Title}' already exists.");
                return View(model);
            }

            var analysis = _analyzer.Analyze(model.Password, $"{model.Username} {model.Title}");

            entry.Title = model.Title.Trim();
            entry.Website = model.Website?.Trim();
            entry.Username = model.Username?.Trim();
            entry.EncryptedPassword = _encryption.Encrypt(model.Password);
            entry.PasswordStrength = analysis.StrengthLevel;
            entry.StrengthScore = analysis.Score;
            entry.Category = model.Category;
            entry.Notes = model.Notes?.Trim();
            entry.IsFavorite = model.IsFavorite;
            entry.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Password for '{entry.Title}' has been updated and re-encrypted.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = GetCurrentUserId();
            var entry = await _context.PasswordVaults.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
            if (entry == null) return NotFound();

            var decrypted = _encryption.Decrypt(entry.EncryptedPassword);
            var analysis = _analyzer.Analyze(decrypted, $"{entry.Username} {entry.Title}");

            ViewBag.Analysis = analysis;
            ViewBag.DecryptedPassword = decrypted;

            var model = new PasswordVaultItemViewModel
            {
                Id = entry.Id,
                Title = entry.Title,
                Website = entry.Website,
                Username = entry.Username,
                PasswordStrength = entry.PasswordStrength,
                StrengthScore = entry.StrengthScore,
                Category = entry.Category,
                Notes = entry.Notes,
                IsFavorite = entry.IsFavorite,
                CreatedAt = entry.CreatedAt,
                UpdatedAt = entry.UpdatedAt,
                IsExpired = entry.IsExpired,
                AgeInDays = entry.AgeInDays
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetCurrentUserId();
            var entry = await _context.PasswordVaults.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
            if (entry != null)
            {
                _context.PasswordVaults.Remove(entry);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Password for '{entry.Title}' was permanently deleted.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> RevealPassword([FromForm] int? id, [FromQuery] int? qid)
        {
            int targetId = id ?? qid ?? 0;
            var userId = GetCurrentUserId();
            var entry = await _context.PasswordVaults.FirstOrDefaultAsync(p => p.Id == targetId && p.UserId == userId);
            if (entry == null)
            {
                return Json(new PasswordRevealResponse { Success = false, Message = "Password not found." });
            }

            var decrypted = _encryption.Decrypt(entry.EncryptedPassword);
            return Json(new PasswordRevealResponse { Success = true, Password = decrypted });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleFavorite([FromForm] int? id, [FromQuery] int? qid)
        {
            int targetId = id ?? qid ?? 0;
            var userId = GetCurrentUserId();
            var entry = await _context.PasswordVaults.FirstOrDefaultAsync(p => p.Id == targetId && p.UserId == userId);
            if (entry == null) return Json(new { success = false });

            entry.IsFavorite = !entry.IsFavorite;
            await _context.SaveChangesAsync();

            return Json(new { success = true, isFavorite = entry.IsFavorite });
        }

        [HttpPost]
        public async Task<IActionResult> CheckDuplicate([FromBody] DuplicateCheckRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Password))
                return Json(new DuplicateCheckResponse { IsDuplicate = false });

            var userId = GetCurrentUserId();
            var userPasswords = await _context.PasswordVaults
                .Where(p => p.UserId == userId && (!request.CurrentId.HasValue || p.Id != request.CurrentId.Value))
                .ToListAsync();

            var matches = userPasswords
                .Where(p => _encryption.Decrypt(p.EncryptedPassword) == request.Password)
                .Select(p => p.Title)
                .ToList();

            return Json(new DuplicateCheckResponse
            {
                IsDuplicate = matches.Any(),
                DuplicateTitles = matches,
                Message = matches.Any() ? $"This password is already used for: {string.Join(", ", matches)}" : null
            });
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out int id) ? id : 0;
        }
    }
}

