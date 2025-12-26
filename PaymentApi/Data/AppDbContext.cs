using Microsoft.EntityFrameworkCore;

namespace PaymentApi.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<RevokedToken> RevokedTokens => Set<RevokedToken>();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(e =>
            {
                e.HasIndex(u => u.Username).IsUnique(); 
                e.Property(u => u.Balance).HasColumnType("decimal(18,2)"); 
                e.Property(u => u.RowVersion).IsRowVersion(); 
            });

            modelBuilder.Entity<Payment>(e =>
            {
                e.Property(p => p.Amount).HasColumnType("decimal(18,2)");
                e.HasOne<User>().WithMany().HasForeignKey(p => p.UserId);
            });

            modelBuilder.Entity<RevokedToken>(e =>
            {
                e.HasIndex(r => r.Jti).IsUnique(); 
            });
        }
    }

    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = default!;
        public string PasswordHash { get; set; } = default!;
        public decimal Balance { get; set; } = 8.00m;
        public DateTime? LockoutUntil { get; set; } 
        public int FailedLoginCount { get; set; } = 0;
        public byte[]? RowVersion { get; set; } 
    }

    public class Payment
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; } = 1.10m;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class RevokedToken
    {
        public int Id { get; set; }
        public string Jti { get; set; } = default!;
        public DateTime ExpiresAt { get; set; }
        public DateTime RevokedAt { get; set; } = DateTime.UtcNow;
    }
}
