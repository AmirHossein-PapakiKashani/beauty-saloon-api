using BarberSalon.Domain.Common;

namespace BarberSalon.Domain.Reviews.Entities;

/// <summary>
/// Domain entity representing a customer review for a salon service and/or staff member.
/// </summary>
public sealed class Review : BaseEntity
{
    private Review() { }

    public Guid? AppointmentId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public Guid? StaffId { get; private set; }
    public string StaffName { get; private set; } = string.Empty;
    public Guid? ServiceId { get; private set; }
    public string ServiceName { get; private set; } = string.Empty;
    public int Rating { get; private set; } // 1-5
    public string Comment { get; private set; } = string.Empty;
    public string Status { get; private set; } = "published"; // "published" | "hidden"

    public static Review Create(
        Guid customerId,
        string customerName,
        int rating,
        string comment,
        Guid? staffId = null,
        string? staffName = null,
        Guid? serviceId = null,
        string? serviceName = null,
        Guid? appointmentId = null)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5.");

        return new Review
        {
            CustomerId = customerId,
            CustomerName = (customerName ?? string.Empty).Trim(),
            Rating = rating,
            Comment = (comment ?? string.Empty).Trim(),
            StaffId = staffId,
            StaffName = (staffName ?? string.Empty).Trim(),
            ServiceId = serviceId,
            ServiceName = (serviceName ?? string.Empty).Trim(),
            AppointmentId = appointmentId,
            Status = "published"
        };
    }

    public void UpdateStatus(string status)
    {
        Status = status?.Trim().ToLowerInvariant() == "hidden" ? "hidden" : "published";
        Touch();
    }
}
