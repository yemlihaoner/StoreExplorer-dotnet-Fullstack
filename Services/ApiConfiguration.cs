namespace StoreExplorer.Services;

public static class ApiConfiguration
{
    public static Uri BaseAddress => new(GetBaseAddress());

    private static string GetBaseAddress()
    {
        return DeviceInfo.Platform == DevicePlatform.Android
            ? "http://10.0.2.2:5271/"
            : "http://localhost:5271/";
    }
}
