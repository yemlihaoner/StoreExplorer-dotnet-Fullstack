using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreExplorer.Models;
using StoreExplorer.Services;

namespace StoreExplorer.ViewModels;

public sealed partial class StoreDetailViewModel : ObservableObject
{
    private readonly StoreApiService storeApi = ServiceRegistry.StoreApi;
    private readonly AuthSession session = ServiceRegistry.AuthSession;

    private StoreDto store = null!;
    private bool isBusy;
    private bool hasExistingReview;
    private bool isFavorite;
    private int rating = 4;
    private string comment = string.Empty;
    private string reviewStatusMessage = string.Empty;



    public StoreDto Store
    {
        get => store;
        set => SetProperty(ref store, value);
    }

    public ObservableCollection<MenuItemDto> Menu { get; } = new();
    public ObservableCollection<ReviewDto> Reviews { get; } = new();

    public int Rating
    {
        get => rating;
        set => SetProperty(ref rating, value);
    }

    public string Comment
    {
        get => comment;
        set => SetProperty(ref comment, value);
    }

    public bool IsBusy
    {
        get => isBusy;
        set => SetProperty(ref isBusy, value);
    }

    public bool IsAuthenticated => session.IsAuthenticated;

    public bool IsFavorite
    {
        get => isFavorite;
        set
        {
            if (SetProperty(ref isFavorite, value))
            {
                OnPropertyChanged(nameof(FavoriteButtonText));
            }
        }
    }

    public string FavoriteButtonText => IsFavorite ? "Remove from favorites" : "Add to favorites";

    public bool HasExistingReview
    {
        get => hasExistingReview;
        set
        {
            if (SetProperty(ref hasExistingReview, value))
            {
                OnPropertyChanged(nameof(CanLeaveReview));
                OnPropertyChanged(nameof(ReviewGuardMessage));
            }
        }
    }

    public bool CanLeaveReview => IsAuthenticated && !HasExistingReview;

    public string ReviewGuardMessage => HasExistingReview
        ? "You already submitted a review for this store."
        : string.Empty;

    public string ReviewStatusMessage
    {
        get => reviewStatusMessage;
        set => SetProperty(ref reviewStatusMessage, value);
    }

    public IRelayCommand<string> SetRatingCommand { get; }
    public IAsyncRelayCommand ToggleFavoriteCommand { get; }

    private void OnSessionChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(IsAuthenticated));
        OnPropertyChanged(nameof(CanLeaveReview));
        RefreshReviewState();
        _ = RefreshFavoriteStateAsync();
    }

    public IAsyncRelayCommand SubmitReviewCommand { get; }

    private async Task SubmitReviewAsyncImpl()
    {
        if (!IsAuthenticated)
        {
            // Caller (view) should show prompt; keep method safe
            ReviewStatusMessage = "Please sign in to leave a review.";
            return;
        }

        if (HasExistingReview)
        {
            ReviewStatusMessage = "You already submitted a review for this store.";
            return;
        }

        if (IsBusy)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(Comment))
        {
            ReviewStatusMessage = "Please add a short comment before submitting.";
            return;
        }

        try
        {
            IsBusy = true;
            var result = await storeApi.AddReviewAsync(Store.Id, Rating, Comment.Trim());
            ReviewStatusMessage = result.Message;
            if (result.IsSuccess)
            {
                // refresh store list and update local store data
                var stores = await storeApi.GetStoresAsync();
                var updated = stores.FirstOrDefault(s => s.Id == Store.Id);
                if (updated != null)
                {
                    Store = updated;
                    Reviews.Clear();
                    foreach (var r in Store.Reviews)
                    {
                        Reviews.Add(r);
                    }
                }

                Comment = string.Empty;
                Rating = 4;
                RefreshReviewState();
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    public IAsyncRelayCommand NavigateToLoginCommand { get; }
    public IAsyncRelayCommand NavigateToSignUpCommand { get; }

    private async Task NavigateToLoginAsyncImpl()
    {
        await Shell.Current.GoToAsync(nameof(StoreExplorer.Views.LoginPage));
    }

    private async Task NavigateToSignUpAsyncImpl()
    {
        await Shell.Current.GoToAsync(nameof(StoreExplorer.Views.SignUpPage));
    }

    public StoreDetailViewModel(StoreDto store)
    {
        Store = store;
        Menu.Clear();
        foreach (var m in store.Menu)
        {
            Menu.Add(m);
        }

        Reviews.Clear();
        foreach (var r in store.Reviews)
        {
            Reviews.Add(r);
        }

        RefreshReviewState();

        session.SessionChanged += OnSessionChanged;

        SetRatingCommand = new RelayCommand<string>(ratingValue =>
        {
            if (int.TryParse(ratingValue, out var parsedRating))
            {
                Rating = Math.Clamp(parsedRating, 1, 5);
            }
        });
        SubmitReviewCommand = new AsyncRelayCommand(SubmitReviewAsyncImpl);
        ToggleFavoriteCommand = new AsyncRelayCommand(ToggleFavoriteAsyncImpl);
        NavigateToLoginCommand = new AsyncRelayCommand(NavigateToLoginAsyncImpl);
        NavigateToSignUpCommand = new AsyncRelayCommand(NavigateToSignUpAsyncImpl);

        _ = RefreshFavoriteStateAsync();
    }

    private async Task ToggleFavoriteAsyncImpl()
    {
        if (!IsAuthenticated)
        {
            ReviewStatusMessage = "Please sign in to manage favorites.";
            return;
        }

        if (IsFavorite)
        {
            var removed = await ServiceRegistry.UserApi.RemoveFavoriteAsync(Store.Id);
            if (removed)
            {
                IsFavorite = false;
                ReviewStatusMessage = "Removed from favorites.";
            }

            return;
        }

        var added = await ServiceRegistry.UserApi.AddFavoriteAsync(Store.Id);
        if (added)
        {
            IsFavorite = true;
            ReviewStatusMessage = "Added to favorites.";
        }
    }

    private async Task RefreshFavoriteStateAsync()
    {
        if (!IsAuthenticated)
        {
            IsFavorite = false;
            return;
        }

        var favorites = await ServiceRegistry.UserApi.GetFavoriteStoresAsync();
        IsFavorite = favorites.Any(store => store.Id == Store.Id);
    }

    private void RefreshReviewState()
    {
        if (!IsAuthenticated)
        {
            HasExistingReview = false;
            return;
        }

        var currentUserName = session.User?.UserName;
        if (string.IsNullOrWhiteSpace(currentUserName))
        {
            HasExistingReview = false;
            return;
        }

        HasExistingReview = Reviews.Any(review =>
            string.Equals(review.Author, currentUserName, StringComparison.OrdinalIgnoreCase));
    }
}
