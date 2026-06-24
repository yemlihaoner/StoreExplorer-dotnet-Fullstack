using StoreExplorer.ViewModels;

namespace StoreExplorer.Views;

public partial class FavoritesPage : ContentPage
{
    private readonly FavoritesPageViewModel viewModel = new();

    public FavoritesPage()
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.LoadAsync();
    }
}
