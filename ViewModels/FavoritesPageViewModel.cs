using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyMAUIApp1.Models;
using MyMAUIApp1.Services;

namespace MyMAUIApp1.ViewModels;

public sealed partial class FavoritesPageViewModel : ObservableObject
{
    private readonly UserApiService userApi = ServiceRegistry.UserApi;
    private readonly AuthSession session = ServiceRegistry.AuthSession;

    private bool isBusy;
    private string statusMessage = "Load favorites to view saved stores.";

    public FavoritesPageViewModel()
    {
        session.SessionChanged += OnSessionChanged;
        RemoveFavoriteCommand = new AsyncRelayCommand<StoreDto?>(RemoveFavoriteAsync);
        OpenStoreDetailCommand = new AsyncRelayCommand<StoreDto?>(OpenStoreDetailAsync);
        NavigateToLoginCommand = new AsyncRelayCommand(NavigateToLoginAsync);
        NavigateToSignUpCommand = new AsyncRelayCommand(NavigateToSignUpAsync);
    }

    public ObservableCollection<StoreDto> FavoriteStores { get; } = new();

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

    public IAsyncRelayCommand<StoreDto?> RemoveFavoriteCommand { get; }
    public IAsyncRelayCommand<StoreDto?> OpenStoreDetailCommand { get; }
    public IAsyncRelayCommand NavigateToLoginCommand { get; }
    public IAsyncRelayCommand NavigateToSignUpCommand { get; }

    public async Task LoadAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (!session.IsAuthenticated)
        {
            FavoriteStores.Clear();
            StatusMessage = "Sign in to view favorites.";
            return;
        }

        try
        {
            IsBusy = true;
            var stores = await userApi.GetFavoriteStoresAsync();
            FavoriteStores.Clear();
            foreach (var store in stores)
            {
                FavoriteStores.Add(store);
            }

            StatusMessage = FavoriteStores.Count == 0
                ? "No favorites yet. Save stores from the home page."
                : $"Loaded {FavoriteStores.Count} favorite stores.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public bool IsAuthenticated => session.IsAuthenticated;

    private void OnSessionChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(IsAuthenticated));
    }

    public async Task RemoveFavoriteAsync(StoreDto? store)
    {
        if (store is null)
        {
            return;
        }

        var removed = await userApi.RemoveFavoriteAsync(store.Id);
        if (removed)
        {
            FavoriteStores.Remove(store);
            StatusMessage = $"Removed {store.Name} from favorites.";
        }
        else
        {
            StatusMessage = "Could not remove favorite store.";
        }
    }

    private async Task OpenStoreDetailAsync(StoreDto? selectedStore)
    {
        if (selectedStore is null)
        {
            return;
        }

        await Shell.Current.Navigation.PushAsync(new Views.StoreDetailPage(selectedStore));
    }

    private static async Task NavigateToLoginAsync()
    {
        await Shell.Current.GoToAsync(nameof(Views.LoginPage));
    }

    private static async Task NavigateToSignUpAsync()
    {
        await Shell.Current.GoToAsync(nameof(Views.SignUpPage));
    }
}
