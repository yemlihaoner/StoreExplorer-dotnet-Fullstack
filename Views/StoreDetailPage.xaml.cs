using StoreExplorer.Models;
using Microsoft.Maui.Controls;

namespace StoreExplorer.Views;

public partial class StoreDetailPage : ContentPage
{
    private readonly StoreExplorer.ViewModels.StoreDetailViewModel viewModel;

    public StoreDetailPage(StoreDto store)
    {
        InitializeComponent();
        viewModel = new StoreExplorer.ViewModels.StoreDetailViewModel(store);
        BindingContext = viewModel;
        Title = viewModel.Store.Name;
    }
}

