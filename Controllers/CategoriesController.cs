using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecurePass.Data;
using SecurePass.Models.ViewModels;

namespace SecurePass.Controllers
{
    [Authorize]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var passwords = await _context.PasswordVaults
                .Where(p => p.UserId == userId)
                .ToListAsync();

            var categoryMetadata = new List<(string Name, string Icon, string Color, string Description)>
            {
                ("Banking", "fa-building-columns", "#22C55E", "Financial institutions, net banking, payment gateways & crypto wallets"),
                ("Work", "fa-briefcase", "#6D5DF6", "Corporate tools, VPNs, dev accounts, servers & enterprise software"),
                ("Social", "fa-share-nodes", "#3B82F6", "Social media networks, chat apps, forums & messaging platforms"),
                ("Education", "fa-graduation-cap", "#EC4899", "University portals, online courses, research databases & LMS"),
                ("Shopping", "fa-cart-shopping", "#F59E0B", "E-commerce stores, retail accounts, order tracking & subscriptions"),
                ("Entertainment", "fa-film", "#8B5CF6", "Streaming video, music platforms, gaming consoles & leisure"),
                ("Personal", "fa-user-lock", "#06B6D4", "Personal emails, cloud storage, smart home & private accounts"),
                ("Others", "fa-folder-open", "#64748B", "Miscellaneous services, utilities, and general credentials")
            };

            var list = categoryMetadata.Select(cat => new
            {
                Name = cat.Name,
                Icon = cat.Icon,
                Color = cat.Color,
                Description = cat.Description,
                Count = passwords.Count(p => p.Category == cat.Name),
                AvgScore = passwords.Any(p => p.Category == cat.Name)
                    ? (int)passwords.Where(p => p.Category == cat.Name).Average(p => p.StrengthScore)
                    : 0
            }).ToList();

            ViewBag.CategoryStats = list;
            return View();
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out int id) ? id : 0;
        }
    }
}
