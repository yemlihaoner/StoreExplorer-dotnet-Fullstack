using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreExplorer.Models;
using StoreExplorer.Services;
using StoreExplorer.Views;

namespace StoreExplorer.ViewModels;

public sealed partial class ProfilePageViewModel : ObservableObject
{
    private readonly AuthSession session = ServiceRegistry.AuthSession;
    private readonly UserApiService userApi = ServiceRegistry.UserApi;

    private bool isBusy;
    private string statusMessage = "Load your profile to view favorites and recent visits.";
    private string userName = "Guest";
    private string email = "Not signed in";

    public ProfilePageViewModel()
    {
        session.SessionChanged += OnSessionChanged;
        NavigateToFavoritesCommand = new AsyncRelayCommand(NavigateToFavoritesAsync);
        NavigateToSettingsCommand = new AsyncRelayCommand(NavigateToSettingsAsync);
        NavigateToLoginCommand = new AsyncRelayCommand(NavigateToLoginAsync);
        NavigateToSignUpCommand = new AsyncRelayCommand(NavigateToSignUpAsync);
        LogoutCommand = new RelayCommand(Logout);
    }

    public ObservableCollection<UserVisitDto> RecentVisits { get; } = new();

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

    public string UserName
    {
        get => userName;
        set => SetProperty(ref userName, value);
    }

    public string Email
    {
        get => email;
        set => SetProperty(ref email, value);
    }

    public bool IsAuthenticated => session.IsAuthenticated;

    public IAsyncRelayCommand NavigateToFavoritesCommand { get; }
    public IAsyncRelayCommand NavigateToSettingsCommand { get; }
    public IAsyncRelayCommand NavigateToLoginCommand { get; }
    public IAsyncRelayCommand NavigateToSignUpCommand { get; }
    public IRelayCommand LogoutCommand { get; }

    private void OnSessionChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(IsAuthenticated));
    }

    public async Task LoadAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (!session.IsAuthenticated)
        {
            UserName = "Guest";
            Email = "Not signed in";
            RecentVisits.Clear();
            StatusMessage = "You are not signed in.";
            OnPropertyChanged(nameof(IsAuthenticated));
            return;
        }

        try
        {
            IsBusy = true;
            var profile = await userApi.GetProfileAsync();
            if (profile is null)
            {
                StatusMessage = "Unable to load profile.";
                return;
            }

            UserName = profile.UserName;
            Email = profile.Email;

            RecentVisits.Clear();
            foreach (var visit in profile.RecentVisits)
            {
                RecentVisits.Add(visit);
            }

            StatusMessage = profile.RecentVisits.Count == 0
                ? "No recent visits yet. Leave a review from a store detail card."
                : $"Loaded {profile.RecentVisits.Count} recent visits.";
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(IsAuthenticated));
        }
    }

    public void Logout()
    {
        ServiceRegistry.AuthApi.Logout();
        UserName = "Guest";
        Email = "Not signed in";
        RecentVisits.Clear();
        StatusMessage = "You have been signed out.";
        OnPropertyChanged(nameof(IsAuthenticated));
    }

    private static async Task NavigateToFavoritesAsync()
    {
        await Shell.Current.GoToAsync(nameof(FavoritesPage));
    }

    private static async Task NavigateToSettingsAsync()
    {
        await Shell.Current.GoToAsync(nameof(SettingsPage));
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
