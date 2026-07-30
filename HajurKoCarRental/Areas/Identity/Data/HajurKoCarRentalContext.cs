using HajurKoCarRental.Areas.Identity.Data;
using HajurKoCarRental.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HajurKoCarRental.Data;

public class HajurKoCarRentalContext : IdentityDbContext<HajurKoCarRentalUser>
{
    public HajurKoCarRentalContext(DbContextOptions<HajurKoCarRentalContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Customize the ASP.NET Identity model and override the defaults if needed.
        // For example, you can rename the ASP.NET Identity table names and more.
        // Add your customizations after calling base.OnModelCreating(builder);

        builder.Entity<CarBranch>()
            .HasOne(cb => cb.Car)
            .WithMany(c => c.CarBranches)
            .HasForeignKey(cb => cb.CarId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CarBranch>()
            .HasOne(cb => cb.Branch)
            .WithMany(b => b.CarBranches)
            .HasForeignKey(cb => cb.BranchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CarBranch>()
            .HasIndex(cb => new { cb.CarId, cb.BranchId })
            .IsUnique();

        builder.Entity<OfferBranch>()
            .HasOne(ob => ob.Offer)
            .WithMany(o => o.OfferBranches)
            .HasForeignKey(ob => ob.OfferId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<OfferBranch>()
            .HasOne(ob => ob.Branch)
            .WithMany(b => b.OfferBranches)
            .HasForeignKey(ob => ob.BranchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<OfferBranch>()
            .HasIndex(ob => new { ob.OfferId, ob.BranchId })
            .IsUnique();

        builder.Entity<Rent>()
            .HasOne(r => r.PickupBranch)
            .WithMany()
            .HasForeignKey(r => r.PickupBranchId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Rent>()
            .HasOne(r => r.DropOffBranch)
            .WithMany()
            .HasForeignKey(r => r.DropOffBranchId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<RentalHistory>()
            .HasOne(h => h.User)
            .WithMany()
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<AuditLog>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    public DbSet<HajurKoCarRental.Models.Car>? Car { get; set; }
    public DbSet<HajurKoCarRental.Models.CarBrand>? CarBrand { get; set; }
    public DbSet<HajurKoCarRental.Models.Payment>? Payment { get; set; }
    public DbSet<HajurKoCarRental.Models.Rent>? Rent { get; set; }
    public DbSet<HajurKoCarRental.Models.Offer>? Offer { get; set; }
    public DbSet<HajurKoCarRental.Models.RentOffer>? RentOffer { get; set; }
    public DbSet<HajurKoCarRental.Models.CarDamage>? CarDamage { get; set; }
    public DbSet<HajurKoCarRental.Models.RefundableRent>? RefundableRent { get; set; }
    public DbSet<HajurKoCarRental.Models.RentalHistory>? RentalHistory { get; set; }
    public DbSet<HajurKoCarRental.Models.Festival>? Festivals { get; set; }
    public DbSet<HajurKoCarRental.Models.RetentionOffer>? RetentionOffer { get; set; }
    public DbSet<HajurKoCarRental.Models.ChatMessage>? ChatMessage { get; set; }
    public DbSet<HajurKoCarRental.Models.PendingRegistration>? PendingRegistration { get; set; }
    public DbSet<HajurKoCarRental.Models.Branch>? Branches { get; set; }
    public DbSet<HajurKoCarRental.Models.CarBranch>? CarBranches { get; set; }
    public DbSet<HajurKoCarRental.Models.OfferBranch>? OfferBranches { get; set; }
    public DbSet<HajurKoCarRental.Models.AuditLog>? AuditLogs { get; set; }
    public DbSet<HajurKoCarRental.Models.LateFee>? LateFees { get; set; }
    public DbSet<HajurKoCarRental.Models.CarTracking>? CarTrackings { get; set; }
    public DbSet<HajurKoCarRental.Models.TrackingPoint>? TrackingPoints { get; set; }
}
