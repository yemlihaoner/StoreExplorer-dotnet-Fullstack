using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MyMAUIApp1.Models;

namespace MyMAUIApp1.Services;

public sealed class UserApiService
{
    private readonly HttpClient httpClient;
    private readonly AuthSession authSession;

    public UserApiService(AuthSession authSession)
    {
        this.authSession = authSession;
        httpClient = new HttpClient
        {
            BaseAddress = ApiConfiguration.BaseAddress
        };
    }

    public async Task<UserProfileDto?> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, "api/users/me");
        if (request is null)
        {
            return null;
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<UserProfileDto>(cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<StoreDto>> GetFavoriteStoresAsync(CancellationToken cancellationToken = default)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, "api/users/me/favorites");
        if (request is null)
        {
            return [];
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        var favorites = await response.Content.ReadFromJsonAsync<List<StoreDto>>(cancellationToken: cancellationToken);
        return favorites ?? [];
    }

    public async Task<bool> AddFavoriteAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Post, $"api/users/me/favorites/{storeId}");
        if (request is null)
        {
            return false;
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RemoveFavoriteAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Delete, $"api/users/me/favorites/{storeId}");
        if (request is null)
        {
            return false;
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<(bool IsSuccess, string Message)> ChangePasswordAsync(ChangePasswordRequest payload, CancellationToken cancellationToken = default)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Put, "api/users/me/password");
        if (request is null)
        {
            return (false, "You are not authenticated.");
        }

        request.Content = JsonContent.Create(payload);
        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return (true, "Password updated.");
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            return (false, "Password update was rejected. Check current password and policy.");
        }

        return (false, $"Password update failed ({(int)response.StatusCode}).");
    }

    private HttpRequestMessage? CreateAuthorizedRequest(HttpMethod method, string route)
    {
        if (!authSession.IsAuthenticated)
        {
            return null;
        }

        var request = new HttpRequestMessage(method, route);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authSession.AccessToken);
        return request;
    }
}
