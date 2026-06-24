using StoreExplorer.ViewModels;

namespace StoreExplorer.Views;

public partial class LoginPage : ContentPage
{
    private readonly LoginPageViewModel viewModel = new();

    public LoginPage()
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
