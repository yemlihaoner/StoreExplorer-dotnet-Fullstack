using StoreExplorer.ViewModels;

namespace StoreExplorer.Views;

public partial class SignUpPage : ContentPage
{
    private readonly SignUpPageViewModel viewModel = new();

    public SignUpPage()
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
