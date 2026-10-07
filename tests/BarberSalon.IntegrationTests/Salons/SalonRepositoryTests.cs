using BarberSalon.Domain.Salons.Entities;
using BarberSalon.Infrastructure.Persistence;
using BarberSalon.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BarberSalon.IntegrationTests.Salons;

public class SalonRepositoryTests
{
    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task AddAndGetByIdAsync_ShouldPersistAndRetrieveSalon()
    {
        var context = CreateContext();
        var repo = new SalonRepository(context);
        var salon = Salon.Create("Elite Barber", "elite-barber", "0219999", "Tehran", DateTime.UtcNow);

        await repo.AddAsync(salon);
        var fetched = await repo.GetByIdAsync(salon.Id);

        fetched.Should().NotBeNull();
        fetched!.Name.Should().Be("Elite Barber");
        fetched.Slug.Should().Be("elite-barber");
    }

    [Fact]
    public async Task SlugExistsAsync_ShouldDetectDuplicateSlug()
    {
        var context = CreateContext();
        var repo = new SalonRepository(context);
        var salon = Salon.Create("Elite Barber", "elite-barber", "0219999", "Tehran", DateTime.UtcNow);
        await repo.AddAsync(salon);

        var exists = await repo.SlugExistsAsync("elite-barber");
        var otherExists = await repo.SlugExistsAsync("other-slug");

        exists.Should().BeTrue();
        otherExists.Should().BeFalse();
    }
}
