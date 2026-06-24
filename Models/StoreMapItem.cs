using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Devices.Sensors;

namespace StoreExplorer.Models;

public sealed partial class StoreMapItem : ObservableObject
{
    public StoreMapItem(StoreDto store, double distanceKm)
    {
        Store = store;
        DistanceKm = distanceKm;
    }

    public StoreDto Store { get; }

    public double DistanceKm { get; }

    [ObservableProperty]
    private bool isFavorite;

    public Location Location => new(Store.Location.Latitude, Store.Location.Longitude);

    public string DistanceText => double.IsNaN(DistanceKm) ? "Distance unavailable" : $"{DistanceKm:0.0} km away";

    public string FavoriteButtonText => IsFavorite ? "Remove from favorites" : "Add to favorites";

    partial void OnIsFavoriteChanged(bool value)
    {
        OnPropertyChanged(nameof(FavoriteButtonText));
    }
}