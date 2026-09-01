namespace SecurePass.Models.ViewModels
{
    public class PasswordAnalysisResult
    {
        public int Score { get; set; }
        public string StrengthLevel { get; set; } = "Weak"; // Weak, Medium, Strong, Very Strong
        public string ColorHex { get; set; } = "#EF4444";
        public double EntropyBits { get; set; }
        public string CrackTimeFormatted { get; set; } = "Instant";
        public string CrackTimeSecondsDesc { get; set; } = "0 seconds";
        public int Length { get; set; }
        public int CharacterSetSize { get; set; }

        // Checklist checks
        public bool HasMinLength { get; set; }
        public bool HasUppercase { get; set; }
        public bool HasLowercase { get; set; }
        public bool HasNumbers { get; set; }
        public bool HasSpecialChars { get; set; }
        public bool HasNoRepeatedChars { get; set; }
        public bool HasNoSequentialChars { get; set; }
        public bool IsNotCommonPassword { get; set; }
        public bool HasNoPersonalInfo { get; set; }

        public List<string> Suggestions { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<RuleCheckItem> Rules { get; set; } = new();
    }

    public class RuleCheckItem
    {
        public string RuleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public string Icon { get; set; } = string.Empty;
    }

    public class PasswordCheckRequest
    {
        public string Password { get; set; } = string.Empty;
        public string? UserInfo { get; set; } // Name or email to check against
    }
}
