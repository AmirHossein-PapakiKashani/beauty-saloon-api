namespace BarberSalon.Domain.BeautyProfile;

public sealed class BeautyHistoryEntry
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string ServiceName { get; private set; } = string.Empty;
    public string StaffName { get; private set; } = string.Empty;
    public string Date { get; private set; } = string.Empty;
    public string Formula { get; private set; } = string.Empty;
    public string Notes { get; private set; } = string.Empty;
    public string? PhotoUrl { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private BeautyHistoryEntry() { }

    public static BeautyHistoryEntry Create(
        Guid customerId,
        string serviceName,
        string staffName,
        string date,
        string formula,
        string notes,
        string? photoUrl = null)
    {
        return new BeautyHistoryEntry
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            ServiceName = serviceName?.Trim() ?? string.Empty,
            StaffName = staffName?.Trim() ?? string.Empty,
            Date = date?.Trim() ?? string.Empty,
            Formula = formula?.Trim() ?? string.Empty,
            Notes = notes?.Trim() ?? string.Empty,
            PhotoUrl = photoUrl,
            CreatedAt = DateTime.UtcNow
        };
    }
}
