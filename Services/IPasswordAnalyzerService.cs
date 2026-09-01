using SecurePass.Models.ViewModels;

namespace SecurePass.Services
{
    public interface IPasswordAnalyzerService
    {
        PasswordAnalysisResult Analyze(string password, string? personalInfo = null);
        bool IsCommonPassword(string password);
    }
}
