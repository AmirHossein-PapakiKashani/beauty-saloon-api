# Reference Implementation: SalonService — Complete Layer-by-Layer Example

> This file is the **canonical pattern** for all modules in this project.
> Every new module MUST follow this exact structure.
> All code below is production-ready and compliant with AGENTS.md §10.

---

## File Map

```
Domain/SalonServices/Entities/SalonService.cs
Application/SalonServices/Interfaces/ISalonServiceRepository.cs
Application/SalonServices/DTOs/SalonServiceDto.cs
Application/SalonServices/DTOs/CreateSalonServiceRequest.cs
Application/SalonServices/DTOs/UpdateSalonServiceRequest.cs
Application/SalonServices/Services/SalonServiceManager.cs
Infrastructure/Repositories/SalonServiceRepository.cs
Infrastructure/Persistence/Configurations/SalonServiceConfiguration.cs
API/Controllers/SalonServicesController.cs
```

---

## Prerequisites: Shared Common Classes

These classes live in `Common` and are shared across all modules.
Do NOT duplicate them per module.

### `Domain/Common/BaseEntity.cs`

```csharp
namespace BarberSalon.Domain.Common;

/// <summary>
/// Base class for all domain entities.
/// Manages identity, creation timestamp, and last-updated timestamp.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>Unique identifier. Set once in the constructor.</summary>
    public Guid Id { get; private set; }

    /// <summary>UTC timestamp of when this entity was created.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>UTC timestamp of the last modification.</summary>
    public DateTime UpdatedAt { get; private set; }

    protected BaseEntity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates <see cref="UpdatedAt"/> to the current UTC time.
    /// Call this at the end of every mutation method inside an Entity.
    /// </summary>
    protected void Touch() => UpdatedAt = DateTime.UtcNow;
}
```

### `Application/Common/Exceptions/NotFoundException.cs`

```csharp
namespace BarberSalon.Application.Common.Exceptions;

/// <summary>Thrown when a requested entity does not exist in the database.</summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} with key '{key}' was not found.") { }
}
```

### `Application/Common/Exceptions/ValidationException.cs`

```csharp
namespace BarberSalon.Application.Common.Exceptions;

/// <summary>Thrown when business input data fails validation at the application layer.</summary>
public sealed class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}
```

### `Application/Common/Exceptions/DomainException.cs`

```csharp
namespace BarberSalon.Application.Common.Exceptions;

/// <summary>Thrown when a domain invariant or business rule is violated inside an Entity.</summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
```

### `Application/Common/Interfaces/IUnitOfWork.cs`

```csharp
namespace BarberSalon.Application.Common.Interfaces;

/// <summary>
/// Unit of Work abstraction. Persists all pending EF Core changes in a single transaction.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Commits all tracked changes to the database.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

---

## Layer 1: Domain

### `Domain/SalonServices/Entities/SalonService.cs`

```csharp
using BarberSalon.Application.Common.Exceptions;
using BarberSalon.Domain.Common;

namespace BarberSalon.Domain.SalonServices.Entities;

/// <summary>
/// A service offered by the salon — e.g. Haircut, Beard Trim, Hair Colour.
/// Acts as an independent Aggregate Root.
/// </summary>
public sealed class SalonService : BaseEntity
{
    /// <summary>Display name of the service. Must be unique across the salon.</summary>
    public string Name { get; private set; } = default!;

    /// <summary>Short description shown to customers during booking.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Estimated duration of the service in minutes.</summary>
    public int DurationMinutes { get; private set; }

    /// <summary>Price of the service in the local currency (Toman).</summary>
    public decimal Price { get; private set; }

    /// <summary>Category grouping — e.g. "Hair", "Beard", "Skin".</summary>
    public string Category { get; private set; } = default!;

    /// <summary>Whether the service is active and bookable. False means archived.</summary>
    public bool IsActive { get; private set; }

    // Private parameterless constructor required by EF Core
    private SalonService() { }

