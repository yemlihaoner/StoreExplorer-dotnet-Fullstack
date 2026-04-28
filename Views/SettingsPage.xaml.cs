using MyMAUIApp1.ViewModels;

namespace MyMAUIApp1.Views;

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
