using System.Text.RegularExpressions;
using SecurePass.Models.ViewModels;

namespace SecurePass.Services
{
    public class PasswordAnalyzerService : IPasswordAnalyzerService
    {
        private static readonly HashSet<string> CommonPasswords = new(StringComparer.OrdinalIgnoreCase)
        {
            "123456", "password", "12345678", "qwerty", "123456789", "12345", "1234", "111111", "1234567",
            "dragon", "123123", "baseball", "iloveyou", "trustno1", "admin", "welcome", "login", "master",
            "football", "monkey", "sunshine", "princess", "superman", "shadow", "pass1234", "pass@123",
            "test1234", "qwertyuiop", "asdfghjkl", "zxcvbnm", "letmein", "changeme", "secret", "p@ssword",
            "p@ssw0rd", "admin123", "root", "toor", "user", "guest", "default", "securepass", "secure123"
        };

        private static readonly string[] KeyboardPatterns = new[]
        {
            "qwertyuiop", "asdfghjkl", "zxcvbnm",
            "poiuytrewq", "lkjhgfdsa", "mnbvcxz",
            "1234567890", "0987654321",
            "abcdefghijklmnopqrstuvwxyz",
            "zyxwvutsrqponmlkjihgfedcba"
        };

        public PasswordAnalysisResult Analyze(string password, string? personalInfo = null)
        {
            var result = new PasswordAnalysisResult();

            if (string.IsNullOrEmpty(password))
            {
                result.Score = 0;
                result.StrengthLevel = "Empty";
                result.ColorHex = "#64748B";
                result.CrackTimeFormatted = "Instant";
                result.CrackTimeSecondsDesc = "0 seconds";
                result.Suggestions.Add("Enter a password to begin real-time analysis.");
                return result;
            }

            result.Length = password.Length;

            // Character checks
            bool hasUpper = password.Any(char.IsUpper);
            bool hasLower = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));
            bool hasMinLength = password.Length >= 8;
            bool hasStrongLength = password.Length >= 12;
            bool hasVeryStrongLength = password.Length >= 16;

            // Pattern checks
            bool hasRepeated = HasConsecutiveRepeats(password);
            bool hasSequential = HasSequentialPattern(password);
            bool isCommon = IsCommonPassword(password);
            bool hasPersonal = ContainsPersonalInfo(password, personalInfo);

            result.HasMinLength = hasMinLength;
            result.HasUppercase = hasUpper;
            result.HasLowercase = hasLower;
            result.HasNumbers = hasDigit;
            result.HasSpecialChars = hasSpecial;
            result.HasNoRepeatedChars = !hasRepeated;
            result.HasNoSequentialChars = !hasSequential;
            result.IsNotCommonPassword = !isCommon;
            result.HasNoPersonalInfo = !hasPersonal;

            // Calculate Pool Size (CharacterSetSize)
            int poolSize = 0;
            if (hasLower) poolSize += 26;
            if (hasUpper) poolSize += 26;
            if (hasDigit) poolSize += 10;
            if (hasSpecial) poolSize += 33;
            result.CharacterSetSize = poolSize > 0 ? poolSize : 1;

            // Entropy Calculation: E = L * log2(R)
            double entropy = password.Length * Math.Log2(result.CharacterSetSize);
            result.EntropyBits = Math.Round(entropy, 1);

            // Base score from length & variety
            int score = 0;

            // Length points (up to 40 pts)
            if (password.Length >= 20) score += 40;
            else if (password.Length >= 16) score += 35;
            else if (password.Length >= 12) score += 28;
            else if (password.Length >= 8) score += 18;
            else score += password.Length * 2;

            // Character variety points (up to 40 pts)
            int varietyCount = (hasLower ? 1 : 0) + (hasUpper ? 1 : 0) + (hasDigit ? 1 : 0) + (hasSpecial ? 1 : 0);
            score += varietyCount * 10;

            // Entropy bonus (up to 20 pts)
            if (entropy >= 80) score += 20;
            else if (entropy >= 60) score += 15;
            else if (entropy >= 40) score += 10;
            else if (entropy >= 25) score += 5;

            // Penalties
            if (isCommon) score = Math.Min(score, 15);
            if (hasPersonal) score -= 25;
            if (hasSequential) score -= 15;
            if (hasRepeated) score -= 15;
            if (password.Length < 8) score = Math.Min(score, 30);