    /// <summary>
    /// Creates a new <see cref="SalonService"/> with validation.
    /// This is the only public way to instantiate this entity.
    /// </summary>
    /// <param name="name">Service name. Must not be empty. Max 100 chars.</param>
    /// <param name="description">Optional description. Max 500 chars.</param>
    /// <param name="durationMinutes">Duration in minutes. Must be between 1 and 480.</param>
    /// <param name="price">Price in Toman. Must not be negative.</param>
    /// <param name="category">Category name. Must not be empty. Max 50 chars.</param>
    /// <returns>A new active <see cref="SalonService"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when any validation rule fails.</exception>
    public static SalonService Create(
        string name,
        string description,
        int durationMinutes,
        decimal price,
        string category)
    {
        Validate(name, durationMinutes, price, category);

        return new SalonService
        {
            Name = name.Trim(),
            Description = description?.Trim() ?? string.Empty,
            DurationMinutes = durationMinutes,
            Price = price,
            Category = category.Trim(),
            IsActive = true
        };
    }

    /// <summary>
    /// Updates the service details. Applies the same validation rules as <see cref="Create"/>.
    /// </summary>
    /// <exception cref="DomainException">Thrown when any validation rule fails.</exception>
    public void Update(
        string name,
        string description,
        int durationMinutes,
        decimal price,
        string category)
    {
        Validate(name, durationMinutes, price, category);

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        DurationMinutes = durationMinutes;
        Price = price;
        Category = category.Trim();
        Touch();
    }

    /// <summary>
    /// Archives the service. Archived services do not appear in public listings
    /// and cannot be selected during booking.
    /// </summary>
    /// <exception cref="DomainException">Thrown when the service is already archived.</exception>
    public void Archive()
    {
        if (!IsActive)
            throw new DomainException("This service is already archived.");

        IsActive = false;
        Touch();
    }

    /// <summary>Reactivates a previously archived service.</summary>
    public void Activate()
    {
        IsActive = true;
        Touch();
    }

    // ─── Private Validation ─────────────────────────────────────────────────

    private static void Validate(string name, int durationMinutes, decimal price, string category)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Service name cannot be empty.");

        if (name.Length > 100)
            throw new DomainException("Service name cannot exceed 100 characters.");

        if (durationMinutes <= 0)
            throw new DomainException("Duration must be a positive number of minutes.");

        if (durationMinutes > 480)
            throw new DomainException("Duration cannot exceed 480 minutes (8 hours).");

        if (price < 0)
            throw new DomainException("Price cannot be negative.");

        if (string.IsNullOrWhiteSpace(category))
            throw new DomainException("Category cannot be empty.");
    }
}
```

---

## Layer 2: Application

### `Application/SalonServices/Interfaces/ISalonServiceRepository.cs`

```csharp
using BarberSalon.Domain.SalonServices.Entities;

namespace BarberSalon.Application.SalonServices.Interfaces;

/// <summary>Data access contract for <see cref="SalonService"/> entities.</summary>
public interface ISalonServiceRepository
{
    /// <summary>Returns a service by its identifier, or null if not found.</summary>
    Task<SalonService?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns all active services ordered by Category then Name.</summary>
    Task<List<SalonService>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns all services (active and archived). For Admin use only.</summary>
    Task<List<SalonService>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns true if a service with the given name already exists.</summary>
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Adds a new service to the EF Core change tracker.</summary>
    Task AddAsync(SalonService service, CancellationToken cancellationToken = default);

    /// <summary>Marks a modified service as updated in the EF Core change tracker.</summary>
    void Update(SalonService service);
}
```

### `Application/SalonServices/DTOs/SalonServiceDto.cs`

```csharp
namespace BarberSalon.Application.SalonServices.DTOs;

