using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecurePass.Data;
using SecurePass.Models.ViewModels;

namespace SecurePass.Controllers
{
    [Authorize]
    public class FavoritesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FavoritesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var favorites = await _context.PasswordVaults
                .Where(p => p.UserId == userId && p.IsFavorite)
                .OrderByDescending(p => p.UpdatedAt)
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

            return View(favorites);
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out int id) ? id : 0;
        }
    }
}
