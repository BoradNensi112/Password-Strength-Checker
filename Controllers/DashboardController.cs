using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecurePass.Data;
using SecurePass.Models.ViewModels;

namespace SecurePass.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var passwords = await _context.PasswordVaults
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var total = passwords.Count;
            var weak = passwords.Count(p => p.PasswordStrength == "Weak" || p.StrengthScore < 40);
            var medium = passwords.Count(p => p.PasswordStrength == "Medium" || (p.StrengthScore >= 40 && p.StrengthScore < 70));
            var strong = passwords.Count(p => p.PasswordStrength == "Strong" || (p.StrengthScore >= 70 && p.StrengthScore < 90));
            var veryStrong = passwords.Count(p => p.PasswordStrength == "Very Strong" || p.StrengthScore >= 90);

            var avgScore = total > 0 ? (int)passwords.Average(p => p.StrengthScore) : 0;
            var expiredCount = passwords.Count(p => p.IsExpired);
            var favCount = passwords.Count(p => p.IsFavorite);

            // Duplicate detection across encrypted passwords
            var duplicateCount = passwords
                .GroupBy(p => p.EncryptedPassword)
                .Where(g => g.Count() > 1)
                .Sum(g => g.Count());

            // Health Score calculation (0 to 100%)
            int healthScore = 100;
            if (total > 0)
            {
                double weakPenalty = ((double)weak / total) * 40;
                double mediumPenalty = ((double)medium / total) * 15;
                double duplicatePenalty = ((double)duplicateCount / total) * 25;
                double expiredPenalty = ((double)expiredCount / total) * 20;

                healthScore = (int)Math.Max(10, Math.Round(100 - weakPenalty - mediumPenalty - duplicatePenalty - expiredPenalty));
            }

            var categoryCounts = passwords
                .GroupBy(p => p.Category ?? "Others")
                .ToDictionary(g => g.Key, g => g.Count());

            // Weekly activity points
            var weeklyActivity = new List<StrengthHistoryPoint>();
            var today = DateTime.UtcNow.Date;
            for (int i = 6; i >= 0; i--)
            {
                var targetDate = today.AddDays(-i);
                var count = passwords.Count(p => p.CreatedAt.Date == targetDate);
                weeklyActivity.Add(new StrengthHistoryPoint
                {
                    Day = targetDate.ToString("ddd"),
                    Count = count
                });
            }

            var recentItems = passwords.Take(5).Select(p => new PasswordVaultItemViewModel
            {
                Id = p.Id,
                Title = p.Title,
                Website = p.Website,
                Username = p.Username,
                PasswordStrength = p.PasswordStrength,
                StrengthScore = p.StrengthScore,
                Category = p.Category,
                IsFavorite = p.IsFavorite,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                IsExpired = p.IsExpired,
                AgeInDays = p.AgeInDays
            }).ToList();

            var viewModel = new DashboardViewModel
            {
                TotalPasswords = total,
                WeakPasswords = weak,
                MediumPasswords = medium,
                StrongPasswords = strong,
                VeryStrongPasswords = veryStrong,
                AverageStrengthScore = avgScore,
                HealthScore = healthScore,
                ExpiredPasswordsCount = expiredCount,
                DuplicatePasswordsCount = duplicateCount,
                FavoriteCount = favCount,
                CategoryCounts = categoryCounts,
                WeeklyActivity = weeklyActivity,
                RecentPasswords = recentItems
            };

            return View(viewModel);
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out int id) ? id : 0;
        }
    }
}
