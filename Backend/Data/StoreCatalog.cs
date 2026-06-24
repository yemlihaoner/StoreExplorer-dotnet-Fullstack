using StoreExplorer.Backend.Models;
using System.Collections.Concurrent;

namespace StoreExplorer.Backend.Data;

public static class StoreCatalog
{
    private static readonly ConcurrentDictionary<Guid, List<ReviewDto>> AdditionalReviewsByStoreId = new();

    private static readonly IReadOnlyList<StoreDto> Stores = new[]
    {
        new StoreDto(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Harbor Coffee",
            "A bright cafe near the water with espresso, pastries, and breakfast bowls.",
            new StoreLocationDto("12 Dockside Ave, Seattle, WA", 47.6062, -122.3321),
            new[]
            {
                new MenuItemDto("Espresso", "Double shot pulled fresh.", 3.50m, "Coffee"),
                new MenuItemDto("Latte", "Whole milk with a silky foam cap.", 4.75m, "Coffee"),
                new MenuItemDto("Blueberry Muffin", "Baked daily with lemon glaze.", 3.25m, "Bakery")
            },
            new[]
            {
                new ReviewDto("Maya", 5, "Great coffee and a clean place to work.", new DateOnly(2026, 4, 12)),
                new ReviewDto("Jordan", 4, "Fast service and good pastries.", new DateOnly(2026, 4, 8))
            }),
        new StoreDto(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "Summit Market",
            "Neighborhood grocery with sandwiches, salads, and a compact hot bar.",
            new StoreLocationDto("88 Pine St, Seattle, WA", 47.6101, -122.3354),
            new[]
            {
                new MenuItemDto("Turkey Sandwich", "House-roasted turkey with herb mayo.", 8.95m, "Lunch"),
                new MenuItemDto("Garden Salad", "Greens, cucumber, tomato, and vinaigrette.", 7.25m, "Lunch"),
                new MenuItemDto("Cold Brew", "Smooth cold brew served over ice.", 4.25m, "Coffee")
            },
            new[]
            {
                new ReviewDto("Alyssa", 5, "Perfect quick stop for lunch.", new DateOnly(2026, 4, 9)),
                new ReviewDto("Ben", 4, "Good selection and easy to navigate.", new DateOnly(2026, 4, 5))
            }),
        new StoreDto(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "Northline Books",
            "Bookstore cafe with reading nooks, tea, and a quiet afternoon vibe.",
            new StoreLocationDto("230 Library Way, Bellevue, WA", 47.6105, -122.2015),
            new[]
            {
                new MenuItemDto("Matcha Latte", "Ceremonial grade matcha with oat milk.", 5.50m, "Tea"),
                new MenuItemDto("Chamomile Tea", "Loose leaf steeped to order.", 3.95m, "Tea"),
                new MenuItemDto("Cheesecake Slice", "Creamy slice with berry compote.", 6.25m, "Dessert")
            },
            new[]
            {
                new ReviewDto("Renee", 5, "Feels calm and easy to spend an hour in.", new DateOnly(2026, 4, 14)),
                new ReviewDto("Chris", 4, "Loved the tea selection.", new DateOnly(2026, 4, 11))
            }),
        new StoreDto(
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            "Pixel Pantry",
            "Late-night snacks, energy drinks, and toasted breakfast sandwiches.",
            new StoreLocationDto("540 Tech Rd, Redmond, WA", 47.6740, -122.1215),
            new[]
            {
                new MenuItemDto("Breakfast Sandwich", "Egg, cheddar, and bacon on brioche.", 6.95m, "Breakfast"),
                new MenuItemDto("Energy Drink", "Citrus zero-sugar can.", 3.75m, "Beverage"),
                new MenuItemDto("Trail Mix", "Almonds, cashews, cranberries, and chocolate.", 4.50m, "Snacks")
            },
            new[]
            {
                new ReviewDto("Sam", 5, "Saved me on a late study night.", new DateOnly(2026, 4, 16)),
                new ReviewDto("Priya", 4, "Quick stop with surprisingly good sandwiches.", new DateOnly(2026, 4, 10))
            })
    };

    public static IReadOnlyList<StoreDto> GetStores() => Stores.Select(MergeAdditionalReviews).ToArray();

    public static StoreDto? GetStoreById(Guid storeId)
    {
        var store = Stores.FirstOrDefault(candidate => candidate.Id == storeId);
        return store is null ? null : MergeAdditionalReviews(store);
    }

    public static IReadOnlyList<StoreDto> GetStoresByIds(IEnumerable<Guid> storeIds)
    {
        var idSet = storeIds.ToHashSet();
        return Stores
            .Where(store => idSet.Contains(store.Id))
            .Select(MergeAdditionalReviews)
            .ToArray();
    }

    public static bool AddReview(Guid storeId, ReviewDto review)
    {
        if (Stores.All(store => store.Id != storeId))
        {
            return false;
        }

        var reviewList = AdditionalReviewsByStoreId.GetOrAdd(storeId, _ => []);
        lock (reviewList)
        {
            reviewList.Add(review);
        }

        return true;
    }

    public static IReadOnlyList<StoreDto> GetNearbyStores(double latitude, double longitude, double radiusKm)
    {
        return GetStores()
            .Select(store => new
            {
                Store = store,
                Distance = DistanceInKilometers(latitude, longitude, store.Location.Latitude, store.Location.Longitude)
            })
            .Where(candidate => candidate.Distance <= radiusKm)
            .OrderBy(candidate => candidate.Distance)
            .Select(candidate => candidate.Store)
            .ToArray();
    }

    private static double DistanceInKilometers(double startLatitude, double startLongitude, double endLatitude, double endLongitude)
    {
        const double earthRadiusKm = 6371.0;

        var latitudeDelta = DegreesToRadians(endLatitude - startLatitude);
        var longitudeDelta = DegreesToRadians(endLongitude - startLongitude);

        var a = Math.Pow(Math.Sin(latitudeDelta / 2), 2)
            + Math.Cos(DegreesToRadians(startLatitude)) * Math.Cos(DegreesToRadians(endLatitude))
            * Math.Pow(Math.Sin(longitudeDelta / 2), 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusKm * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;

    private static StoreDto MergeAdditionalReviews(StoreDto store)
    {
        if (!AdditionalReviewsByStoreId.TryGetValue(store.Id, out var additionalReviews))
        {
            return store;
        }

        lock (additionalReviews)
        {
            var mergedReviews = store.Reviews
                .Concat(additionalReviews)
                .OrderByDescending(review => review.VisitDate)
                .ToArray();

            return store with { Reviews = mergedReviews };
        }
    }
}