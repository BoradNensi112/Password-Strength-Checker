using System.Security.Cryptography;
using System.Text;
using SecurePass.Models.ViewModels;

namespace SecurePass.Services
{
    public class PasswordGeneratorService : IPasswordGeneratorService
    {
        private const string LowercaseChars = "abcdefghijklmnopqrstuvwxyz";
        private const string UppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string NumberChars = "0123456789";
        private const string SymbolChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";

        private const string SimilarChars = "0OolI1|'\"";

        private readonly IPasswordAnalyzerService _analyzer;

        public PasswordGeneratorService(IPasswordAnalyzerService analyzer)
        {
            _analyzer = analyzer;
        }

        public GeneratedPasswordResult Generate(GeneratorOptions options)
        {
            int length = Math.Clamp(options.Length, 8, 64);

            var lower = options.AvoidSimilarChars
                ? new string(LowercaseChars.Where(c => !SimilarChars.Contains(c)).ToArray())
                : LowercaseChars;

            var upper = options.AvoidSimilarChars
                ? new string(UppercaseChars.Where(c => !SimilarChars.Contains(c)).ToArray())
                : UppercaseChars;

            var numbers = options.AvoidSimilarChars
                ? new string(NumberChars.Where(c => !SimilarChars.Contains(c)).ToArray())
                : NumberChars;

            var symbols = options.AvoidSimilarChars
                ? new string(SymbolChars.Where(c => !SimilarChars.Contains(c)).ToArray())
                : SymbolChars;

            var pools = new List<string>();
            var passwordChars = new List<char>();

            if (options.IncludeLowercase && lower.Length > 0)
            {
                pools.Add(lower);
                passwordChars.Add(GetRandomChar(lower));
            }

            if (options.IncludeUppercase && upper.Length > 0)
            {
                pools.Add(upper);
                passwordChars.Add(GetRandomChar(upper));
            }

            if (options.IncludeNumbers && numbers.Length > 0)
            {
                pools.Add(numbers);
                passwordChars.Add(GetRandomChar(numbers));
            }

            if (options.IncludeSymbols && symbols.Length > 0)
            {
                pools.Add(symbols);
                passwordChars.Add(GetRandomChar(symbols));
            }

            if (pools.Count == 0)
            {
                pools.Add(lower);
                passwordChars.Add(GetRandomChar(lower));
            }

            var combinedPool = string.Concat(pools);

            // Fill remaining characters
            while (passwordChars.Count < length)
            {
                passwordChars.Add(GetRandomChar(combinedPool));
            }

            // Cryptographically secure Fisher-Yates shuffle
            for (int i = passwordChars.Count - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);
                (passwordChars[i], passwordChars[j]) = (passwordChars[j], passwordChars[i]);
            }

            string password = new string(passwordChars.ToArray());
            var analysis = _analyzer.Analyze(password);

            return new GeneratedPasswordResult
            {
                Password = password,
                Score = analysis.Score,
                StrengthLevel = analysis.StrengthLevel,
                CrackTimeFormatted = analysis.CrackTimeFormatted,
                EntropyBits = analysis.EntropyBits
            };
        }

        private static char GetRandomChar(string charSet)
        {
            int index = RandomNumberGenerator.GetInt32(charSet.Length);
            return charSet[index];
        }
    }
}
