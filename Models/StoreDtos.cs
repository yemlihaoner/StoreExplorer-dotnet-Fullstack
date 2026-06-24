namespace StoreExplorer.Models;

public sealed record StoreDto(
    Guid Id,
    string Name,
    string Description,
    StoreLocationDto Location,
    IReadOnlyList<MenuItemDto> Menu,
    IReadOnlyList<ReviewDto> Reviews);

public sealed record StoreLocationDto(
    string Address,
    double Latitude,
    double Longitude);

public sealed record MenuItemDto(
    string Name,
    string Description,
    decimal Price,
    string Category);

public sealed record ReviewDto(
    string Author,
    int Rating,
    string Comment,
    DateOnly VisitDate);