using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyMAUIApp1.Models;
using MyMAUIApp1.Services;

namespace MyMAUIApp1.ViewModels;

public sealed partial class SignUpPageViewModel : ObservableObject
{
    private readonly AuthApiService authApi = ServiceRegistry.AuthApi;

    private string email = string.Empty;
    private string userName = string.Empty;
    private string password = string.Empty;
    private bool isBusy;
    private string statusMessage = "Create an account to save favorite stores.";

    public SignUpPageViewModel()
    {
        SignUpCommand = new AsyncRelayCommand(SignUpAsync);
    }

    public string Email
    {
        get => email;
        set => SetProperty(ref email, value);
    }

    public string UserName
    {
        get => userName;
        set => SetProperty(ref userName, value);
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

    public IAsyncRelayCommand SignUpCommand { get; }

    public async Task SignUpAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            var result = await authApi.SignUpAsync(new SignUpRequest(Email.Trim(), UserName.Trim(), Password));
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
}
