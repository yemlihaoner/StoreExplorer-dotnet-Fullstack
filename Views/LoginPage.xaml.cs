using MyMAUIApp1.ViewModels;

namespace MyMAUIApp1.Views;

public partial class LoginPage : ContentPage
{
    private readonly LoginPageViewModel viewModel = new();

    public LoginPage()
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