/// <summary>Read model returned to the client for a salon service.</summary>
public record SalonServiceDto(
    Guid Id,
    string Name,
    string Description,
    int DurationMinutes,
    decimal Price,
    string Category,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
```

### `Application/SalonServices/DTOs/CreateSalonServiceRequest.cs`

```csharp
namespace BarberSalon.Application.SalonServices.DTOs;

/// <summary>Input model for creating a new salon service.</summary>
public record CreateSalonServiceRequest(
    string Name,
    string Description,
    int DurationMinutes,
    decimal Price,
    string Category
);
```

### `Application/SalonServices/DTOs/UpdateSalonServiceRequest.cs`

```csharp
namespace BarberSalon.Application.SalonServices.DTOs;

/// <summary>Input model for updating an existing salon service.</summary>
public record UpdateSalonServiceRequest(
    string Name,
    string Description,
    int DurationMinutes,
    decimal Price,
    string Category
);
```

### `Application/SalonServices/Services/SalonServiceManager.cs`

```csharp
using BarberSalon.Application.Common.Exceptions;
using BarberSalon.Application.Common.Interfaces;
using BarberSalon.Application.SalonServices.DTOs;
using BarberSalon.Application.SalonServices.Interfaces;
using BarberSalon.Domain.SalonServices.Entities;

namespace BarberSalon.Application.SalonServices.Services;

/// <summary>
/// Application-layer use cases for managing salon services.
/// Orchestrates domain logic, repository access, and persistence.
/// </summary>
public sealed class SalonServiceManager
{
    private readonly ISalonServiceRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public SalonServiceManager(ISalonServiceRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>Returns all active services as DTOs.</summary>
    public async Task<List<SalonServiceDto>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        var services = await _repository.GetAllActiveAsync(cancellationToken);
        return services.Select(MapToDto).ToList();
    }

    /// <summary>Returns all services (active and archived) as DTOs. For Admin use.</summary>
    public async Task<List<SalonServiceDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var services = await _repository.GetAllAsync(cancellationToken);
        return services.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Returns a single service by its identifier.
    /// </summary>
    /// <exception cref="NotFoundException">Thrown when no service with the given ID exists.</exception>
    public async Task<SalonServiceDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var service = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(SalonService), id);

        return MapToDto(service);
    }

    /// <summary>
    /// Creates and persists a new salon service.
    /// </summary>
    /// <exception cref="ValidationException">Thrown when a service with the same name already exists.</exception>
    public async Task<SalonServiceDto> CreateAsync(
        CreateSalonServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        var nameExists = await _repository.ExistsByNameAsync(request.Name, cancellationToken);
        if (nameExists)
            throw new ValidationException($"A service named '{request.Name}' already exists.");

        var service = SalonService.Create(
            request.Name,
            request.Description,
            request.DurationMinutes,
            request.Price,
            request.Category);

        await _repository.AddAsync(service, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(service);
    }

    /// <summary>
    /// Updates an existing service's details.
    /// </summary>
    /// <exception cref="NotFoundException">Thrown when the service does not exist.</exception>
    public async Task<SalonServiceDto> UpdateAsync(
        Guid id,
        UpdateSalonServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        var service = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(SalonService), id);

        service.Update(
            request.Name,
            request.Description,
            request.DurationMinutes,
            request.Price,
            request.Category);

        _repository.Update(service);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(service);
    }

    /// <summary>
    /// Archives a service (soft delete). The service will no longer appear in public listings.
    /// </summary>
    /// <exception cref="NotFoundException">Thrown when the service does not exist.</exception>
    public async Task ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var service = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(SalonService), id);

        service.Archive();
        _repository.Update(service);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // ─── Private Helpers ────────────────────────────────────────────────────

    private static SalonServiceDto MapToDto(SalonService s) => new(
        s.Id,
        s.Name,
        s.Description,
        s.DurationMinutes,
        s.Price,
        s.Category,
        s.IsActive,
        s.CreatedAt,
        s.UpdatedAt
    );
}
```

---

## Layer 3: Infrastructure

### `Infrastructure/Repositories/SalonServiceRepository.cs`

```csharp
using BarberSalon.Application.SalonServices.Interfaces;
using BarberSalon.Domain.SalonServices.Entities;
using BarberSalon.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BarberSalon.Infrastructure.Repositories;

/// <inheritdoc cref="ISalonServiceRepository"/>
public sealed class SalonServiceRepository : ISalonServiceRepository
{
    private readonly AppDbContext _context;

