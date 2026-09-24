using BarberSalon.Application.Common.Exceptions;
using BarberSalon.Application.Common.Interfaces;
using BarberSalon.Application.Customers.DTOs;
using BarberSalon.Application.Customers.Interfaces;
using BarberSalon.Domain.Common;
using BarberSalon.Domain.Customers.Entities;

namespace BarberSalon.Application.Customers.Services;

/// <summary>
/// Application service orchestrating Customer CRM use cases.
/// </summary>
public sealed class CustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of <see cref="CustomerService"/>.
    /// </summary>
    /// <param name="customerRepository">Repository contract for customers.</param>
    /// <param name="unitOfWork">Unit of Work contract for persisting changes.</param>
    public CustomerService(ICustomerRepository customerRepository, IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Creates and persists a new customer profile.
    /// </summary>
    /// <param name="request">Creation payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created customer DTO.</returns>
    /// <exception cref="ValidationException">Thrown when validation fails or customer with phone already exists.</exception>
    public async Task<CustomerDto> CreateCustomerAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ValidationException("Request body cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("Customer name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Phone))
        {
            throw new ValidationException("Customer phone number is required.");
        }

        var normalizedPhone = request.Phone.Trim();
        var exists = await _customerRepository.ExistsByPhoneNumberAsync(normalizedPhone, cancellationToken);
        if (exists)
        {
            throw new ValidationException($"A customer with phone number '{normalizedPhone}' already exists.");
        }

        var customer = Customer.Create(
            fullName: request.Name,
            phoneNumber: normalizedPhone,
            gender: request.Gender,
            notes: request.Notes);

        await _customerRepository.AddAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(customer);
    }

    /// <summary>
    /// Returns a single customer by their unique identifier.
    /// </summary>
    /// <param name="id">The customer GUID identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The customer DTO.</returns>
    /// <exception cref="NotFoundException">Thrown when the customer is not found.</exception>
    public async Task<CustomerDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), id);

        return MapToDto(customer);
    }

    /// <summary>
    /// Updates an existing customer profile.
    /// </summary>
    /// <param name="id">The customer unique identifier.</param>
    /// <param name="request">Update payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated customer DTO.</returns>
    /// <exception cref="NotFoundException">Thrown when customer does not exist.</exception>
    /// <exception cref="ValidationException">Thrown when phone is duplicate or invalid.</exception>
    public async Task<CustomerDto> UpdateCustomerAsync(
        Guid id,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ValidationException("Request body cannot be null.");
        }

        var customer = await _customerRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), id);

        var newName = string.IsNullOrWhiteSpace(request.Name) ? customer.FullName : request.Name.Trim();
        var newPhone = string.IsNullOrWhiteSpace(request.Phone) ? customer.PhoneNumber : request.Phone.Trim();

        if (newPhone != customer.PhoneNumber)
        {
            var exists = await _customerRepository.ExistsByPhoneNumberAsync(newPhone, cancellationToken);
            if (exists)
            {
                throw new ValidationException($"A customer with phone number '{newPhone}' already exists.");
            }
        }

        var newGender = request.Gender ?? customer.Gender;
        var newNotes = request.Notes ?? customer.Notes;

        try
        {
            customer.Update(newName, newPhone, newGender, newNotes, customer.UserId);

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                if (request.Status.Equals("inactive", StringComparison.OrdinalIgnoreCase) && customer.IsActive)
                {
                    customer.Archive();
                }
                else if (request.Status.Equals("active", StringComparison.OrdinalIgnoreCase) && !customer.IsActive)
                {
                    customer.Activate();
                }
            }
        }
        catch (DomainException ex)
        {
            throw new ValidationException(ex.Message);
        }

        _customerRepository.Update(customer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(customer);
    }

    /// <summary>
    /// Archives (soft deletes) an existing customer.
    /// </summary>
    /// <param name="id">The customer identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when customer does not exist.</exception>
    public async Task ArchiveCustomerAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), id);

        customer.Archive();
        _customerRepository.Update(customer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Returns all active customers as DTOs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of active customer DTOs.</returns>
    public async Task<List<CustomerDto>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        var customers = await _customerRepository.GetAllActiveAsync(cancellationToken);
        return customers.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Returns all customers (active and archived) as DTOs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all customer DTOs.</returns>
    public async Task<List<CustomerDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var customers = await _customerRepository.GetAllAsync(cancellationToken);
        return customers.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Returns customers optionally filtered by status ("active", "inactive", or all by default).
    /// </summary>
    /// <param name="status">Optional status filter ("active", "inactive", "all").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of customer DTOs.</returns>
    public async Task<List<CustomerDto>> GetCustomersAsync(
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(status, "active", StringComparison.OrdinalIgnoreCase))
        {
            return await GetAllActiveAsync(cancellationToken);
        }

        if (string.Equals(status, "inactive", StringComparison.OrdinalIgnoreCase))
        {
            var all = await _customerRepository.GetAllAsync(cancellationToken);
            return all.Where(c => !c.IsActive).Select(MapToDto).ToList();
        }

        return await GetAllAsync(cancellationToken);
    }

    /// <summary>
    /// Searches active customers matching the specified query in their name or phone number.
    /// If query is empty or whitespace, all active customers are returned.
    /// </summary>
    /// <param name="query">The search query term.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of matching customer DTOs.</returns>
    public async Task<List<CustomerDto>> SearchCustomersAsync(
        string? query = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return await GetAllActiveAsync(cancellationToken);
        }

        var customers = await _customerRepository.SearchAsync(query.Trim(), cancellationToken);
        return customers.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Maps a domain <see cref="Customer"/> entity to a <see cref="CustomerDto"/>.
    /// </summary>
    /// <param name="c">The customer entity.</param>
    /// <returns>The mapped customer DTO.</returns>
    public static CustomerDto MapToDto(Customer c) => new(
        Id: c.Id,
        Name: c.FullName,
        Phone: c.PhoneNumber,
        Gender: c.Gender,
        Notes: c.Notes,
        CreatedAt: c.CreatedAt,
        UpdatedAt: c.UpdatedAt,
        AppointmentsCount: c.AppointmentsCount,
        LastAppointment: c.LastAppointment,
        Status: c.IsActive ? "active" : "inactive"
    );
}
