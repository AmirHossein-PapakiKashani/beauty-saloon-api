using BarberSalon.Application.Common.Interfaces;
using BarberSalon.Domain.Auth.Entities;
using BarberSalon.Domain.Booking.Entities;
using BarberSalon.Domain.Customers.Entities;
using BarberSalon.Domain.Loyalty.Entities;
using BarberSalon.Domain.Portfolio.Entities;
using BarberSalon.Domain.Reviews.Entities;
using BarberSalon.Domain.SalonServices.Entities;
using BarberSalon.Domain.Staff.Entities;
using BarberSalon.Domain.Waitlist.Entities;
using Microsoft.EntityFrameworkCore;

namespace BarberSalon.Infrastructure.Persistence;

/// <summary>
/// Application database context for EF Core.
/// </summary>
public class AppDbContext : DbContext, IUnitOfWork
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    /// <summary>Appointments table.</summary>
    public DbSet<Appointment> Appointments => Set<Appointment>();

    /// <summary>Customers table.</summary>
    public DbSet<Customer> Customers => Set<Customer>();

    /// <summary>OTP codes table.</summary>
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();

    /// <summary>Users table.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Salon services table.</summary>
    public DbSet<SalonService> SalonServices => Set<SalonService>();

    /// <summary>Staff members table.</summary>
    public DbSet<StaffMember> StaffMembers => Set<StaffMember>();

    /// <summary>Reviews table.</summary>
    public DbSet<Review> Reviews => Set<Review>();

    /// <summary>Waitlist table.</summary>
    public DbSet<WaitlistEntry> WaitlistEntries => Set<WaitlistEntry>();

    /// <summary>Loyalty accounts table.</summary>
    public DbSet<LoyaltyAccount> LoyaltyAccounts => Set<LoyaltyAccount>();

    /// <summary>Referrals table.</summary>
    public DbSet<Referral> Referrals => Set<Referral>();

    /// <summary>Portfolio items table.</summary>
    public DbSet<PortfolioItem> PortfolioItems => Set<PortfolioItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
