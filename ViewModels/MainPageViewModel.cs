using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Devices.Sensors;
using MyMAUIApp1.Models;
using MyMAUIApp1.Services;
using MyMAUIApp1.Views;

namespace MyMAUIApp1.ViewModels;

public sealed partial class MainPageViewModel : ObservableObject
{
    private readonly StoreApiService storeApiService = ServiceRegistry.StoreApi;
    private readonly UserApiService userApiService = ServiceRegistry.UserApi;
    private readonly AuthSession authSession = ServiceRegistry.AuthSession;

    private bool isBusy;
    private string statusMessage = "Loading stores...";
    private Location? currentLocation;

    public MainPageViewModel()
    {
        authSession.SessionChanged += OnSessionChanged;

        OpenStoreCommand = new AsyncRelayCommand<StoreMapItem?>(OpenStoreAsync);
        ToggleFavoriteCommand = new AsyncRelayCommand<StoreMapItem?>(ToggleFavoriteAsync);
        LoginCommand = new AsyncRelayCommand(NavigateToLoginAsync);
        SignUpCommand = new AsyncRelayCommand(NavigateToSignUpAsync);
        ProfileCommand = new AsyncRelayCommand(NavigateToProfileAsync);
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

    public Location? CurrentLocation
    {
        get => currentLocation;
        set => SetProperty(ref currentLocation, value);
    }

    public ObservableCollection<StoreMapItem> NearbyStores { get; } = new();

    public bool IsAuthenticated => authSession.IsAuthenticated;

    public IAsyncRelayCommand<StoreMapItem?> OpenStoreCommand { get; }
    public IAsyncRelayCommand<StoreMapItem?> ToggleFavoriteCommand { get; }
    public IAsyncRelayCommand LoginCommand { get; }
    public IAsyncRelayCommand SignUpCommand { get; }
    public IAsyncRelayCommand ProfileCommand { get; }

    private async void OnSessionChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(IsAuthenticated));
        await RefreshFavoriteStatesAsync();
    }

    public async Task LoadAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = "Looking up your location and nearby stores...";

            var location = await TryGetCurrentLocationAsync();
            var stores = location is null
                ? await storeApiService.GetStoresAsync()
                : await storeApiService.GetNearbyStoresAsync(location.Latitude, location.Longitude, 100);

            var origin = location ?? GetFallbackOrigin(stores);
            CurrentLocation = origin;

            var items = stores
                .Select(store => new StoreMapItem(store, origin is null ? double.NaN : DistanceInKilometers(origin, new Location(store.Location.Latitude, store.Location.Longitude))))
                .OrderBy(store => store.DistanceKm)
                .ToList();

            NearbyStores.Clear();
            foreach (var store in items)
            {
                NearbyStores.Add(store);
            }

            await RefreshFavoriteStatesAsync();

            StatusMessage = NearbyStores.Count == 0
                ? "No stores were returned by the backend."
                : location is null
                    ? "Location access was unavailable, so stores are ranked from the first backend result."
                    : $"Found {NearbyStores.Count} nearby stores.";

            OnPropertyChanged(nameof(IsAuthenticated));
        }
        catch (Exception exception)
        {
            StatusMessage = $"Unable to load stores: {exception.Message}";
            NearbyStores.Clear();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task OpenStoreAsync(StoreMapItem? store)
    {
        if (store is null)
        {
            return;
        }

        await Shell.Current.Navigation.PushAsync(new StoreDetailPage(store.Store));
    }

    private async Task ToggleFavoriteAsync(StoreMapItem? store)
    {
        if (store is null)
        {
            return;
        }

        if (!authSession.IsAuthenticated)
        {
            StatusMessage = "Login required to save favorites.";
            return;
        }

        if (store.IsFavorite)
        {
            var removed = await userApiService.RemoveFavoriteAsync(store.Store.Id);
            if (removed)
            {
                store.IsFavorite = false;
                StatusMessage = $"Removed {store.Store.Name} from favorites.";
            }
            else
            {
                StatusMessage = "Could not remove favorite.";
            }

            return;
        }

        var added = await userApiService.AddFavoriteAsync(store.Store.Id);
        if (added)
        {
            store.IsFavorite = true;
            StatusMessage = $"Saved {store.Store.Name} to favorites.";
        }
        else
        {
            StatusMessage = "Could not save favorite.";
        }
    }

    private async Task RefreshFavoriteStatesAsync()
    {
        if (!authSession.IsAuthenticated)
        {
            foreach (var store in NearbyStores)
            {
                store.IsFavorite = false;
            }

            return;
        }

        var favoriteStores = await userApiService.GetFavoriteStoresAsync();
        var favoriteIds = favoriteStores.Select(store => store.Id).ToHashSet();
        foreach (var store in NearbyStores)
        {
            store.IsFavorite = favoriteIds.Contains(store.Store.Id);
        }
    }

    private async Task NavigateToLoginAsync()
    {
        await Shell.Current.GoToAsync(nameof(LoginPage));
    }

    private async Task NavigateToSignUpAsync()
    {
        await Shell.Current.GoToAsync(nameof(SignUpPage));
    }

    private async Task NavigateToProfileAsync()
    {
        await Shell.Current.GoToAsync("//ProfilePage");
    }

    private static async Task<Location?> TryGetCurrentLocationAsync()
    {
        try
        {
            var cachedLocation = await Geolocation.Default.GetLastKnownLocationAsync();
            if (cachedLocation is not null)
            {
                return cachedLocation;
            }

            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
            return await Geolocation.Default.GetLocationAsync(request);
        }
        catch
        {
            return null;
        }
    }

    private static Location? GetFallbackOrigin(IReadOnlyList<StoreDto> stores)
    {
        var firstStore = stores.FirstOrDefault();
        return firstStore is null
            ? null
            : new Location(firstStore.Location.Latitude, firstStore.Location.Longitude);
    }

    private static double DistanceInKilometers(Location origin, Location destination)
    {
        const double earthRadiusKm = 6371.0;

        var latitudeDelta = DegreesToRadians(destination.Latitude - origin.Latitude);
        var longitudeDelta = DegreesToRadians(destination.Longitude - origin.Longitude);

        var a = Math.Pow(Math.Sin(latitudeDelta / 2), 2)
            + Math.Cos(DegreesToRadians(origin.Latitude)) * Math.Cos(DegreesToRadians(destination.Latitude))
            * Math.Pow(Math.Sin(longitudeDelta / 2), 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusKm * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}