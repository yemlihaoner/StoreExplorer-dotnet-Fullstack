using System.Threading.Tasks;

namespace Microsoft.Maui.Controls
{
    public class Shell
    {
        public static Shell? Current { get; set; }
        
        public Task GoToAsync(string state)
        {
            return Task.CompletedTask;
        }
        
        public Task DisplayAlertAsync(string title, string message, string cancel)
        {
            return Task.CompletedTask;
        }
    }
}

namespace StoreExplorer.Views
{
    public class LoginPage {}
    public class SignUpPage {}
    public class ForgotPasswordPage {}
}

namespace StoreExplorer.Services
{
    public class UserApiService
    {
        public UserApiService(AuthSession session) {}
    }

    public class StoreApiService
    {
        public StoreApiService(AuthSession session) {}
    }
}

namespace Microsoft.Maui.Devices
{
    public static class DeviceInfo
    {
        public static DevicePlatform Platform { get; set; } = DevicePlatform.Unknown;
    }

    public struct DevicePlatform
    {
        public static DevicePlatform Android => new DevicePlatform("Android");
        public static DevicePlatform Unknown => new DevicePlatform("Unknown");

        private readonly string _val;
        private DevicePlatform(string val) => _val = val;
        
        public static bool operator ==(DevicePlatform left, DevicePlatform right) => left._val == right._val;
        public static bool operator !=(DevicePlatform left, DevicePlatform right) => left._val != right._val;
        public override bool Equals(object? obj) => obj is DevicePlatform dp && dp._val == _val;
        public override int GetHashCode() => _val.GetHashCode();
    }
}
