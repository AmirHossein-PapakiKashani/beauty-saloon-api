using BarberSalon.Application.Booking.Services;
using BarberSalon.Application.Common.Exceptions;
using BarberSalon.Application.Common.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace BarberSalon.Application.Tests;

public class AvailableDaysServiceTests
{
    private const string FixedToday = "2026-09-01";

    private readonly IClock _clock;

    public AvailableDaysServiceTests()
    {
        _clock = Substitute.For<IClock>();
        _clock.UtcNow.Returns(new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task GetAvailableDaysAsync_WhenCountGiven_ReturnsExactlyCountConsecutiveDaysAscending()
    {
        var service = new AvailableDaysService(_clock);

        var result = await service.GetAvailableDaysAsync(3);

        result.Should().HaveCount(3);
        result[0].FullDate.Should().Be("2026-09-01");
        result[1].FullDate.Should().Be("2026-09-02");
        result[2].FullDate.Should().Be("2026-09-03");
        result.Select(d => DateOnly.Parse(d.FullDate)).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetAvailableDaysAsync_WhenCountOmitted_ReturnsDefaultSevenDays()
    {
        var service = new AvailableDaysService(_clock);

        var result = await service.GetAvailableDaysAsync();

        result.Should().HaveCount(AvailableDaysService.DefaultCount).And.HaveCount(7);
    }

    [Fact]
    public async Task GetAvailableDaysAsync_ExposesDateDayNameAndFullDateForEachDay()
    {
        var service = new AvailableDaysService(_clock);

        var result = await service.GetAvailableDaysAsync(1);

        var day = result.Single();
        day.Date.Should().Be("1");
        day.DayName.Should().NotBeNullOrWhiteSpace();
        day.FullDate.Should().Be(FixedToday);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetAvailableDaysAsync_WhenCountBelowOne_ThrowsValidationException(int count)
    {
        var service = new AvailableDaysService(_clock);

        var act = () => service.GetAvailableDaysAsync(count);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task GetAvailableDaysAsync_WhenCountExceedsMax_ThrowsValidationException()
    {
        var service = new AvailableDaysService(_clock);

        var act = () => service.GetAvailableDaysAsync(AvailableDaysService.MaxCount + 1);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task GetAvailableDaysAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        var service = new AvailableDaysService(_clock);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => service.GetAvailableDaysAsync(10, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}