    public SalonServiceRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<SalonService?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.SalonServices
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<List<SalonService>> GetAllActiveAsync(CancellationToken cancellationToken = default)
        => await _context.SalonServices
            .Where(s => s.IsActive)
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Name)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<List<SalonService>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.SalonServices
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Name)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
        => await _context.SalonServices
            .AnyAsync(s => s.Name == name.Trim(), cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(SalonService service, CancellationToken cancellationToken = default)
        => await _context.SalonServices.AddAsync(service, cancellationToken);

    /// <inheritdoc />
    public void Update(SalonService service)
        => _context.SalonServices.Update(service);
}
```

### `Infrastructure/Persistence/Configurations/SalonServiceConfiguration.cs`

```csharp
using BarberSalon.Domain.SalonServices.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarberSalon.Infrastructure.Persistence.Configurations;

/// <summary>EF Core Fluent API configuration for the SalonService entity.</summary>
public sealed class SalonServiceConfiguration : IEntityTypeConfiguration<SalonService>
{
    public void Configure(EntityTypeBuilder<SalonService> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Description)
            .HasMaxLength(500);

        builder.Property(s => s.Category)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.Price)
            .HasPrecision(18, 2);

        // Unique index — service names must be distinct
        builder.HasIndex(s => s.Name)
            .IsUnique()
            .HasDatabaseName("IX_SalonServices_Name");

        // Filtered index — most queries filter by IsActive
        builder.HasIndex(s => s.IsActive)
            .HasDatabaseName("IX_SalonServices_IsActive");

        builder.ToTable("SalonServices");
    }
}
```

---

## Layer 4: API

### `API/Controllers/SalonServicesController.cs`

```csharp
using BarberSalon.Application.SalonServices.DTOs;
using BarberSalon.Application.SalonServices.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarberSalon.API.Controllers;

/// <summary>
/// Manages salon services (e.g. Haircut, Beard Trim).
/// Customers can read services; Admins can create, update, and archive them.
/// </summary>
[ApiController]
[Route("api/salon-services")]
public sealed class SalonServicesController : ControllerBase
{
    private readonly SalonServiceManager _manager;

    public SalonServicesController(SalonServiceManager manager)
    {
        _manager = manager;
    }

    /// <summary>Returns all active salon services.</summary>
    /// <remarks>Public endpoint — no authentication required.</remarks>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<SalonServiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _manager.GetAllActiveAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>Returns all services including archived ones.</summary>
    /// <remarks>Admin only.</remarks>
    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(List<SalonServiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllIncludingArchived(CancellationToken cancellationToken)
    {
        var result = await _manager.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>Returns a single salon service by its identifier.</summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SalonServiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _manager.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>Creates a new salon service.</summary>
    /// <remarks>Admin only. Service name must be unique.</remarks>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SalonServiceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreateSalonServiceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _manager.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Updates an existing salon service.</summary>
    /// <remarks>Admin only.</remarks>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SalonServiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateSalonServiceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _manager.UpdateAsync(id, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Archives a salon service (soft delete).
    /// Archived services are hidden from customers and cannot be booked.
    /// </summary>
    /// <remarks>Admin only. This action is reversible via reactivation.</remarks>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        await _manager.ArchiveAsync(id, cancellationToken);
        return NoContent();
    }
}
```

---

## Self-Check Table — Verify every new module against this

| Rule | Where in this example |
|------|----------------------|
| Static `Create()` factory — no public constructor | `SalonService.Create()` |
| All properties use `private set` | Every property in `SalonService` |
| Business rules throw `DomainException` inside Entity | `Archive()`, `Validate()` |
| Repository Interface lives in Application | `ISalonServiceRepository.cs` |
| No EF Core in Application or Domain | EF Core only in Repository |
| DTOs use `record` | `SalonServiceDto`, request records |
| `<inheritdoc />` on Repository implementation | `SalonServiceRepository` |
| `[ProducesResponseType]` for every status code | `SalonServicesController` |
| `CreatedAtAction` for POST responses | `Create()` action |
| Soft delete — no hard delete | `ArchiveAsync()` |
| `CancellationToken` on every async method | All methods throughout |
| Private parameterless constructor for EF Core | `private SalonService() { }` |
| `Touch()` called at end of every mutation | `Update()`, `Archive()`, `Activate()` |
