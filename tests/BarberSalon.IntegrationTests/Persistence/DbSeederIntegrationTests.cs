using BarberSalon.Domain.Auth.Entities;
using BarberSalon.Domain.Auth.Enums;
using BarberSalon.Domain.Customers.Entities;
using BarberSalon.Domain.SalonServices.Entities;
using BarberSalon.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BarberSalon.IntegrationTests.Persistence;

public class DbSeederIntegrationTests
{
    private static IServiceProvider CreateInMemoryServiceProvider(string? dbName = null)
    {
        var services = new ServiceCollection();
        var uniqueName = dbName ?? ("DbSeederTest_" + Guid.NewGuid().ToString("N"));

        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(uniqueName));

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task SeedAsync_ShouldSeedAdminCustomerAndVipUsers_WithActiveOtps()
    {
        // Arrange
        var serviceProvider = CreateInMemoryServiceProvider();

        // Act
        using (var scope = serviceProvider.CreateScope())
        {
            await DbSeeder.SeedAsync(scope.ServiceProvider);
        }

        // Assert
        using (var scope = serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // 1. Admin user
            var adminUser = await db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == "09120000000");
            adminUser.Should().NotBeNull();
            adminUser!.FullName.Should().Be("مدیر سیستم");
            adminUser.Role.Should().Be(UserRole.Admin);

            // 2. Demo Customer user
            var demoUser = await db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == "09121234567");
            demoUser.Should().NotBeNull();
            demoUser!.FullName.Should().Be("علی احمدی");
            demoUser.Role.Should().Be(UserRole.Customer);

            var demoCustomer = await db.Customers.FirstOrDefaultAsync(c => c.PhoneNumber == "09121234567");
            demoCustomer.Should().NotBeNull();
            demoCustomer!.FullName.Should().Be("علی احمدی");
            demoCustomer.UserId.Should().Be(demoUser.Id);

            // 3. VIP Customer user
            var vipUser = await db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == "09121112233");
            vipUser.Should().NotBeNull();
            vipUser!.FullName.Should().Be("سارا محمدی");
            vipUser.Role.Should().Be(UserRole.Customer);

            var vipCustomer = await db.Customers.FirstOrDefaultAsync(c => c.PhoneNumber == "09121112233");
            vipCustomer.Should().NotBeNull();
            vipCustomer!.FullName.Should().Be("سارا محمدی");
            vipCustomer.UserId.Should().Be(vipUser.Id);

            // 4. Active OtpCodes
            var testPhones = new[] { "09120000000", "09121234567", "09121112233" };
            var now = DateTime.UtcNow;

            foreach (var phone in testPhones)
            {
                var otps = await db.OtpCodes.Where(o => o.PhoneNumber == phone).ToListAsync();
                otps.Should().NotBeEmpty($"Phone {phone} should have an active OTP code seeded");

                var activeOtp = otps.FirstOrDefault(o => o.IsValid(now));
                activeOtp.Should().NotBeNull($"Phone {phone} must have an active unexpired OTP");
                activeOtp!.Code.Should().Be("12345");
                activeOtp.IsUsed.Should().BeFalse();
                activeOtp.ExpiresAt.Should().BeAfter(now.AddDays(300));
            }
        }
    }

    [Fact]
    public async Task SeedAsync_ShouldBeIdempotent_WhenCalledMultipleTimes()
    {
        // Arrange
        var serviceProvider = CreateInMemoryServiceProvider();

        // Act - Call SeedAsync twice
        await DbSeeder.SeedAsync(serviceProvider);
        await DbSeeder.SeedAsync(serviceProvider);

        // Assert - No duplicates or exceptions
        using (var scope = serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var adminUsers = await db.Users.Where(u => u.PhoneNumber == "09120000000").ToListAsync();
            adminUsers.Should().HaveCount(1);

            var demoUsers = await db.Users.Where(u => u.PhoneNumber == "09121234567").ToListAsync();
            demoUsers.Should().HaveCount(1);

            var demoCustomers = await db.Customers.Where(c => c.PhoneNumber == "09121234567").ToListAsync();
            demoCustomers.Should().HaveCount(1);

            var vipUsers = await db.Users.Where(u => u.PhoneNumber == "09121112233").ToListAsync();
            vipUsers.Should().HaveCount(1);

            var vipCustomers = await db.Customers.Where(c => c.PhoneNumber == "09121112233").ToListAsync();
            vipCustomers.Should().HaveCount(1);

            var demoOtps = await db.OtpCodes.Where(o => o.PhoneNumber == "09121234567" && !o.IsUsed).ToListAsync();
            demoOtps.Should().HaveCount(1);
        }
    }

    [Fact]
    public async Task SeedAsync_ShouldSeedUsersAndOtps_EvenWhenSalonServicesAlreadyExist()
    {
        // Arrange - Pre-populate existing salon services to simulate partially-seeded / existing database
        var serviceProvider = CreateInMemoryServiceProvider();
        using (var scope = serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.SalonServices.Add(SalonService.Create("کوتاهی تستی", "توضیحات", 30, 100_000, "haircut"));
            await db.SaveChangesAsync();
        }

        // Act
        await DbSeeder.SeedAsync(serviceProvider);

        // Assert - Test users and OTPs must still be seeded
        using (var scope = serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var adminUser = await db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == "09120000000");
            adminUser.Should().NotBeNull("Admin user must be seeded even if services already exist");

            var demoUser = await db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == "09121234567");
            demoUser.Should().NotBeNull("Demo user must be seeded even if services already exist");

            var demoCustomer = await db.Customers.FirstOrDefaultAsync(c => c.PhoneNumber == "09121234567");
            demoCustomer.Should().NotBeNull("Demo customer must be seeded even if services already exist");

            var otp = await db.OtpCodes.FirstOrDefaultAsync(o => o.PhoneNumber == "09121234567" && o.Code == "12345");
            otp.Should().NotBeNull("Demo OTP must be seeded even if services already exist");
        }
    }
}
