using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecurePass.Models
{
    public class UserSetting
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }

        [MaxLength(20)]
        public string ThemeMode { get; set; } = "dark";

        public int AutoLockMinutes { get; set; } = 15;

        public int DefaultGeneratorLength { get; set; } = 16;

        public bool IncludeSymbols { get; set; } = true;

        public bool IncludeNumbers { get; set; } = true;

        public bool IncludeUppercase { get; set; } = true;

        public bool IncludeLowercase { get; set; } = true;

        public bool AvoidSimilarChars { get; set; } = true;
    }
}
