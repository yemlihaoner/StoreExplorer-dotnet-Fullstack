using System.Collections.Concurrent;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using StoreExplorer.Backend.Models;

namespace StoreExplorer.Backend.Services;

public sealed class UserAccountService
{
    private const int MaxFailedLoginAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly ConcurrentDictionary<Guid, UserAccountRecord> usersById = new();
    private readonly ConcurrentDictionary<string, Guid> userIdByEmail = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> passwordResetCodes = new(StringComparer.OrdinalIgnoreCase);
    private readonly PasswordHasher<UserAccountRecord> passwordHasher = new();

    public (bool IsSuccess, string? Error, UserSummaryDto? User) TryCreateUser(SignUpRequest request)
    {
        var email = NormalizeEmail(request.Email);
        var userName = request.UserName.Trim();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(userName) || !IsValidEmail(email))
        {
            return (false, "A valid email and username are required.", null);
        }

        var user = new UserAccountRecord
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = userName
        };

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        if (!userIdByEmail.TryAdd(email, user.Id))
        {
            return (false, "A user with this email already exists.", null);
        }

        usersById[user.Id] = user;
        return (true, null, ToSummary(user));
    }

    public (bool IsSuccess, string? Error, UserSummaryDto? User, bool IsLockedOut, int? RetryAfterSeconds) TryValidateCredentials(LoginRequest request)
    {
        if (!TryGetByEmail(request.Email, out var user))
        {
            return (false, "Invalid email or password.", null, false, null);
        }

        lock (user!.SyncRoot)
        {
            if (user.LockoutEndUtc is not null && user.LockoutEndUtc > DateTimeOffset.UtcNow)
            {
                var retryAfter = (int)Math.Ceiling((user.LockoutEndUtc.Value - DateTimeOffset.UtcNow).TotalSeconds);
                return (false, "Account temporarily locked due to failed sign-in attempts.", null, true, Math.Max(retryAfter, 1));
            }

            var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                user.FailedLoginAttempts++;

                if (user.FailedLoginAttempts >= MaxFailedLoginAttempts)
                {
                    user.FailedLoginAttempts = 0;
                    user.LockoutEndUtc = DateTimeOffset.UtcNow.Add(LockoutDuration);
                    return (false, "Account temporarily locked due to failed sign-in attempts.", null, true, (int)LockoutDuration.TotalSeconds);
                }

                return (false, "Invalid email or password.", null, false, null);
            }

            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
            }

            user.FailedLoginAttempts = 0;
            user.LockoutEndUtc = null;

            return (true, null, ToSummary(user), false, null);
        }
    }

    public UserSummaryDto? GetSummaryById(Guid userId)
    {
        return usersById.TryGetValue(userId, out var user) ? ToSummary(user) : null;
    }

    public UserProfileDto? GetProfile(Guid userId)
    {
        if (!usersById.TryGetValue(userId, out var user))
        {
            return null;
        }

        lock (user.SyncRoot)
        {
            return new UserProfileDto(
                user.Id,
                user.Email,
                user.UserName,
                user.FavoriteStoreIds.ToArray(),
                user.RecentVisits.OrderByDescending(visit => visit.VisitedAtUtc).ToArray());
        }
    }

    public bool AddFavorite(Guid userId, Guid storeId)
    {
        if (!usersById.TryGetValue(userId, out var user))
        {
            return false;
        }

        lock (user.SyncRoot)
        {
            user.FavoriteStoreIds.Add(storeId);
            return true;
        }
    }

    public bool RemoveFavorite(Guid userId, Guid storeId)
    {
        if (!usersById.TryGetValue(userId, out var user))
        {
            return false;
        }

        lock (user.SyncRoot)
        {
            user.FavoriteStoreIds.Remove(storeId);
            return true;
        }
    }

    public IReadOnlyList<Guid>? GetFavoriteStoreIds(Guid userId)
    {
        if (!usersById.TryGetValue(userId, out var user))
        {
            return null;
        }

        lock (user.SyncRoot)
        {
            return user.FavoriteStoreIds.ToArray();
        }
    }

    public bool TryChangePassword(Guid userId, ChangePasswordRequest request, out string? error)
    {
        error = null;

        if (!usersById.TryGetValue(userId, out var user))
        {
            error = "User not found.";
            return false;
        }

        var oldPasswordValidation = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
        if (oldPasswordValidation == PasswordVerificationResult.Failed)
        {
            error = "Current password is incorrect.";
            return false;
        }

        if (string.Equals(request.CurrentPassword, request.NewPassword, StringComparison.Ordinal))
        {
            error = "New password must be different from current password.";
            return false;
        }

        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        return true;
    }

    public (bool IsSuccess, string? Error, string? Code) InitiatePasswordReset(string emailRaw)
    {
        var email = NormalizeEmail(emailRaw);
        if (string.IsNullOrWhiteSpace(email) || !userIdByEmail.ContainsKey(email))
        {
            return (false, "User with this email was not found.", null);
        }

        var rand = new Random();
        var code = rand.Next(100000, 999999).ToString();
        passwordResetCodes[email] = code;

        return (true, null, code);
    }

    public bool TryResetPassword(string emailRaw, string code, string newPassword, out string? error)
    {
        error = null;
        var email = NormalizeEmail(emailRaw);

        if (string.IsNullOrWhiteSpace(email) || !userIdByEmail.TryGetValue(email, out var userId) || !usersById.TryGetValue(userId, out var user))
        {
            error = "User not found.";
            return false;
        }

        if (!passwordResetCodes.TryGetValue(email, out var storedCode) || !string.Equals(storedCode, code, StringComparison.Ordinal))
        {
            error = "Invalid or expired reset code.";
            return false;
        }

        user.PasswordHash = passwordHasher.HashPassword(user, newPassword);
        passwordResetCodes.TryRemove(email, out _);
        return true;
    }

    public bool AddRecentVisit(Guid userId, UserVisitDto visit)
    {
        if (!usersById.TryGetValue(userId, out var user))
        {
            return false;
        }

        lock (user.SyncRoot)
        {
            user.RecentVisits.Add(visit);

            // Keep the profile payload compact and focused on recent history.
            if (user.RecentVisits.Count > 30)
            {
                user.RecentVisits.RemoveRange(0, user.RecentVisits.Count - 30);
            }

            return true;
        }
    }

    public bool HasReviewedStore(Guid userId, Guid storeId)
    {
        if (!usersById.TryGetValue(userId, out var user))
        {
            return false;
        }

        lock (user.SyncRoot)
        {
            return user.RecentVisits.Any(visit => visit.StoreId == storeId);
        }
    }

    public static IReadOnlyList<string> ValidatePassword(string password)
    {
        var errors = new List<string>();

        if (password.Length < 12)
        {
            errors.Add("Password must be at least 12 characters long.");
        }

        if (!password.Any(char.IsUpper))
        {
            errors.Add("Password must contain at least one uppercase letter.");
        }

        if (!password.Any(char.IsLower))
        {
            errors.Add("Password must contain at least one lowercase letter.");
        }

        if (!password.Any(char.IsDigit))
        {
            errors.Add("Password must contain at least one digit.");
        }

        if (!password.Any(character => !char.IsLetterOrDigit(character)))
        {
            errors.Add("Password must contain at least one symbol.");
        }

        return errors;
    }

    private bool TryGetByEmail(string email, out UserAccountRecord? user)
    {
        user = null;

        var normalizedEmail = NormalizeEmail(email);
        if (!userIdByEmail.TryGetValue(normalizedEmail, out var userId))
        {
            return false;
        }

        return usersById.TryGetValue(userId, out user);
    }

    private static UserSummaryDto ToSummary(UserAccountRecord user)
    {
        return new UserSummaryDto(user.Id, user.Email, user.UserName);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static bool IsValidEmail(string email)
    {
        try
        {
            _ = new MailAddress(email);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private sealed class UserAccountRecord
    {
        public Guid Id { get; set; }

        public string Email { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public int FailedLoginAttempts { get; set; }

        public DateTimeOffset? LockoutEndUtc { get; set; }

        public HashSet<Guid> FavoriteStoreIds { get; } = [];

        public List<UserVisitDto> RecentVisits { get; } = [];

        public object SyncRoot { get; } = new();
    }
}