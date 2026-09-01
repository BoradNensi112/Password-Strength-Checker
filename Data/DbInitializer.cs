using BCrypt.Net;
using SecurePass.Models;
using SecurePass.Services;

namespace SecurePass.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context, IEncryptionService encryptionService, IPasswordAnalyzerService analyzer)
        {
            context.Database.EnsureCreated();

            if (context.Users.Any())
            {
                return; // DB has already been seeded
            }

            // Seed Demo User
            var demoUser = new User
            {
                FullName = "Cyber Security Admin",
                Email = "demo@securepass.io",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Secure@Pass2026!"),
                Avatar = "shield-user",
                CreatedAt = DateTime.UtcNow.AddMonths(-4),
                LastLoginAt = DateTime.UtcNow
            };

            context.Users.Add(demoUser);
            context.SaveChanges();

            // Seed Default Settings
            var userSetting = new UserSetting
            {
                UserId = demoUser.Id,
                ThemeMode = "dark",
                AutoLockMinutes = 15,
                DefaultGeneratorLength = 16,
                IncludeUppercase = true,
                IncludeLowercase = true,
                IncludeNumbers = true,
                IncludeSymbols = true,
                AvoidSimilarChars = true
            };
            context.UserSettings.Add(userSetting);

            // Seed Sample Passwords for Demo
            var samplePasswords = new List<(string Title, string? Website, string? Username, string PlainPassword, string Category, string? Notes, bool IsFav, int DaysAgo)>
            {
                ("SBI Net Banking", "https://onlinesbi.sbi", "sbi_admin_99", "K9#xP!vL#91", "Banking", "Primary corporate banking credentials. 2FA enabled via token.", true, 12),
                ("GitHub Account", "https://github.com", "dev-master", "G!tHub#DevOps!Secured", "Work", "Main organizational repo access. SSH keys configured.", true, 20),
                ("Instagram Personal", "https://instagram.com", "cyber_warrior", "InstaPass@2024", "Social", "Personal social media profile.", false, 45),
                ("Gmail College", "https://mail.google.com", "student@university.edu", "Col1ege#Edu2025!", "Education", "University student portal & email.", true, 18),
                ("College ERP", "https://erp.university.edu", "2024CS1092", "SimplePass123", "Education", "Semester exam grading portal. Needs password upgrade.", false, 95),
                ("Amazon Prime Shopping", "https://amazon.com", "buyer@gmail.com", "SimplePass123", "Shopping", "Family shopping account (reused password test).", false, 92),
                ("Netflix Streaming", "https://netflix.com", "family@home.net", "Flix#Watcher2026$", "Entertainment", "Ultra HD 4K family profile PIN 4421.", false, 35),
                ("AWS Production Root", "https://aws.amazon.com", "root-ops@enterprise.cloud", "Aws#Vault!99X#mQ", "Work", "AWS cloud root account. Protected with physical YubiKey.", true, 110)
            };

            foreach (var item in samplePasswords)
            {
                var analysis = analyzer.Analyze(item.PlainPassword);
                var entry = new PasswordVault
                {
                    UserId = demoUser.Id,
                    Title = item.Title,
                    Website = item.Website,
                    Username = item.Username,
                    EncryptedPassword = encryptionService.Encrypt(item.PlainPassword),
                    PasswordStrength = analysis.StrengthLevel,
                    StrengthScore = analysis.Score,
                    Category = item.Category,
                    Notes = item.Notes,
                    IsFavorite = item.IsFav,
                    CreatedAt = DateTime.UtcNow.AddDays(-item.DaysAgo),
                    UpdatedAt = DateTime.UtcNow.AddDays(-item.DaysAgo)
                };
                context.PasswordVaults.Add(entry);
            }

            context.SaveChanges();
        }
    }
}
