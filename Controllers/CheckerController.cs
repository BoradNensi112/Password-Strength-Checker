using Microsoft.AspNetCore.Mvc;
using SecurePass.Models.ViewModels;
using SecurePass.Services;

namespace SecurePass.Controllers
{
    public class CheckerController : Controller
    {
        private readonly IPasswordAnalyzerService _analyzer;

        public CheckerController(IPasswordAnalyzerService analyzer)
        {
            _analyzer = analyzer;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Analyze([FromBody] PasswordCheckRequest request)
        {
            var result = _analyzer.Analyze(request.Password, request.UserInfo);
            return Json(result);
        }
    }
}
