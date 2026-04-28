using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyMAUIApp1.Models;
using MyMAUIApp1.Services;
using Microsoft.Maui.Storage;
using Microsoft.Maui;
using MyMAUIApp1.Views;

namespace MyMAUIApp1.ViewModels;

public sealed partial class SettingsPageViewModel : ObservableObject
{
    private readonly UserApiService userApi = ServiceRegistry.UserApi;
    private readonly AuthSession session = ServiceRegistry.AuthSession;

    private string currentPassword = string.Empty;
    private string newPassword = string.Empty;
    private bool isBusy;
    private string statusMessage = "Change password from here.";

    public SettingsPageViewModel()
    {
        session.SessionChanged += OnSessionChanged;
        ChangePasswordCommand = new AsyncRelayCommand(ChangePasswordAsync);
        NavigateToLoginCommand = new AsyncRelayCommand(NavigateToLoginAsync);
        NavigateToSignUpCommand = new AsyncRelayCommand(NavigateToSignUpAsync);
    }

    public string CurrentPassword
    {
        get => currentPassword;
        set => SetProperty(ref currentPassword, value);
    }

    public string NewPassword
    {
        get => newPassword;
        set => SetProperty(ref newPassword, value);
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

    public IAsyncRelayCommand ChangePasswordCommand { get; }
    public IAsyncRelayCommand NavigateToLoginCommand { get; }
    public IAsyncRelayCommand NavigateToSignUpCommand { get; }

    private const string ThemePrefKey = "user_theme_is_dark";

    public bool IsAuthenticated => session.IsAuthenticated;

    public bool IsDarkTheme
    {
        get => Preferences.Get(ThemePrefKey, Application.Current?.RequestedTheme == AppTheme.Dark);
        set
        {
            Preferences.Set(ThemePrefKey, value);
            var app = Application.Current;
            if (app != null)
            {
                app.UserAppTheme = value ? AppTheme.Dark : AppTheme.Light;
            }
            OnPropertyChanged();
        }
    }

    public void RefreshState()
    {
        OnPropertyChanged(nameof(IsAuthenticated));
        OnPropertyChanged(nameof(IsDarkTheme));
    }

    private void OnSessionChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(IsAuthenticated));
    }

    public async Task ChangePasswordAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (!session.IsAuthenticated)
        {
            StatusMessage = "You must be signed in to change your password.";
            return;
        }

        try
        {
            IsBusy = true;
            var result = await userApi.ChangePasswordAsync(new ChangePasswordRequest(CurrentPassword, NewPassword));
            StatusMessage = result.Message;
            if (result.IsSuccess)
            {
                CurrentPassword = string.Empty;
                NewPassword = string.Empty;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static async Task NavigateToLoginAsync()
    {
        await Shell.Current.GoToAsync(nameof(LoginPage));
    }

    private static async Task NavigateToSignUpAsync()
    {
        await Shell.Current.GoToAsync(nameof(SignUpPage));
    }
}
