using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecurePass.Models
{
    public class PasswordVault
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Website { get; set; }

        [MaxLength(150)]
        public string? Username { get; set; }

        [Required]
        public string EncryptedPassword { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string PasswordStrength { get; set; } = "Weak";

        public int StrengthScore { get; set; } = 0;

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = "Others";

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public bool IsFavorite { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public bool IsExpired => (DateTime.UtcNow - CreatedAt).TotalDays > 90;

        [NotMapped]
        public int AgeInDays => (int)(DateTime.UtcNow - CreatedAt).TotalDays;
    }
}
