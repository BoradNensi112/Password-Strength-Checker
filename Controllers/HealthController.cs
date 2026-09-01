using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecurePass.Data;
using SecurePass.Models.ViewModels;
using SecurePass.Services;

namespace SecurePass.Controllers
{
    [Authorize]
    public class HealthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEncryptionService _encryption;
        private readonly IPasswordAnalyzerService _analyzer;

        public HealthController(ApplicationDbContext context, IEncryptionService encryption, IPasswordAnalyzerService analyzer)
        {
            _context = context;
            _encryption = encryption;
            _analyzer = analyzer;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var passwords = await _context.PasswordVaults
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var total = passwords.Count;
            var weakList = new List<PasswordVaultItemViewModel>();
            var mediumList = new List<PasswordVaultItemViewModel>();
            var strongList = new List<PasswordVaultItemViewModel>();
            var expiredList = new List<PasswordVaultItemViewModel>();
            var plainPasswordMap = new Dictionary<string, List<PasswordVaultItemViewModel>>();

            foreach (var p in passwords)
            {
                var decrypted = _encryption.Decrypt(p.EncryptedPassword);
                var itemVm = new PasswordVaultItemViewModel
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
                };

                if (p.StrengthScore < 40 || p.PasswordStrength == "Weak")
                {
                    weakList.Add(itemVm);
                }
                else if (p.StrengthScore < 70)
                {
                    mediumList.Add(itemVm);
                }
                else
                {
                    strongList.Add(itemVm);
                }

                if (itemVm.IsExpired)
                {
                    expiredList.Add(itemVm);
                }

                if (!string.IsNullOrEmpty(decrypted))
                {
                    if (!plainPasswordMap.ContainsKey(decrypted))
                        plainPasswordMap[decrypted] = new List<PasswordVaultItemViewModel>();

                    plainPasswordMap[decrypted].Add(itemVm);
                }
            }

            var duplicateGroups = plainPasswordMap
                .Where(kvp => kvp.Value.Count > 1)
                .Select(kvp => new DuplicateGroupViewModel
                {
                    MaskedPassword = MaskPassword(kvp.Key),
                    Count = kvp.Value.Count,
                    Items = kvp.Value
                })
                .ToList();

            var duplicateCount = duplicateGroups.Sum(g => g.Count);

            // Compute overall health percentage
            int healthScore = 100;
            if (total > 0)
            {
                double weakPenalty = ((double)weakList.Count / total) * 45;
                double mediumPenalty = ((double)mediumList.Count / total) * 15;
                double duplicatePenalty = ((double)duplicateCount / total) * 25;
                double expiredPenalty = ((double)expiredList.Count / total) * 15;

                healthScore = (int)Math.Max(5, Math.Round(100 - weakPenalty - mediumPenalty - duplicatePenalty - expiredPenalty));
            }

            string status = healthScore >= 85 ? "Excellent (Quantum Secured)" :
                            healthScore >= 70 ? "Good (Minor Improvements Needed)" :
                            healthScore >= 50 ? "Moderate (Action Recommended)" :
                            "Critical (Vulnerable to Breach)";

            string statusColor = healthScore >= 85 ? "#10B981" :
                                 healthScore >= 70 ? "#22C55E" :
                                 healthScore >= 50 ? "#F59E0B" :
                                 "#EF4444";

            var recommendations = new List<string>();
            if (weakList.Any())
                recommendations.Add($"Upgrade {weakList.Count} weak passwords to high-entropy 16+ character passwords.");
            if (duplicateGroups.Any())
                recommendations.Add($"Eliminate password reuse across {duplicateCount} accounts to prevent credential stuffing.");
            if (expiredList.Any())
                recommendations.Add($"Rotate {expiredList.Count} passwords that have not been changed in over 90 days.");
            if (!recommendations.Any())
                recommendations.Add("All passwords in your vault meet recommended cybersecurity standards!");

            var model = new PasswordHealthViewModel
            {
                OverallHealthPercentage = healthScore,
                HealthStatus = status,
                HealthStatusColor = statusColor,
                TotalPasswords = total,
                WeakCount = weakList.Count,
                MediumCount = mediumList.Count,
                StrongCount = strongList.Count,
                DuplicateCount = duplicateCount,
                ExpiredCount = expiredList.Count,
                WeakPasswords = weakList,
                DuplicateGroups = duplicateGroups,
                ExpiredPasswords = expiredList,
                ActionableRecommendations = recommendations
            };

            return View(model);
        }

        private static string MaskPassword(string pass)
        {
            if (string.IsNullOrEmpty(pass)) return "••••••••";
            if (pass.Length <= 4) return new string('•', pass.Length);
            return pass[0] + new string('•', pass.Length - 2) + pass[^1];
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out int id) ? id : 0;
        }
    }
}
