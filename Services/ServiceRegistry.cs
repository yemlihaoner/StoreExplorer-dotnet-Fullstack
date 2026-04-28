namespace MyMAUIApp1.Services;

public static class ServiceRegistry
{
    public static AuthSession AuthSession { get; } = new();

    public static AuthApiService AuthApi { get; } = new(AuthSession);

    public static UserApiService UserApi { get; } = new(AuthSession);

    public static StoreApiService StoreApi { get; } = new(AuthSession);
}
