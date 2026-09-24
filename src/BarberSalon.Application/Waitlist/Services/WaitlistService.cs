using BarberSalon.Application.Auth.Interfaces;
using BarberSalon.Application.Common.Interfaces;
using BarberSalon.Application.Customers.Interfaces;
using BarberSalon.Application.SalonServices.Interfaces;
using BarberSalon.Application.Staff.Interfaces;
using BarberSalon.Application.Waitlist.DTOs;
using BarberSalon.Application.Waitlist.Interfaces;
using BarberSalon.Domain.Waitlist.Entities;

namespace BarberSalon.Application.Waitlist.Services;

public sealed class WaitlistService(
    IWaitlistRepository waitlistRepository,
    IUnitOfWork unitOfWork,
    ICustomerRepository customerRepository,
    IStaffRepository staffRepository,
    ISalonServiceRepository salonServiceRepository,
    IUserRepository userRepository)
{
    public async Task<List<WaitlistEntryDto>> GetAllAsync(
        string? status = null,
        Guid? staffId = null,
        string? date = null,
        CancellationToken ct = default)
    {
        var list = await waitlistRepository.GetAllAsync(status, staffId, date, ct);
        return list.Select(Map).ToList();
    }

    public async Task<WaitlistEntryDto> JoinAsync(JoinWaitlistRequest req, CancellationToken ct = default)
    {
        var customerName = req.CustomerName;
        var customerPhone = req.CustomerPhone;
        if (string.IsNullOrWhiteSpace(customerName) || string.IsNullOrWhiteSpace(customerPhone))
        {
            var customer = await customerRepository.GetByIdAsync(req.CustomerId, ct);
            if (customer is not null)
            {
                customerName ??= customer.FullName;
                customerPhone ??= customer.PhoneNumber;
            }
            else
            {
                var user = await userRepository.GetByIdAsync(req.CustomerId, ct);
                if (user is not null)
                {
                    customerName ??= user.FullName;
                    customerPhone ??= user.PhoneNumber;
                }
            }
        }

        var staffName = req.StaffName;
        if (string.IsNullOrWhiteSpace(staffName))
        {
            var staff = await staffRepository.GetByIdAsync(req.StaffId, ct);
            staffName = staff?.FullName ?? "";
        }

        var serviceName = req.ServiceName;
        if (string.IsNullOrWhiteSpace(serviceName))
        {
            var service = await salonServiceRepository.GetByIdAsync(req.ServiceId, ct);
            serviceName = service?.Name ?? "";
        }

        var count = await waitlistRepository.GetCountForSlotAsync(req.StaffId, req.Date, req.Time, ct);
        var queuePosition = count + 1;

        var entry = WaitlistEntry.Create(
            req.CustomerId,
            customerName ?? "مشتری گرامی",
            customerPhone ?? "",
            req.StaffId,
            staffName,
            req.ServiceId,
            serviceName,
            req.Date,
            req.Time,
            queuePosition);

        await waitlistRepository.AddAsync(entry, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Map(entry);
    }

    public async Task<WaitlistEntryDto?> NotifyAsync(Guid id, CancellationToken ct = default)
    {
        var entry = await waitlistRepository.GetByIdAsync(id, ct);
        if (entry is null) return null;

        entry.Notify();
        await unitOfWork.SaveChangesAsync(ct);

        return Map(entry);
    }

    public async Task<bool> CancelAsync(Guid id, CancellationToken ct = default)
    {
        var entry = await waitlistRepository.GetByIdAsync(id, ct);
        if (entry is null) return false;

        waitlistRepository.Remove(entry);
        await unitOfWork.SaveChangesAsync(ct);
        return true;
    }

    private static WaitlistEntryDto Map(WaitlistEntry w) => new(
        w.Id,
        w.CustomerId,
        w.CustomerName,
        w.CustomerPhone,
        w.StaffId,
        w.StaffName,
        w.ServiceId,
        w.ServiceName,
        w.Date,
        w.Time,
        w.Status,
        w.QueuePosition,
        w.NotifiedAt?.ToString("O"),
        w.CreatedAt.ToString("O"),
        w.UpdatedAt.ToString("O"));
}
