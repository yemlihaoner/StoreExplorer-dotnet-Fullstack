using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreExplorer.Models;
using StoreExplorer.Services;

namespace StoreExplorer.ViewModels;

public sealed partial class ForgotPasswordPageViewModel : ObservableObject
{
    private readonly AuthApiService authApi = ServiceRegistry.AuthApi;

    private string email = string.Empty;
    private string code = string.Empty;
    private string newPassword = string.Empty;
    private bool isBusy;
    private bool isCodeSent;

    private string emailError = string.Empty;
    private string codeError = string.Empty;
    private string passwordError = string.Empty;
    private string generalError = string.Empty;
    private bool isValidationActive;

    public ForgotPasswordPageViewModel()
    {
        RequestCodeCommand = new AsyncRelayCommand(RequestCodeAsync);
        ResetPasswordCommand = new AsyncRelayCommand(ResetPasswordAsync);
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

    public string Code
    {
        get => code;
        set
        {
            if (SetProperty(ref code, value))
            {
                ValidateCode();
            }
        }
    }

    public string NewPassword
    {
        get => newPassword;
        set
        {
            if (SetProperty(ref newPassword, value))
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

    public bool IsCodeSent
    {
        get => isCodeSent;
        set => SetProperty(ref isCodeSent, value);
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

    public string CodeError
    {
        get => codeError;
        set
        {
            if (SetProperty(ref codeError, value))
            {
                OnPropertyChanged(nameof(HasCodeError));
            }
        }
    }

    public bool HasCodeError => !string.IsNullOrWhiteSpace(CodeError);

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

    public IAsyncRelayCommand RequestCodeCommand { get; }
    public IAsyncRelayCommand ResetPasswordCommand { get; }

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

    private void ValidateCode()
    {
        if (!isValidationActive) return;

        if (string.IsNullOrWhiteSpace(code))
        {
            CodeError = "Verification code is required.";
        }
        else
        {
            CodeError = string.Empty;
        }
    }

    private void ValidatePassword()
    {
        if (!isValidationActive) return;

        if (string.IsNullOrEmpty(newPassword))
        {
            PasswordError = "New password is required.";
            return;
        }

        var errors = new List<string>();
        if (newPassword.Length < 12)
        {
            errors.Add("at least 12 characters");
        }
        if (!newPassword.Any(char.IsUpper))
        {
            errors.Add("one uppercase letter");
        }
        if (!newPassword.Any(char.IsLower))
        {
            errors.Add("one lowercase letter");
        }
        if (!newPassword.Any(char.IsDigit))
        {
            errors.Add("one digit");
        }
        if (!newPassword.Any(character => !char.IsLetterOrDigit(character)))
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

    public async Task RequestCodeAsync()
    {
        if (IsBusy) return;

        isValidationActive = true;
        ValidateEmail();

        if (HasEmailError)
        {
            GeneralError = "Please enter a valid email.";
            return;
        }

        GeneralError = string.Empty;

        try
        {
            IsBusy = true;
            var result = await authApi.ForgotPasswordAsync(new ForgotPasswordRequest(Email.Trim()));
            if (result.IsSuccess)
            {
                await Shell.Current.DisplayAlert("Demo Mode Reset Code", $"Reset code generated: {result.Code}\n\nIn a production app, this code would be sent to your email.", "OK");
                IsCodeSent = true;
                isValidationActive = false;
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

    public async Task ResetPasswordAsync()
    {
        if (IsBusy) return;

        isValidationActive = true;
        ValidateCode();
        ValidatePassword();

        if (HasCodeError || HasPasswordError)
        {
            GeneralError = "Please fix the validation errors below.";
            return;
        }

        GeneralError = string.Empty;

        try
        {
            IsBusy = true;
            var result = await authApi.ResetPasswordAsync(new ResetPasswordRequest(Email.Trim(), Code.Trim(), NewPassword));
            if (result.IsSuccess)
            {
                await Shell.Current.DisplayAlert("Success", "Your password has been reset successfully. Please log in.", "OK");
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
}
