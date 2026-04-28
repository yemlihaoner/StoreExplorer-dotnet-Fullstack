using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using MyMAUIApp1.Models;

namespace MyMAUIApp1.Services;

public sealed class StoreApiService
{
    private readonly HttpClient httpClient;
    private readonly AuthSession? authSession;

    public StoreApiService(AuthSession? authSession = null)
    {
        this.authSession = authSession;
        httpClient = new HttpClient
        {
            BaseAddress = ApiConfiguration.BaseAddress
        };
    }

    public async Task<IReadOnlyList<StoreDto>> GetStoresAsync(CancellationToken cancellationToken = default)
    {
        var stores = await httpClient.GetFromJsonAsync<List<StoreDto>>("api/stores", cancellationToken);
        return stores ?? [];
    }

    // Helper to get a single store by id via the full list (backend doesn't expose single-get currently)
    public async Task<StoreDto?> GetStoreByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var stores = await GetStoresAsync(cancellationToken);
        return stores.FirstOrDefault(s => s.Id == id);
    }

    public async Task<IReadOnlyList<StoreDto>> GetNearbyStoresAsync(double latitude, double longitude, double radiusKm = 50, CancellationToken cancellationToken = default)
    {
        var requestUri = $"api/stores/nearby?latitude={latitude}&longitude={longitude}&radiusKm={radiusKm}";
        var stores = await httpClient.GetFromJsonAsync<List<StoreDto>>(requestUri, cancellationToken);
        return stores ?? [];
    }

    public async Task<(bool IsSuccess, string Message)> AddReviewAsync(Guid storeId, int rating, string comment, CancellationToken cancellationToken = default)
    {
        if (authSession is null || !authSession.IsAuthenticated)
        {
            return (false, "You need to log in before leaving a review.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/stores/{storeId}/reviews");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authSession.AccessToken);
        request.Content = JsonContent.Create(new CreateReviewRequest(rating, comment));

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return (true, "Review added.");
        }

        var errorMessage = await TryReadErrorMessageAsync(response, cancellationToken);
        return (false, string.IsNullOrWhiteSpace(errorMessage)
            ? $"Review failed ({(int)response.StatusCode})."
            : errorMessage);
    }

    private static async Task<string?> TryReadErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(contentStream, cancellationToken: cancellationToken);
            if (document.RootElement.TryGetProperty("error", out var errorElement))
            {
                return errorElement.GetString();
            }
        }
        catch
        {
            // Keep generic status fallback when response payload is not a JSON error body.
        }

        return null;
    }
}