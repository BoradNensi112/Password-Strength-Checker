namespace SecurePass.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalPasswords { get; set; }
        public int StrongPasswords { get; set; }
        public int VeryStrongPasswords { get; set; }
        public int MediumPasswords { get; set; }
        public int WeakPasswords { get; set; }
        public int AverageStrengthScore { get; set; }
        public int HealthScore { get; set; }
        public int ExpiredPasswordsCount { get; set; }
        public int DuplicatePasswordsCount { get; set; }
        public int FavoriteCount { get; set; }

        public List<PasswordVaultItemViewModel> RecentPasswords { get; set; } = new();
        public Dictionary<string, int> CategoryCounts { get; set; } = new();
        public List<StrengthHistoryPoint> WeeklyActivity { get; set; } = new();
    }

    public class StrengthHistoryPoint
    {
        public string Day { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
