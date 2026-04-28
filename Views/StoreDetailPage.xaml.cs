using MyMAUIApp1.Models;
using Microsoft.Maui.Controls;

namespace MyMAUIApp1.Views;

public partial class StoreDetailPage : ContentPage
{
    private readonly MyMAUIApp1.ViewModels.StoreDetailViewModel viewModel;

    public StoreDetailPage(StoreDto store)
    {
        InitializeComponent();
        viewModel = new MyMAUIApp1.ViewModels.StoreDetailViewModel(store);
        BindingContext = viewModel;
        Title = viewModel.Store.Name;
    }
}

