using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreExplorer.Models;
using StoreExplorer.Services;

namespace StoreExplorer.ViewModels;

public sealed partial class SignUpPageViewModel : ObservableObject
{
    private readonly AuthApiService authApi = ServiceRegistry.AuthApi;

    private string email = string.Empty;
    private string userName = string.Empty;
    private string password = string.Empty;
    private bool isBusy;
    private string statusMessage = "Create an account to save favorite stores.";

    private string emailError = string.Empty;
    private string userNameError = string.Empty;
    private string passwordError = string.Empty;
    private string generalError = string.Empty;
    private bool isValidationActive;

    public SignUpPageViewModel()
    {
        SignUpCommand = new AsyncRelayCommand(SignUpAsync);
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

    public string UserName
    {
        get => userName;
        set
        {
            if (SetProperty(ref userName, value))
            {
                ValidateUserName();
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

    public string UserNameError
    {
        get => userNameError;
        set
        {
            if (SetProperty(ref userNameError, value))
            {
                OnPropertyChanged(nameof(HasUserNameError));
            }
        }
    }

    public bool HasUserNameError => !string.IsNullOrWhiteSpace(UserNameError);

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

    private void ValidateUserName()
    {
        if (!isValidationActive) return;

        if (string.IsNullOrWhiteSpace(userName))
        {
            UserNameError = "Username is required.";
        }
        else
        {
            UserNameError = string.Empty;
        }
    }

    private void ValidatePassword()
    {
        if (!isValidationActive) return;

        if (string.IsNullOrEmpty(password))
        {
            PasswordError = "Password is required.";
            return;
        }

        var errors = new List<string>();
        if (password.Length < 12)
        {
            errors.Add("at least 12 characters");
        }
        if (!password.Any(char.IsUpper))
        {
            errors.Add("one uppercase letter");
        }
        if (!password.Any(char.IsLower))
        {
            errors.Add("one lowercase letter");
        }
        if (!password.Any(char.IsDigit))
        {
            errors.Add("one digit");
        }
        if (!password.Any(character => !char.IsLetterOrDigit(character)))
        {
            errors.Add("one symbol");
        }

        if (errors.Count > 0)
        {
            PasswordError = "Password must have: " + string.Join(", ", errors) + ".";
        }
        else
        {
            PasswordError = string.Empty;
        }
    }

    public IAsyncRelayCommand SignUpCommand { get; }

    public async Task SignUpAsync()
    {
        if (IsBusy)
        {
            return;
        }

        isValidationActive = true;
        ValidateEmail();
        ValidateUserName();
        ValidatePassword();

        if (HasEmailError || HasUserNameError || HasPasswordError)
        {
            GeneralError = "Please fix the validation errors below.";
            return;
        }

        GeneralError = string.Empty;

        try
        {
            IsBusy = true;
            var result = await authApi.SignUpAsync(new SignUpRequest(Email.Trim(), UserName.Trim(), Password));
            if (result.IsSuccess)
            {
                if (Shell.Current is not null)
                {
                    await Shell.Current.GoToAsync("..");
                }
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
}
