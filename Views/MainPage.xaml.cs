using StoreExplorer.ViewModels;

namespace StoreExplorer.Views;

public partial class MainPage : ContentPage
{
    private readonly MainPageViewModel viewModel = new();

    public MainPage()
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
