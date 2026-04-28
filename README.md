# Store Explorer

A .NET MAUI sample app with MVVM architecture, theme support, favorites, reviews, and simple account flows backed by a local minimal API backend in the same solution.

## Features

- MVVM-based UI for all main pages
- Store explorer with nearby-store ranking
- Store detail page with menu, reviews, and review submission
- Favorites add/remove toggle with persisted backend state
- Login, sign-up, profile, and settings flows
- Light and dark theme support with responsive layouts for smaller screens

## Solution Layout

- `Views/` - MAUI pages
- `ViewModels/` - page viewmodels
- `Models/` - shared client DTOs and UI models
- `Services/` - API access and session state
- `Backend/` - local ASP.NET Core minimal API used by the app

## Requirements

- .NET 10 SDK
- Visual Studio 2022 with .NET MAUI workload, or VS Code with the MAUI tooling installed
- Platform SDKs for the targets you plan to run

## Build

```powershell
dotnet build MyMAUIApp1.csproj -f net10.0-windows10.0.19041.0 -p:UseAppHost=false
```

## Run

Open the solution in Visual Studio and choose the target platform, or run the desired target from the command line once the platform SDK is installed.

## Notes

- The app uses a shared semantic color palette in `Resources/Styles`.
- Light mode uses softer surfaces and stronger text contrast for better readability.
- The Android target may require an installed API level that matches the project configuration.
