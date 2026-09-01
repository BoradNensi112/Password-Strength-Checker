namespace SecurePass.Models.ViewModels
{
    public class PasswordHealthViewModel
    {
        public int OverallHealthPercentage { get; set; }
        public string HealthStatus { get; set; } = "Good";
        public string HealthStatusColor { get; set; } = "#22C55E";

        public int TotalPasswords { get; set; }
        public int WeakCount { get; set; }
        public int MediumCount { get; set; }
        public int StrongCount { get; set; }
        public int DuplicateCount { get; set; }
        public int ExpiredCount { get; set; }

        public List<PasswordVaultItemViewModel> WeakPasswords { get; set; } = new();
        public List<DuplicateGroupViewModel> DuplicateGroups { get; set; } = new();
        public List<PasswordVaultItemViewModel> ExpiredPasswords { get; set; } = new();
        public List<string> ActionableRecommendations { get; set; } = new();
    }

    public class DuplicateGroupViewModel
    {
        public string MaskedPassword { get; set; } = string.Empty;
        public int Count { get; set; }
        public List<PasswordVaultItemViewModel> Items { get; set; } = new();
    }
}
