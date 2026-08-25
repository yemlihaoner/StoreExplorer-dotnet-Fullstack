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

- **.NET 10 SDK**
- **IDE**: [VS Code](https://code.visualstudio.com/) or Visual Studio 2022
- **VS Code Extensions**:
  - [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) (highly recommended for solution navigation and debugger support)
  - [.NET MAUI](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-maui) (for debugging on Android, iOS, and MacCatalyst)
- Platform SDKs for the targets you plan to run (e.g., Windows SDK, Android SDK)

## Running the Application from VS Code

We have preconfigured launch settings in `.vscode/launch.json` and tasks in `.vscode/tasks.json`. You can easily run and debug the projects from VS Code's **Run and Debug** view (`Ctrl+Shift+D`):

1. **Launch Backend + MAUI App (macOS Desktop + Browser)** (Recommended for macOS):
   - One-click launch for local development on macOS.
   - Starts the backend and opens its URL in your browser.
   - Starts the MAUI desktop app with `dotnet run -f net10.0-maccatalyst`.
2. **Launch Backend + MAUI App (Windows)** (Recommended for Windows Users):
   - Launches the API backend and the MAUI Windows desktop application simultaneously.
3. **Backend (.NET Core)**:
   - Runs/debugs only the ASP.NET Core minimal API backend.
4. **MAUI App (macOS Catalyst CLI)**:
   - Runs the MAUI app directly on macOS Catalyst using CLI.
5. **MAUI App (Windows)**:
   - Runs/debugs only the client app targeting Windows desktop.


## Running the Application from CLI

You can also run both the backend and the app using the `dotnet` CLI.

### 1. Run the Backend API
```powershell
dotnet run --project Backend/StoreExplorer.Backend.csproj
```
The API will start listening on `http://localhost:5271`.

### 2. Run the MAUI Client App
Run the app targeting your preferred platform:
- **Windows**:
  ```powershell
  dotnet run --project StoreExplorer.csproj -f net10.0-windows10.0.19041.0
  ```
- **Android**:
  ```powershell
  dotnet run --project StoreExplorer.csproj -f net10.0-android
  ```
- **macOS (Catalyst)**:
  ```powershell
  dotnet run --project StoreExplorer.csproj -f net10.0-maccatalyst
  ```
- **iOS**:
  ```powershell
  dotnet run --project StoreExplorer.csproj -f net10.0-ios
  ```


## CI/CD, Tests & Push Validation

To ensure only successfully compiling and tested code is pushed to your remote repository, we have set up two guards:

### 1. GitHub Actions Pipeline (Remote Validation)
A build pipeline is configured in [.github/workflows/build.yml](/Users/yemliha/Development/StoreExplorer-dotnet-Fullstack/.github/workflows/build.yml). On every `push` and `pull_request` to the main branches, the workflow executes:
* **Linux Runner**: Automatically restores, runs the xUnit test suite (14 tests covering Login, Password Reset, duplicate registrations, and Favorites validation), and verifies the compiling of the Backend API.
* **Windows Runner**: Automatically restores, installs the required .NET MAUI workloads, and verifies compiling of the client app targeting the Windows platform.

### 2. Local Push Guard (Git Pre-Push Hook)
To prevent pushing failing code, a Git pre-push hook script is stored in [.githooks/pre-push](/Users/yemliha/Development/StoreExplorer-dotnet-Fullstack/.githooks/pre-push).

This hook is configured **automatically** for all developers. The build system in `StoreExplorer.csproj` automatically registers this folder with Git (`git config core.hooksPath .githooks`) whenever the project is built.

Once configured, running `git push` automatically:
1. Runs the xUnit test suite (implicitly validating Backend compilation).
2. Verifies client compilation targeting Windows.
If any tests or builds fail, the push is aborted.


## Notes

- The app uses a shared semantic color palette in `Resources/Styles`.
- Light mode uses softer surfaces and stronger text contrast for better readability.
- The Android target may require an installed API level that matches the project configuration.
