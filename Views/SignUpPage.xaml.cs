using MyMAUIApp1.ViewModels;

namespace MyMAUIApp1.Views;

public partial class SignUpPage : ContentPage
{
    private readonly SignUpPageViewModel viewModel = new();

    public SignUpPage()
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
