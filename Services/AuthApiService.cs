using System.Net;
using System.Net.Http.Json;
using StoreExplorer.Models;

namespace StoreExplorer.Services;

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

    public AuthApiService(AuthSession authSession, HttpClient httpClient)
    {
        this.authSession = authSession;
        this.httpClient = httpClient;
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
            try
            {
                var errorObj = await response.Content.ReadFromJsonAsync<BackendErrorResponse>(cancellationToken: cancellationToken);
                if (errorObj != null)
                {
                    if (errorObj.errors != null && errorObj.errors.Count > 0)
                    {
                        return (false, string.Join("\n", errorObj.errors));
                    }
                    if (!string.IsNullOrWhiteSpace(errorObj.error))
                    {
                        return (false, errorObj.error);
                    }
                }
            }
            catch
            {
                // Fallback to parsing raw text
            }

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

    public async Task<(bool IsSuccess, string Message, string? Code)> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/auth/forgot-password", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            try
            {
                var errorObj = await response.Content.ReadFromJsonAsync<BackendErrorResponse>(cancellationToken: cancellationToken);
                if (errorObj != null)
                {
                    if (!string.IsNullOrWhiteSpace(errorObj.error))
                    {
                        return (false, errorObj.error, null);
                    }
                }
            }
            catch {}
            
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return (false, "A user with this email was not found.", null);
            }
            return (false, $"Request failed ({(int)response.StatusCode}).", null);
        }

        var successObj = await response.Content.ReadFromJsonAsync<ForgotPasswordResponse>(cancellationToken: cancellationToken);
        return (true, successObj?.Message ?? "Reset code sent.", successObj?.Code);
    }

    public async Task<(bool IsSuccess, string Message)> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/auth/reset-password", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            try
            {
                var errorObj = await response.Content.ReadFromJsonAsync<BackendErrorResponse>(cancellationToken: cancellationToken);
                if (errorObj != null)
                {
                    if (errorObj.errors != null && errorObj.errors.Count > 0)
                    {
                        return (false, string.Join("\n", errorObj.errors));
                    }
                    if (!string.IsNullOrWhiteSpace(errorObj.error))
                    {
                        return (false, errorObj.error);
                    }
                }
            }
            catch {}
            return (false, $"Password reset failed ({(int)response.StatusCode}).");
        }

        return (true, "Password has been reset successfully.");
    }
}

internal sealed record BackendErrorResponse(List<string>? errors, string? error);
internal sealed record ForgotPasswordResponse(string Message, string Code);
