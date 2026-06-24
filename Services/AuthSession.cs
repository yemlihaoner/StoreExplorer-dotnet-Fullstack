using StoreExplorer.Models;

namespace StoreExplorer.Services;

public sealed class AuthSession
{
    public event EventHandler? SessionChanged;

    public string? AccessToken { get; private set; }

    public DateTimeOffset? ExpiresAtUtc { get; private set; }

    public UserSummaryDto? User { get; private set; }

    public bool IsAuthenticated =>
        !string.IsNullOrWhiteSpace(AccessToken)
        && ExpiresAtUtc is not null
        && ExpiresAtUtc > DateTimeOffset.UtcNow
        && User is not null;

    public void Set(AuthResponse response)
    {
        AccessToken = response.AccessToken;
        ExpiresAtUtc = response.ExpiresAtUtc;
        User = response.User;
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        AccessToken = null;
        ExpiresAtUtc = null;
        User = null;
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }
}
