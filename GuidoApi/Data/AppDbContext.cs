using GoGuido.Models;
using Microsoft.EntityFrameworkCore;

namespace GoGuido.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<Monument> Monuments => Set<Monument>();
    public DbSet<GuideProfile> GuideProfiles => Set<GuideProfile>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>().HasIndex(u => u.Email).IsUnique();
        b.Entity<GuideProfile>().HasIndex(g => g.UserId).IsUnique();
        b.Entity<Review>().HasIndex(r => r.BookingId).IsUnique();
        b.Entity<GuideProfile>().Property(p => p.PricePerHour).HasPrecision(10, 2);
        b.Entity<Booking>().Property(p => p.TotalPrice).HasPrecision(12, 2);
        b.Entity<Monument>().Property(p => p.EntryFeeInr).HasPrecision(10, 2);
        // SQL Server forbids multiple cascade paths -> restrict everywhere
        foreach (var fk in b.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }
}
