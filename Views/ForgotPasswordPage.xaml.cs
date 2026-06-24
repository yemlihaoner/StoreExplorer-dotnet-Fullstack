using StoreExplorer.ViewModels;

namespace StoreExplorer.Views;

public partial class ForgotPasswordPage : ContentPage
{
    private readonly ForgotPasswordPageViewModel viewModel = new();

    public ForgotPasswordPage()
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
