using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecurePass.Data;
using SecurePass.Models.ViewModels;
using SecurePass.Services;

namespace SecurePass.Controllers
{
    public class GeneratorController : Controller
    {
        private readonly IPasswordGeneratorService _generator;
        private readonly ApplicationDbContext _context;

        public GeneratorController(IPasswordGeneratorService generator, ApplicationDbContext context)
        {
            _generator = generator;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var options = new GeneratorOptions
            {
                Length = 16,
                IncludeUppercase = true,
                IncludeLowercase = true,
                IncludeNumbers = true,
                IncludeSymbols = true,
                AvoidSimilarChars = true
            };

            var userId = GetCurrentUserId();
            if (userId > 0)
            {
                var setting = await _context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId);
                if (setting != null)
                {
                    options.Length = setting.DefaultGeneratorLength > 0 ? setting.DefaultGeneratorLength : 16;
                    options.IncludeUppercase = setting.IncludeUppercase;
                    options.IncludeLowercase = setting.IncludeLowercase;
                    options.IncludeNumbers = setting.IncludeNumbers;
                    options.IncludeSymbols = setting.IncludeSymbols;
                    options.AvoidSimilarChars = setting.AvoidSimilarChars;
                }
            }

            var initialResult = _generator.Generate(options);
            ViewBag.InitialResult = initialResult;
            ViewBag.InitialOptions = options;

            return View();
        }

        [HttpPost]
        public IActionResult Generate([FromBody] GeneratorOptions options)
        {
            var result = _generator.Generate(options);
            return Json(result);
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out int id) ? id : 0;
        }
    }
}