            // Clamp score between 0 and 100
            result.Score = Math.Clamp(score, 5, 100);

            // Strength level & color
            if (result.Score >= 90)
            {
                result.StrengthLevel = "Very Strong";
                result.ColorHex = "#10B981"; // Emerald
            }
            else if (result.Score >= 70)
            {
                result.StrengthLevel = "Strong";
                result.ColorHex = "#22C55E"; // Green
            }
            else if (result.Score >= 40)
            {
                result.StrengthLevel = "Medium";
                result.ColorHex = "#F59E0B"; // Amber/Orange
            }
            else
            {
                result.StrengthLevel = "Weak";
                result.ColorHex = "#EF4444"; // Red
            }

            // Estimate crack time
            CalculateCrackTime(result, entropy);

            // Generate actionable suggestions
            GenerateSuggestions(result, password, hasUpper, hasLower, hasDigit, hasSpecial, hasStrongLength, isCommon, hasRepeated, hasSequential, hasPersonal);

            // Populate rules checklist
            PopulateRules(result);

            return result;
        }

        public bool IsCommonPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password)) return false;
            var clean = password.Trim().ToLowerInvariant();
            if (CommonPasswords.Contains(clean)) return true;

            // Leetspeak normalized check
            var normalized = clean.Replace("@", "a")
                                  .Replace("0", "o")
                                  .Replace("1", "i")
                                  .Replace("$", "s")
                                  .Replace("3", "e")
                                  .Replace("!", "i");

            return CommonPasswords.Contains(normalized);
        }

        private static bool HasConsecutiveRepeats(string password)
        {
            if (password.Length < 3) return false;
            for (int i = 0; i < password.Length - 2; i++)
            {
                if (password[i] == password[i + 1] && password[i + 1] == password[i + 2])
                    return true;
            }
            return false;
        }

        private static bool HasSequentialPattern(string password)
        {
            if (password.Length < 3) return false;
            var lower = password.ToLowerInvariant();

            foreach (var pattern in KeyboardPatterns)
            {
                for (int i = 0; i <= pattern.Length - 3; i++)
                {
                    var sub = pattern.Substring(i, 3);
                    if (lower.Contains(sub)) return true;
                }
            }

            return false;
        }

        private static bool ContainsPersonalInfo(string password, string? personalInfo)
        {
            if (string.IsNullOrWhiteSpace(personalInfo) || string.IsNullOrWhiteSpace(password))
                return false;

            var tokens = personalInfo.Split(new[] { ' ', '@', '.', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
            var lowerPass = password.ToLowerInvariant();

            foreach (var token in tokens)
            {
                if (token.Length >= 3 && lowerPass.Contains(token.ToLowerInvariant()))
                    return true;
            }

            return false;
        }

        private static void CalculateCrackTime(PasswordAnalysisResult result, double entropy)
        {
            // Crack rate: 10 billion guesses/second (modern GPU cluster)
            // Combinations = 2^entropy
            if (result.Score < 20 || result.Length < 6)
            {
                result.CrackTimeFormatted = "Instant (< 1 sec)";
                result.CrackTimeSecondsDesc = "Less than 1 second";
                return;
            }

            double totalGuesses = Math.Pow(2, entropy);
            double seconds = totalGuesses / 10_000_000_000.0; // 10B guesses/sec

            if (seconds < 1)
            {
                result.CrackTimeFormatted = "A few milliseconds";
                result.CrackTimeSecondsDesc = "< 1 second";
            }
            else if (seconds < 60)
            {
                result.CrackTimeFormatted = $"{(int)seconds} seconds";
                result.CrackTimeSecondsDesc = $"{(int)seconds}s";
            }
            else if (seconds < 3600)
            {
                result.CrackTimeFormatted = $"{(int)(seconds / 60)} minutes";
                result.CrackTimeSecondsDesc = $"{(int)(seconds / 60)} mins";
            }
            else if (seconds < 86400)
            {
                result.CrackTimeFormatted = $"{(int)(seconds / 3600)} hours";
                result.CrackTimeSecondsDesc = $"{(int)(seconds / 3600)} hrs";
            }
            else if (seconds < 2592000)
            {
                result.CrackTimeFormatted = $"{(int)(seconds / 86400)} days";
                result.CrackTimeSecondsDesc = $"{(int)(seconds / 86400)} days";
            }
            else if (seconds < 31536000)
            {
                result.CrackTimeFormatted = $"{(int)(seconds / 2592000)} months";
                result.CrackTimeSecondsDesc = $"{(int)(seconds / 2592000)} months";
            }
            else if (seconds < 31536000.0 * 100)
            {
                result.CrackTimeFormatted = $"{(int)(seconds / 31536000)} years";
                result.CrackTimeSecondsDesc = $"{(int)(seconds / 31536000)} yrs";
            }
            else if (seconds < 31536000.0 * 10000)
            {
                result.CrackTimeFormatted = $"{seconds / 31536000:N0} years";
                result.CrackTimeSecondsDesc = "Millennia";
            }
            else
            {
                result.CrackTimeFormatted = "Centuries (Unbreakable)";
                result.CrackTimeSecondsDesc = "Trillions of years";
            }
        }

        private static void GenerateSuggestions(
            PasswordAnalysisResult result,
            string password,
            bool hasUpper,
            bool hasLower,
            bool hasDigit,
            bool hasSpecial,
            bool hasStrongLength,
            bool isCommon,
            bool hasRepeated,
            bool hasSequential,
            bool hasPersonal)
        {
            if (password.Length < 12)
                result.Suggestions.Add("Increase password length to at least 12–16 characters for quantum-resistant strength.");

            if (!hasUpper)
                result.Suggestions.Add("Add at least one uppercase letter (A–Z).");

            if (!hasLower)
                result.Suggestions.Add("Add at least one lowercase letter (a–z).");

            if (!hasDigit)
                result.Suggestions.Add("Include numeric digits (0–9) to elevate entropy.");

            if (!hasSpecial)
                result.Suggestions.Add("Add special symbols (e.g., !, @, #, $, %, ^, &, *) to fortify against dictionary attacks.");

            if (hasRepeated)
                result.Suggestions.Add("Avoid repeating identical characters consecutively (e.g., 'aaa' or '111').");

            if (hasSequential)
                result.Suggestions.Add("Avoid sequential letters, numbers, or keyboard walks (e.g., 'abc', '123', 'qwerty').");

            if (isCommon)
                result.Suggestions.Add("This password appears in common breach lists! Never use common dictionary words.");

            if (hasPersonal)
                result.Suggestions.Add("Avoid using your name, username, or email parts inside your password.");

            if (result.Suggestions.Count == 0 && result.Score >= 90)
                result.Suggestions.Add("Excellent! This password meets elite cybersecurity standards.");
        }

        private static void PopulateRules(PasswordAnalysisResult result)
        {
            result.Rules = new List<RuleCheckItem>
            {
                new() { RuleName = "Minimum 8 Characters", Description = "At least 8 chars long (12+ recommended)", Passed = result.HasMinLength, Icon = "fa-ruler" },
                new() { RuleName = "Uppercase Letters", Description = "Contains uppercase letters (A–Z)", Passed = result.HasUppercase, Icon = "fa-font" },
                new() { RuleName = "Lowercase Letters", Description = "Contains lowercase letters (a–z)", Passed = result.HasLowercase, Icon = "fa-font-case" },
                new() { RuleName = "Numeric Digits", Description = "Contains numbers (0–9)", Passed = result.HasNumbers, Icon = "fa-hashtag" },
                new() { RuleName = "Special Symbols", Description = "Contains symbols (!@#$%^&*)", Passed = result.HasSpecialChars, Icon = "fa-shield-halved" },
                new() { RuleName = "No Repeated Chars", Description = "No consecutive 3+ identical characters", Passed = result.HasNoRepeatedChars, Icon = "fa-ban" },
                new() { RuleName = "No Sequential Patterns", Description = "No easy keyboard walks (123, abc, qwerty)", Passed = result.HasNoSequentialChars, Icon = "fa-arrow-trend-up" },
                new() { RuleName = "Unique & Not Common", Description = "Not present in breached passwords list", Passed = result.IsNotCommonPassword, Icon = "fa-database" },
                new() { RuleName = "No Personal Info", Description = "Does not contain your name or email", Passed = result.HasNoPersonalInfo, Icon = "fa-user-shield" }
            };
        }
    }
}
