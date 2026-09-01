using Microsoft.EntityFrameworkCore;
using SecurePass.Models;

namespace SecurePass.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<PasswordVault> PasswordVaults => Set<PasswordVault>();
        public DbSet<UserSetting> UserSettings => Set<UserSetting>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User Email Unique Index
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // PasswordVault relationships
            modelBuilder.Entity<PasswordVault>()
                .HasOne(p => p.User)
                .WithMany(u => u.Passwords)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PasswordVault>()
                .HasIndex(p => new { p.UserId, p.Title });

            // UserSetting relationship
            modelBuilder.Entity<UserSetting>()
                .HasOne(s => s.User)
                .WithOne(u => u.Settings)
                .HasForeignKey<UserSetting>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
