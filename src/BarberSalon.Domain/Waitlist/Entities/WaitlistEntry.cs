using BarberSalon.Domain.Common;

namespace BarberSalon.Domain.Waitlist.Entities;

public sealed class WaitlistEntry : BaseEntity
{
    private WaitlistEntry() { }

    public Guid CustomerId { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public string CustomerPhone { get; private set; } = string.Empty;
    public Guid StaffId { get; private set; }
    public string StaffName { get; private set; } = string.Empty;
    public Guid ServiceId { get; private set; }
    public string ServiceName { get; private set; } = string.Empty;
    public string Date { get; private set; } = string.Empty;
    public string Time { get; private set; } = string.Empty;
    public string Status { get; private set; } = "waiting"; // waiting | notified | claimed | expired | cancelled
    public int QueuePosition { get; private set; } = 1;
    public DateTime? NotifiedAt { get; private set; }

    public static WaitlistEntry Create(
        Guid customerId,
        string customerName,
        string customerPhone,
        Guid staffId,
        string staffName,
        Guid serviceId,
        string serviceName,
        string date,
        string time,
        int queuePosition = 1)
    {
        return new WaitlistEntry
        {
            CustomerId = customerId,
            CustomerName = (customerName ?? string.Empty).Trim(),
            CustomerPhone = (customerPhone ?? string.Empty).Trim(),
            StaffId = staffId,
            StaffName = (staffName ?? string.Empty).Trim(),
            ServiceId = serviceId,
            ServiceName = (serviceName ?? string.Empty).Trim(),
            Date = (date ?? string.Empty).Trim(),
            Time = (time ?? string.Empty).Trim(),
            Status = "waiting",
            QueuePosition = queuePosition
        };
    }

    public void Notify()
    {
        Status = "notified";
        NotifiedAt = DateTime.UtcNow;
        Touch();
    }

    public void Cancel()
    {
        Status = "cancelled";
        Touch();
    }

    public void Claim()
    {
        Status = "claimed";
        Touch();
    }

    public void SetQueuePosition(int position)
    {
        QueuePosition = position;
        Touch();
    }
}
