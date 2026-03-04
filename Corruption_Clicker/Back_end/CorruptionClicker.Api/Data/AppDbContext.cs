using Microsoft.EntityFrameworkCore;

namespace CorruptionClicker.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Upgrade> Upgrades => Set<Upgrade>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserUpgrade> UserUpgrades => Set<UserUpgrade>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Upgrade>().ToTable("upgrades");
        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<UserUpgrade>().ToTable("user_upgrades");
    }
}