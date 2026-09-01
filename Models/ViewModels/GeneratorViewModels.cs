namespace SecurePass.Models.ViewModels
{
    public class GeneratorOptions
    {
        public int Length { get; set; } = 16;
        public bool IncludeUppercase { get; set; } = true;
        public bool IncludeLowercase { get; set; } = true;
        public bool IncludeNumbers { get; set; } = true;
        public bool IncludeSymbols { get; set; } = true;
        public bool AvoidSimilarChars { get; set; } = true; // Avoid 0, O, o, 1, l, I, |
    }

    public class GeneratedPasswordResult
    {
        public string Password { get; set; } = string.Empty;
        public int Score { get; set; }
        public string StrengthLevel { get; set; } = "Strong";
        public string CrackTimeFormatted { get; set; } = "Centuries";
        public double EntropyBits { get; set; }
    }
}
