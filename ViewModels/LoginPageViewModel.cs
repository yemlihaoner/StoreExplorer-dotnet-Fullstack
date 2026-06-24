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

    private string emailError = string.Empty;
    private string passwordError = string.Empty;
    private string generalError = string.Empty;
    private bool isValidationActive;

    public LoginPageViewModel()
    {
        LoginCommand = new AsyncRelayCommand(LoginAsync);
        NavigateToSignUpCommand = new AsyncRelayCommand(NavigateToSignUpAsync);
        NavigateToForgotPasswordCommand = new AsyncRelayCommand(NavigateToForgotPasswordAsync);
    }

    public string Email
    {
        get => email;
        set
        {
            if (SetProperty(ref email, value))
            {
                ValidateEmail();
            }
        }
    }

    public string Password
    {
        get => password;
        set
        {
            if (SetProperty(ref password, value))
            {
                ValidatePassword();
            }
        }
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

    public string EmailError
    {
        get => emailError;
        set
        {
            if (SetProperty(ref emailError, value))
            {
                OnPropertyChanged(nameof(HasEmailError));
            }
        }
    }

    public bool HasEmailError => !string.IsNullOrWhiteSpace(EmailError);

    public string PasswordError
    {
        get => passwordError;
        set
        {
            if (SetProperty(ref passwordError, value))
            {
                OnPropertyChanged(nameof(HasPasswordError));
            }
        }
    }

    public bool HasPasswordError => !string.IsNullOrWhiteSpace(PasswordError);

    public string GeneralError
    {
        get => generalError;
        set
        {
            if (SetProperty(ref generalError, value))
            {
                OnPropertyChanged(nameof(HasGeneralError));
            }
        }
    }

    public bool HasGeneralError => !string.IsNullOrWhiteSpace(GeneralError);

    private void ValidateEmail()
    {
        if (!isValidationActive) return;

        if (string.IsNullOrWhiteSpace(email))
        {
            EmailError = "Email is required.";
            return;
        }

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            if (addr.Address == email.Trim())
            {
                EmailError = string.Empty;
                return;
            }
        }
        catch { }

        EmailError = "Please enter a valid email address.";
    }

    private void ValidatePassword()
    {
        if (!isValidationActive) return;

        if (string.IsNullOrEmpty(password))
        {
            PasswordError = "Password is required.";
        }
        else
        {
            PasswordError = string.Empty;
        }
    }

    public IAsyncRelayCommand LoginCommand { get; }
    public IAsyncRelayCommand NavigateToSignUpCommand { get; }
    public IAsyncRelayCommand NavigateToForgotPasswordCommand { get; }

    public async Task LoginAsync()
    {
        if (IsBusy)
        {
            return;
        }

        isValidationActive = true;
        ValidateEmail();
        ValidatePassword();

        if (HasEmailError || HasPasswordError)
        {
            GeneralError = "Please fix the validation errors above.";
            return;
        }

        GeneralError = string.Empty;

        try
        {
            IsBusy = true;
            var result = await authApi.LoginAsync(new LoginRequest(Email.Trim(), Password));
            if (result.IsSuccess)
            {
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                GeneralError = result.Message;
            }
        }
        catch (Exception ex)
        {
            GeneralError = $"An unexpected error occurred: {ex.Message}";
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

    private static async Task NavigateToForgotPasswordAsync()
    {
        await Shell.Current.GoToAsync(nameof(ForgotPasswordPage));
    }
}
