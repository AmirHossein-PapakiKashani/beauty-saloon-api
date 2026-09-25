namespace BarberSalon.Application.Admin.DTOs;

public sealed record ActivityLogDto(
    string Id,
    string Type,
    string Description,
    string Time
);
