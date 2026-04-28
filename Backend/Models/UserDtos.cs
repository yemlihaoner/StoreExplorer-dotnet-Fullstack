namespace MyMAUIApp1.Backend.Models;

public sealed record SignUpRequest(
    string Email,
    string UserName,
    string Password);

public sealed record LoginRequest(
    string Email,
    string Password);

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);

public sealed record CreateReviewRequest(
    int Rating,
    string Comment);

public sealed record UserSummaryDto(
    Guid Id,
    string Email,
    string UserName);

public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    UserSummaryDto User);

public sealed record UserVisitDto(
    Guid StoreId,
    string StoreName,
    int Rating,
    string Comment,
    DateTimeOffset VisitedAtUtc);

public sealed record UserProfileDto(
    Guid Id,
    string Email,
    string UserName,
    IReadOnlyList<Guid> FavoriteStoreIds,
    IReadOnlyList<UserVisitDto> RecentVisits);