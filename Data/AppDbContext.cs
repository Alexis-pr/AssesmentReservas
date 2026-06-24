using AssesmentReservas.API.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AssesmentReservas.API.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Property> Properties => Set<Property>();
    public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<KycVerification> KycVerifications => Set<KycVerification>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Property>(e =>
        {
            e.Property(p => p.PricePerNight).HasPrecision(12, 2);
            e.HasOne(p => p.Owner)
                .WithMany(u => u.Properties)
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(p => p.City);
            e.HasIndex(p => p.IsActive);
        });

        builder.Entity<PropertyImage>(e =>
        {
            e.HasOne(i => i.Property)
                .WithMany(p => p.Images)
                .HasForeignKey(i => i.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Booking>(e =>
        {
            e.Property(b => b.PricePerNight).HasPrecision(12, 2);
            e.Property(b => b.TotalPrice).HasPrecision(12, 2);
            e.Ignore(b => b.Nights);

            e.HasOne(b => b.Property)
                .WithMany(p => p.Bookings)
                .HasForeignKey(b => b.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(b => b.Guest)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.GuestId)
                .OnDelete(DeleteBehavior.Restrict);

            // Índice para acelerar la detección de solapamientos (anti double-booking).
            e.HasIndex(b => new { b.PropertyId, b.CheckInDate, b.CheckOutDate });
            e.HasIndex(b => b.Status);
        });

        builder.Entity<Favorite>(e =>
        {
            e.HasOne(f => f.User)
                .WithMany(u => u.Favorites)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(f => f.Property)
                .WithMany()
                .HasForeignKey(f => f.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Un inmueble solo puede estar una vez en la wishlist de un usuario.
            e.HasIndex(f => new { f.UserId, f.PropertyId }).IsUnique();
        });

        builder.Entity<KycVerification>(e =>
        {
            e.HasOne(k => k.User)
                .WithMany()
                .HasForeignKey(k => k.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(k => k.UserId);
        });

        builder.Entity<Notification>(e =>
        {
            e.HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(n => new { n.UserId, n.IsRead });
        });
    }
}
