using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecurePass.Data;
using SecurePass.Models.ViewModels;

namespace SecurePass.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SettingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var setting = await _context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId);

            var model = new SettingsViewModel
            {
                ThemeMode = setting?.ThemeMode ?? "dark",
                AutoLockMinutes = setting?.AutoLockMinutes ?? 15,
                DefaultGeneratorLength = setting?.DefaultGeneratorLength ?? 16,
                IncludeSymbols = setting?.IncludeSymbols ?? true,
                IncludeNumbers = setting?.IncludeNumbers ?? true,
                IncludeUppercase = setting?.IncludeUppercase ?? true,
                IncludeLowercase = setting?.IncludeLowercase ?? true,
                AvoidSimilarChars = setting?.AvoidSimilarChars ?? true
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(SettingsViewModel model)
        {
            var userId = GetCurrentUserId();
            var setting = await _context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId);

            if (setting == null)
            {
                setting = new Models.UserSetting { UserId = userId };
                _context.UserSettings.Add(setting);
            }

            setting.ThemeMode = model.ThemeMode;
            setting.AutoLockMinutes = model.AutoLockMinutes;
            setting.DefaultGeneratorLength = model.DefaultGeneratorLength;
            setting.IncludeSymbols = model.IncludeSymbols;
            setting.IncludeNumbers = model.IncludeNumbers;
            setting.IncludeUppercase = model.IncludeUppercase;
            setting.IncludeLowercase = model.IncludeLowercase;
            setting.AvoidSimilarChars = model.AvoidSimilarChars;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Preferences and security settings saved successfully.";
            return View(model);
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out int id) ? id : 0;
        }
    }
}
