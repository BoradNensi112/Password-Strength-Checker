using System.ComponentModel.DataAnnotations;

namespace SecurePass.Models.ViewModels
{
    public class PasswordVaultCreateEditModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Password title is required")]
        [StringLength(150, ErrorMessage = "Title cannot exceed 150 characters")]
        [Display(Name = "Password Title")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Website / App URL")]
        public string? Website { get; set; }

        [Display(Name = "Username or Email")]
        public string? Username { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm Password")]
        public string? ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Category is required")]
        public string Category { get; set; } = "Personal";

        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
        public string? Notes { get; set; }

        public bool IsFavorite { get; set; } = false;

        public int StrengthScore { get; set; }
        public string PasswordStrength { get; set; } = "Weak";
    }

    public class PasswordVaultItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Website { get; set; }
        public string? Username { get; set; }
        public string PasswordStrength { get; set; } = "Weak";
        public int StrengthScore { get; set; }
        public string Category { get; set; } = "Others";
        public string? Notes { get; set; }
        public bool IsFavorite { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsExpired { get; set; }
        public int AgeInDays { get; set; }
    }

    public class VaultIndexViewModel
    {
        public List<PasswordVaultItemViewModel> Items { get; set; } = new();
        public string? SearchQuery { get; set; }
        public string? CategoryFilter { get; set; }
        public string? StrengthFilter { get; set; }
        public string? SortBy { get; set; } = "latest";
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalItems { get; set; } = 0;
        public int PageSize { get; set; } = 10;
        public List<string> Categories { get; set; } = new();
    }

    public class PasswordRevealResponse
    {
        public bool Success { get; set; }
        public string? Password { get; set; }
        public string? Message { get; set; }
    }

    public class DuplicateCheckRequest
    {
        public string Password { get; set; } = string.Empty;
        public int? CurrentId { get; set; }
    }

    public class DuplicateCheckResponse
    {
        public bool IsDuplicate { get; set; }
        public List<string> DuplicateTitles { get; set; } = new();
        public string? Message { get; set; }
    }
}
