using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;
using StoreExplorer.ViewModels;
using StoreExplorer.Services;
using StoreExplorer.Models;

namespace StoreExplorer.Tests;

public class ViewModelTests
{
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, Task<HttpResponseMessage>> SendAsyncFunc { get; set; } = null!;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            return SendAsyncFunc(request);
        }
    }

    private readonly MockHttpMessageHandler _httpHandler;
    private readonly HttpClient _httpClient;
    private readonly AuthSession _authSession;
    private readonly AuthApiService _authApi;

    public ViewModelTests()
    {
        _httpHandler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_httpHandler)
        {
            BaseAddress = new Uri("http://localhost:5271/")
        };
        _authSession = new AuthSession();
        _authApi = new AuthApiService(_authSession, _httpClient);
        
        // Inject into ServiceRegistry for ViewModels to resolve
        ServiceRegistry.AuthSession = _authSession;
        ServiceRegistry.AuthApi = _authApi;
    }

    [Fact]
    public void SignUpPageViewModel_Validation_DetectsEmptyOrInvalidFields()
    {
        // Arrange
        var vm = new SignUpPageViewModel();

        // Act & Assert
        // Initial state has no errors
        Assert.False(vm.HasEmailError);
        Assert.False(vm.HasUserNameError);
        Assert.False(vm.HasPasswordError);

        // Validate command sets active validation
        vm.SignUpCommand.Execute(null);

        // Blank fields trigger required error labels
        Assert.True(vm.HasEmailError);
        Assert.Equal("Email is required.", vm.EmailError);
        Assert.True(vm.HasUserNameError);
        Assert.Equal("Username is required.", vm.UserNameError);
        Assert.True(vm.HasPasswordError);
        Assert.Equal("Password is required.", vm.PasswordError);

        // Provide invalid email format
        vm.Email = "invalid-email";
        Assert.True(vm.HasEmailError);
        Assert.Equal("Please enter a valid email address.", vm.EmailError);

        // Provide valid fields
        vm.Email = "valid@example.com";
        vm.UserName = "someuser";
        vm.Password = "StrongPassword123!"; // meets backend criteria
        Assert.False(vm.HasEmailError);
        Assert.False(vm.HasUserNameError);
        Assert.False(vm.HasPasswordError);
    }

    [Fact]
    public async Task SignUpPageViewModel_SignUpCommand_SucceedsOnValidInputs()
    {
        // Arrange
        var vm = new SignUpPageViewModel
        {
            Email = "newuser@example.com",
            UserName = "newuser",
            Password = "ValidPassword123!"
        };

        var responseBody = new AuthResponse(
            "dummy_token",
            DateTimeOffset.UtcNow.AddHours(1),
            new UserSummaryDto(Guid.NewGuid(), "newuser@example.com", "newuser")
        );

        _httpHandler.SendAsyncFunc = req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/api/auth/signup", req.RequestUri?.AbsolutePath);
            
            var res = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(responseBody), Encoding.UTF8, "application/json")
            };
            return Task.FromResult(res);
        };

        // Act
        await vm.SignUpAsync();

        // Assert
        Assert.False(vm.HasGeneralError);
        Assert.Equal("dummy_token", _authSession.AccessToken);
        Assert.Equal("newuser", _authSession.User?.UserName);
    }

    [Fact]
    public async Task LoginPageViewModel_LoginCommand_FailedCredentialsSetsGeneralError()
    {
        // Arrange
        var vm = new LoginPageViewModel
        {
            Email = "test@example.com",
            Password = "WrongPassword123!"
        };

        _httpHandler.SendAsyncFunc = req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/api/auth/login", req.RequestUri?.AbsolutePath);
            
            var res = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            return Task.FromResult(res);
        };

        // Act
        await vm.LoginAsync();

        // Assert
        Assert.True(vm.HasGeneralError);
        Assert.Equal("Invalid email or password.", vm.GeneralError);
        Assert.Null(_authSession.AccessToken);
    }

    [Fact]
    public async Task ForgotPasswordPageViewModel_Flow_StepByStepTransitions()
    {
        // Arrange
        var vm = new ForgotPasswordPageViewModel
        {
            Email = "reset@example.com"
        };

        _httpHandler.SendAsyncFunc = req =>
        {
            if (req.RequestUri?.AbsolutePath == "/api/auth/forgot-password")
            {
                var forgotRes = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"message\":\"Success\",\"code\":\"123456\"}", Encoding.UTF8, "application/json")
                };
                return Task.FromResult(forgotRes);
            }
            else if (req.RequestUri?.AbsolutePath == "/api/auth/reset-password")
            {
                var resetRes = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"message\":\"Success\"}", Encoding.UTF8, "application/json")
                };
                return Task.FromResult(resetRes);
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));
        };

        // Act - Step 1: Request Code
        Assert.False(vm.IsCodeSent);
        await vm.RequestCodeAsync();

        // Assert - Step 1 complete
        Assert.True(vm.IsCodeSent);
        Assert.False(vm.HasGeneralError);

        // Act - Step 2: Reset Password
        vm.Code = "123456";
        vm.NewPassword = "NewStrongPassword123!";
        await vm.ResetPasswordAsync();

        // Assert - Step 2 complete
        Assert.False(vm.HasGeneralError);
    }
}
