using BarberSalon.Application.Auth.Interfaces;
using BarberSalon.Application.Booking.Interfaces;
using BarberSalon.Application.Common.Interfaces;
using BarberSalon.Application.Customers.Interfaces;
using BarberSalon.Application.Loyalty.Interfaces;
using BarberSalon.Application.Portfolio.Interfaces;
using BarberSalon.Application.Reminders.Interfaces;
using BarberSalon.Application.Reviews.Interfaces;
using BarberSalon.Application.SalonServices.Interfaces;
using BarberSalon.Application.Staff.Interfaces;
using BarberSalon.Application.Waitlist.Interfaces;
using BarberSalon.Infrastructure.Auth;
using BarberSalon.Infrastructure.ExternalServices;
using BarberSalon.Infrastructure.Persistence;
using BarberSalon.Infrastructure.Repositories;
using BarberSalon.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BarberSalon.Infrastructure;

/// <summary>Registers infrastructure-layer services with the dependency injection container.</summary>
public static class DependencyInjection
{
    /// <summary>Adds infrastructure-layer implementations to the service collection.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IOtpGenerator, RandomOtpGenerator>();
        services.AddScoped<ISmsService, SmsService>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var config = configuration ?? sp.GetService<IConfiguration>();
            var connectionString = config?.GetConnectionString("DefaultConnection")
                ?? "Host=localhost;Database=barber_salon;Username=postgres;Password=postgres";
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<ISalonServiceRepository, SalonServiceRepository>();
        services.AddScoped<IStaffRepository, StaffRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IOtpCodeRepository, OtpCodeRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IWaitlistRepository, WaitlistRepository>();
        services.AddScoped<ILoyaltyRepository, LoyaltyRepository>();
        services.AddScoped<IPortfolioRepository, PortfolioRepository>();
        services.AddScoped<IReminderRepository, ReminderRepository>();

        return services;
    }
}