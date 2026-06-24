using StoreExplorer.ViewModels;

namespace StoreExplorer.Views;

public partial class ProfilePage : ContentPage
{
    private readonly ProfilePageViewModel viewModel = new();

    public ProfilePage()
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
