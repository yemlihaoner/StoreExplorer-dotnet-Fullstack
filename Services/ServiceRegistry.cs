namespace StoreExplorer.Services;

public static class ServiceRegistry
{
    public static AuthSession AuthSession { get; set; } = new();

    public static AuthApiService AuthApi { get; set; } = new(AuthSession);

    public static UserApiService UserApi { get; set; } = new(AuthSession);

    public static StoreApiService StoreApi { get; set; } = new(AuthSession);
}
