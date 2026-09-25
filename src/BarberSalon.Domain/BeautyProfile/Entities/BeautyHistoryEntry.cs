using BarberSalon.Domain.Common;

namespace BarberSalon.Domain.BeautyProfile.Entities;

public sealed class BeautyHistoryEntry : BaseEntity
{
    public Guid CustomerId { get; private set; }
    public string ServiceName { get; private set; } = string.Empty;
    public string StaffName { get; private set; } = string.Empty;
    public string Date { get; private set; } = string.Empty;
    public string Formula { get; private set; } = string.Empty;
    public string Notes { get; private set; } = string.Empty;
    public string? PhotoUrl { get; private set; }

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
            CustomerId = customerId,
            ServiceName = serviceName?.Trim() ?? string.Empty,
            StaffName = staffName?.Trim() ?? string.Empty,
            Date = date?.Trim() ?? string.Empty,
            Formula = formula?.Trim() ?? string.Empty,
            Notes = notes?.Trim() ?? string.Empty,
            PhotoUrl = photoUrl
        };
    }
}
