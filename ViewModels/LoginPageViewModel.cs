using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreExplorer.Models;
using StoreExplorer.Services;
using StoreExplorer.Views;

namespace StoreExplorer.ViewModels;

public sealed partial class LoginPageViewModel : ObservableObject
{
    private readonly AuthApiService authApi = ServiceRegistry.AuthApi;

    private string email = string.Empty;
    private string password = string.Empty;
    private bool isBusy;
    private string statusMessage = "Sign in to manage favorites and visits.";

    public LoginPageViewModel()
    {
        LoginCommand = new AsyncRelayCommand(LoginAsync);
        NavigateToSignUpCommand = new AsyncRelayCommand(NavigateToSignUpAsync);
    }

    public string Email
    {
        get => email;
        set => SetProperty(ref email, value);
    }

    public string Password
    {
        get => password;
        set => SetProperty(ref password, value);
    }

    public bool IsBusy
    {
        get => isBusy;
        set => SetProperty(ref isBusy, value);
    }

    public string StatusMessage
    {
        get => statusMessage;
        set => SetProperty(ref statusMessage, value);
    }

    public IAsyncRelayCommand LoginCommand { get; }
    public IAsyncRelayCommand NavigateToSignUpCommand { get; }

    public async Task LoginAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            var result = await authApi.LoginAsync(new LoginRequest(Email.Trim(), Password));
            StatusMessage = result.Message;
            if (result.IsSuccess)
            {
                await Shell.Current.GoToAsync("..");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static async Task NavigateToSignUpAsync()
    {
        await Shell.Current.GoToAsync(nameof(SignUpPage));
    }
}
