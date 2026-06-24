using StoreExplorer.ViewModels;

namespace StoreExplorer.Views;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsPageViewModel viewModel = new();

    public SettingsPage()
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        viewModel.RefreshState();
    }
}
