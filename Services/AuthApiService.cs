using System.Net;
using System.Net.Http.Json;
using MyMAUIApp1.Models;

namespace MyMAUIApp1.Services;

public sealed class AuthApiService
{
    private readonly HttpClient httpClient;
    private readonly AuthSession authSession;

    public AuthApiService(AuthSession authSession)
    {
        this.authSession = authSession;
        httpClient = new HttpClient
        {
            BaseAddress = ApiConfiguration.BaseAddress
        };
    }

    public async Task<(bool IsSuccess, string Message)> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/auth/login", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.Locked)
            {
                return (false, "Account temporarily locked due to failed sign-in attempts.");
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return (false, "Invalid email or password.");
            }

            return (false, $"Login failed ({(int)response.StatusCode}).");
        }

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: cancellationToken);
        if (auth is null)
        {
            return (false, "Login failed due to an invalid server response.");
        }

        authSession.Set(auth);
        return (true, "Logged in.");
    }

    public async Task<(bool IsSuccess, string Message)> SignUpAsync(SignUpRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/auth/signup", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                return (false, "A user with this email already exists.");
            }

            return (false, string.IsNullOrWhiteSpace(body) ? $"Signup failed ({(int)response.StatusCode})." : body);
        }

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: cancellationToken);
        if (auth is null)
        {
            return (false, "Signup failed due to an invalid server response.");
        }

        authSession.Set(auth);
        return (true, "Account created.");
    }

    public void Logout()
    {
        authSession.Clear();
    }
}
