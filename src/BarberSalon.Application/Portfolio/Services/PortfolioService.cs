using BarberSalon.Application.Common.Interfaces;
using BarberSalon.Application.Portfolio.DTOs;
using BarberSalon.Application.Portfolio.Interfaces;
using BarberSalon.Domain.Portfolio.Entities;

namespace BarberSalon.Application.Portfolio.Services;

public sealed class PortfolioService(
    IPortfolioRepository portfolioRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<List<PortfolioItemDto>> GetAllAsync(string? category = null, Guid? staffId = null, CancellationToken ct = default)
    {
        var items = await portfolioRepository.GetAllAsync(category, staffId, ct);
        return items.Select(Map).ToList();
    }

    public async Task<PortfolioItemDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await portfolioRepository.GetByIdAsync(id, ct);
        return item is null ? null : Map(item);
    }

    public async Task<PortfolioItemDto> CreateAsync(CreatePortfolioItemRequest req, CancellationToken ct = default)
    {
        var item = PortfolioItem.Create(
            req.Title,
            req.Category,
            req.BeforeImageUrl,
            req.AfterImageUrl,
            req.Description,
            req.StaffId);

        await portfolioRepository.AddAsync(item, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Map(item);
    }

    public async Task<PortfolioItemDto?> UpdateAsync(Guid id, UpdatePortfolioItemRequest req, CancellationToken ct = default)
    {
        var item = await portfolioRepository.GetByIdAsync(id, ct);
        if (item is null) return null;

        item.Update(req.Title, req.Category, req.BeforeImageUrl, req.AfterImageUrl, req.Description, req.StaffId);
        await unitOfWork.SaveChangesAsync(ct);
        return Map(item);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var item = await portfolioRepository.GetByIdAsync(id, ct);
        if (item is null) return false;

        portfolioRepository.Remove(item);
        await unitOfWork.SaveChangesAsync(ct);
        return true;
    }

    private static PortfolioItemDto Map(PortfolioItem p) =>
        new(p.Id, p.Title, p.Category, p.BeforeImageUrl, p.AfterImageUrl, p.Description, p.StaffId, p.CreatedAt);
}
