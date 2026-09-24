using BarberSalon.Application.Auth.Interfaces;
using BarberSalon.Application.Common.Interfaces;
using BarberSalon.Application.Customers.Interfaces;
using BarberSalon.Application.Reviews.DTOs;
using BarberSalon.Application.Reviews.Interfaces;
using BarberSalon.Application.SalonServices.Interfaces;
using BarberSalon.Application.Staff.Interfaces;
using BarberSalon.Domain.Reviews.Entities;

namespace BarberSalon.Application.Reviews.Services;

public sealed class ReviewService(
    IReviewRepository reviewRepository,
    IUnitOfWork unitOfWork,
    ICustomerRepository customerRepository,
    IStaffRepository staffRepository,
    ISalonServiceRepository salonServiceRepository,
    IUserRepository userRepository)
{
    public async Task<List<ReviewDto>> GetAllAsync(
        Guid? customerId,
        Guid? staffId,
        Guid? serviceId,
        CancellationToken ct = default)
    {
        var list = await reviewRepository.GetAllAsync(customerId, staffId, serviceId, ct);
        return list.Select(Map).ToList();
    }

    public async Task<List<ReviewDto>> GetByStaffIdAsync(Guid staffId, CancellationToken ct = default)
    {
        return await GetAllAsync(null, staffId, null, ct);
    }

    public async Task<ReviewDto> CreateAsync(CreateReviewRequest req, CancellationToken ct = default)
    {
        var customerName = req.CustomerName;
        if (string.IsNullOrWhiteSpace(customerName))
        {
            var customer = await customerRepository.GetByIdAsync(req.CustomerId, ct);
            customerName = customer?.FullName;
            if (string.IsNullOrWhiteSpace(customerName))
            {
                var user = await userRepository.GetByIdAsync(req.CustomerId, ct);
                customerName = user?.FullName ?? "مشتری گرامی";
            }
        }

        var staffName = req.StaffName;
        if (string.IsNullOrWhiteSpace(staffName) && req.StaffId.HasValue)
        {
            var staff = await staffRepository.GetByIdAsync(req.StaffId.Value, ct);
            staffName = staff?.FullName ?? "";
        }

        var serviceName = req.ServiceName;
        if (string.IsNullOrWhiteSpace(serviceName) && req.ServiceId.HasValue)
        {
            var service = await salonServiceRepository.GetByIdAsync(req.ServiceId.Value, ct);
            serviceName = service?.Name ?? "";
        }

        var review = Review.Create(
            req.CustomerId,
            customerName ?? "مشتری گرامی",
            req.Rating,
            req.Comment,
            req.StaffId,
            staffName,
            req.ServiceId,
            serviceName,
            req.AppointmentId);

        await reviewRepository.AddAsync(review, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Map(review);
    }

    public async Task<ReviewDto?> UpdateStatusAsync(Guid id, string status, CancellationToken ct = default)
    {
        var review = await reviewRepository.GetByIdAsync(id, ct);
        if (review is null) return null;

        review.UpdateStatus(status);
        await unitOfWork.SaveChangesAsync(ct);

        return Map(review);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var review = await reviewRepository.GetByIdAsync(id, ct);
        if (review is null) return false;

        reviewRepository.Remove(review);
        await unitOfWork.SaveChangesAsync(ct);
        return true;
    }

    private static ReviewDto Map(Review r) => new(
        r.Id,
        r.AppointmentId,
        r.CustomerId,
        r.CustomerName,
        r.StaffId,
        r.StaffName,
        r.ServiceId,
        r.ServiceName,
        r.Rating,
        r.Comment,
        r.Status,
        r.CreatedAt.ToString("O"),
        r.UpdatedAt.ToString("O"));
}
