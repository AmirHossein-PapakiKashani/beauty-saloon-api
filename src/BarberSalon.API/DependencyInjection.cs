using BarberSalon.Application.Admin.Services;
using BarberSalon.Application.Auth.Services;
using BarberSalon.Application.Booking.Services;
using BarberSalon.Application.Customers.Services;
using BarberSalon.Application.Loyalty.Services;
using BarberSalon.Application.Reviews.Services;
using BarberSalon.Application.SalonServices.Services;
using BarberSalon.Application.Staff.Services;
using BarberSalon.Application.Waitlist.Services;
using BarberSalon.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BarberSalon.API;

/// <summary>Registers API-layer composition for the dependency injection container.</summary>
public static class DependencyInjection
{
    public const string CorsPolicyName = "AllowFrontend";

    /// <summary>Adds all application and infrastructure services to the container.</summary>
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyName, policy =>
            {
                policy.WithOrigins("http://localhost:3000")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        services.AddScoped<AuthService>();
        services.AddScoped<AvailableDaysService>();
        services.AddScoped<BookingService>();
        services.AddScoped<CustomerService>();
        services.AddScoped<LoyaltyService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<ReviewService>();
        services.AddScoped<SalonServiceManager>();
        services.AddScoped<StaffService>();
        services.AddScoped<UserService>();
        services.AddScoped<WaitlistService>();
        services.AddInfrastructure(configuration);
        return services;
    }
}