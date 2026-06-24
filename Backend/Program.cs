using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using StoreExplorer.Backend.Data;
using StoreExplorer.Backend.Models;
using StoreExplorer.Backend.Services;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var configuredJwtSigningKey = builder.Configuration["Jwt:SigningKey"];
if (string.IsNullOrWhiteSpace(configuredJwtSigningKey))
{
    if (builder.Environment.IsDevelopment())
    {
        configuredJwtSigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        builder.Logging.AddSimpleConsole(options => options.SingleLine = true);
    }
    else
    {
        throw new InvalidOperationException("Jwt:SigningKey must be configured in non-development environments.");
    }
}

if (Encoding.UTF8.GetByteCount(configuredJwtSigningKey) < 32)
{
    throw new InvalidOperationException("Jwt:SigningKey must be at least 32 bytes long.");
}

var tokenSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuredJwtSigningKey));

builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("maui-client", policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});
builder.Services.AddSingleton(tokenSigningKey);
builder.Services.AddSingleton<UserAccountService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "StoreExplorer.Backend",
            ValidAudience = "StoreExplorer.Client",
            IssuerSigningKey = tokenSigningKey,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("maui-client");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/openapi/v1.json"));

app.MapGet("/api/stores", () => Results.Ok(StoreCatalog.GetStores()));

app.MapGet("/api/stores/{id:guid}", (Guid id) =>
{
    var store = StoreCatalog.GetStores().FirstOrDefault(candidate => candidate.Id == id);
    return store is null ? Results.NotFound() : Results.Ok(store);
});

app.MapGet("/api/stores/nearby", (double latitude, double longitude, double radiusKm = 50) =>
{
    var nearbyStores = StoreCatalog.GetNearbyStores(latitude, longitude, radiusKm);
    return Results.Ok(nearbyStores);
});

app.MapPost("/api/auth/signup", (SignUpRequest request, UserAccountService accounts, SymmetricSecurityKey signingKey) =>
{
    var passwordErrors = UserAccountService.ValidatePassword(request.Password);
    if (passwordErrors.Count > 0)
    {
        return Results.BadRequest(new { errors = passwordErrors });
    }

    var result = accounts.TryCreateUser(request);
    if (!result.IsSuccess || result.User is null)
    {
        return Results.Conflict(new { error = result.Error });
    }

    var token = CreateAuthResponse(result.User, signingKey);
    return Results.Ok(token);
}).RequireRateLimiting("auth");

app.MapPost("/api/auth/login", (LoginRequest request, UserAccountService accounts, SymmetricSecurityKey signingKey) =>
{
    var result = accounts.TryValidateCredentials(request);
    if (!result.IsSuccess || result.User is null)
    {
        if (result.IsLockedOut)
        {
            return Results.StatusCode(StatusCodes.Status423Locked);
        }

        return Results.Unauthorized();
    }

    var token = CreateAuthResponse(result.User, signingKey);
    return Results.Ok(token);
}).RequireRateLimiting("auth");

app.MapGet("/api/users/me", (ClaimsPrincipal principal, UserAccountService accounts) =>
{
    var userId = GetUserId(principal);
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    var profile = accounts.GetProfile(userId.Value);
    return profile is null ? Results.NotFound() : Results.Ok(profile);
}).RequireAuthorization();

app.MapPut("/api/users/me/password", (ClaimsPrincipal principal, ChangePasswordRequest request, UserAccountService accounts) =>
{
    var userId = GetUserId(principal);
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    var passwordErrors = UserAccountService.ValidatePassword(request.NewPassword);
    if (passwordErrors.Count > 0)
    {
        return Results.BadRequest(new { errors = passwordErrors });
    }

    if (!accounts.TryChangePassword(userId.Value, request, out var error))
    {
        return Results.BadRequest(new { error });
    }

    return Results.NoContent();
}).RequireAuthorization().RequireRateLimiting("auth");

app.MapGet("/api/users/me/favorites", (ClaimsPrincipal principal, UserAccountService accounts) =>
{
    var userId = GetUserId(principal);
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    var favoriteStoreIds = accounts.GetFavoriteStoreIds(userId.Value);
    if (favoriteStoreIds is null)
    {
        return Results.NotFound();
    }

    var stores = StoreCatalog.GetStoresByIds(favoriteStoreIds);
    return Results.Ok(stores);
}).RequireAuthorization();

app.MapPost("/api/users/me/favorites/{storeId:guid}", (ClaimsPrincipal principal, Guid storeId, UserAccountService accounts) =>
{
    var userId = GetUserId(principal);
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    if (StoreCatalog.GetStoreById(storeId) is null)
    {
        return Results.NotFound(new { error = "Store was not found." });
    }

    var updated = accounts.AddFavorite(userId.Value, storeId);
    return updated ? Results.NoContent() : Results.NotFound();
}).RequireAuthorization();

app.MapDelete("/api/users/me/favorites/{storeId:guid}", (ClaimsPrincipal principal, Guid storeId, UserAccountService accounts) =>
{
    var userId = GetUserId(principal);
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    var updated = accounts.RemoveFavorite(userId.Value, storeId);
    return updated ? Results.NoContent() : Results.NotFound();
}).RequireAuthorization();

app.MapPost("/api/stores/{storeId:guid}/reviews", (ClaimsPrincipal principal, Guid storeId, CreateReviewRequest request, UserAccountService accounts) =>
{
    var userId = GetUserId(principal);
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    var user = accounts.GetSummaryById(userId.Value);
    if (user is null)
    {
        return Results.NotFound(new { error = "User was not found." });
    }

    var store = StoreCatalog.GetStoreById(storeId);
    if (store is null)
    {
        return Results.NotFound(new { error = "Store was not found." });
    }

    if (request.Rating is < 1 or > 5)
    {
        return Results.BadRequest(new { error = "Rating must be between 1 and 5." });
    }

    if (string.IsNullOrWhiteSpace(request.Comment))
    {
        return Results.BadRequest(new { error = "Comment is required." });
    }

    if (accounts.HasReviewedStore(user.Id, storeId))
    {
        return Results.Conflict(new { error = "You already reviewed this store." });
    }

    var visit = new UserVisitDto(
        storeId,
        store.Name,
        request.Rating,
        request.Comment.Trim(),
        DateTimeOffset.UtcNow);

    accounts.AddRecentVisit(user.Id, visit);

    StoreCatalog.AddReview(
        storeId,
        new ReviewDto(user.UserName, request.Rating, request.Comment.Trim(), DateOnly.FromDateTime(DateTime.UtcNow)));

    return Results.Created($"/api/stores/{storeId}", visit);
}).RequireAuthorization();

app.Run();

static AuthResponse CreateAuthResponse(UserSummaryDto user, SymmetricSecurityKey signingKey)
{
    var expiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(30);
    var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Email, user.Email),
        new(ClaimTypes.Name, user.UserName)
    };

    var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
    var jwt = new JwtSecurityToken(
        issuer: "StoreExplorer.Backend",
        audience: "StoreExplorer.Client",
        claims: claims,
        expires: expiresAtUtc.UtcDateTime,
        signingCredentials: credentials);

    var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);
    return new AuthResponse(accessToken, expiresAtUtc, user);
}

static Guid? GetUserId(ClaimsPrincipal principal)
{
    var rawId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

    return Guid.TryParse(rawId, out var userId) ? userId : null;
}
