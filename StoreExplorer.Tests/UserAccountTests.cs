using Xunit;
using StoreExplorer.Backend.Services;
using StoreExplorer.Backend.Models;

namespace StoreExplorer.Tests;

public class UserAccountTests
{
    private readonly UserAccountService _accountService;

    public UserAccountTests()
    {
        _accountService = new UserAccountService();
    }

    [Fact]
    public void ValidatePassword_WithValidPassword_ReturnsNoErrors()
    {
        // Arrange
        var password = "StrongPassword123!";

        // Act
        var errors = UserAccountService.ValidatePassword(password);

        // Assert
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("short", "at least 12 characters")]
    [InlineData("nouppercase123!", "uppercase letter")]
    [InlineData("NOLOWERCASE123!", "lowercase letter")]
    [InlineData("NoDigitSymbol", "digit")]
    [InlineData("NoSymbol123", "symbol")]
    public void ValidatePassword_WithInvalidPassword_ReturnsExpectedErrors(string password, string expectedErrorSubstr)
    {
        // Act
        var errors = UserAccountService.ValidatePassword(password);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, err => err.Contains(expectedErrorSubstr, System.StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TryCreateUser_WithValidRequest_Succeeds()
    {
        // Arrange
        var request = new SignUpRequest("test@example.com", "testuser", "StrongPassword123!");

        // Act
        var result = _accountService.TryCreateUser(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.NotNull(result.User);
        Assert.Equal("test@example.com", result.User.Email);
        Assert.Equal("testuser", result.User.UserName);
    }

    [Fact]
    public void TryCreateUser_WithDuplicateEmail_Fails()
    {
        // Arrange
        var request1 = new SignUpRequest("dup@example.com", "user1", "StrongPassword123!");
        var request2 = new SignUpRequest("dup@example.com", "user2", "StrongPassword123!");

        _accountService.TryCreateUser(request1);

        // Act
        var result = _accountService.TryCreateUser(request2);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("already exists", result.Error, System.StringComparison.OrdinalIgnoreCase);
        Assert.Null(result.User);
    }

    [Fact]
    public void TryValidateCredentials_WithCorrectPassword_Succeeds()
    {
        // Arrange
        var signUp = new SignUpRequest("login@example.com", "loginuser", "StrongPassword123!");
        _accountService.TryCreateUser(signUp);

        var login = new LoginRequest("login@example.com", "StrongPassword123!");

        // Act
        var result = _accountService.TryValidateCredentials(login);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.NotNull(result.User);
        Assert.False(result.IsLockedOut);
    }

    [Fact]
    public void TryValidateCredentials_WithWrongPassword_Fails()
    {
        // Arrange
        var signUp = new SignUpRequest("wrongpass@example.com", "wrongpass", "StrongPassword123!");
        _accountService.TryCreateUser(signUp);

        var login = new LoginRequest("wrongpass@example.com", "WrongPassword123!");

        // Act
        var result = _accountService.TryValidateCredentials(login);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid email or password.", result.Error);
        Assert.Null(result.User);
    }

    [Fact]
    public void TryValidateCredentials_AfterMultipleFailedAttempts_LocksOut()
    {
        // Arrange
        var signUp = new SignUpRequest("lockout@example.com", "lockout", "StrongPassword123!");
        _accountService.TryCreateUser(signUp);

        var login = new LoginRequest("lockout@example.com", "WrongPassword123!");

        // Act & Assert failed attempts (Max is 5)
        for (int i = 0; i < 4; i++)
        {
            var res = _accountService.TryValidateCredentials(login);
            Assert.False(res.IsSuccess);
            Assert.False(res.IsLockedOut);
        }

        // 5th attempt locks the account
        var finalResult = _accountService.TryValidateCredentials(login);
        Assert.False(finalResult.IsSuccess);
        Assert.True(finalResult.IsLockedOut);
        Assert.Contains("temporarily locked", finalResult.Error, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FavoriteFunctionality_AddFavorite_Succeeds()
    {
        // Arrange
        var signUp = new SignUpRequest("fav@example.com", "favuser", "StrongPassword123!");
        var userResult = _accountService.TryCreateUser(signUp);
        var userId = userResult.User.Id;
        var storeId = System.Guid.NewGuid();

        // Act
        var addFavResult = _accountService.AddFavorite(userId, storeId);
        var profile = _accountService.GetProfile(userId);

        // Assert
        Assert.True(addFavResult);
        Assert.NotNull(profile);
        Assert.Contains(storeId, profile.FavoriteStoreIds);
    }

    [Fact]
    public void PasswordReset_InitiateAndReset_Succeeds()
    {
        // Arrange
        var signUp = new SignUpRequest("reset@example.com", "resetuser", "StrongPassword123!");
        var userResult = _accountService.TryCreateUser(signUp);
        var email = signUp.Email;

        // Act 1: Initiate reset
        var initiateResult = _accountService.InitiatePasswordReset(email);
        Assert.True(initiateResult.IsSuccess);
        Assert.NotNull(initiateResult.Code);

        // Act 2: Reset with correct code and new password
        var newPassword = "NewStrongPassword456!";
        var resetResult = _accountService.TryResetPassword(email, initiateResult.Code, newPassword, out var error);

        // Assert
        Assert.True(resetResult);
        Assert.Null(error);

        // Verify credentials with new password
        var loginResult = _accountService.TryValidateCredentials(new LoginRequest(email, newPassword));
        Assert.True(loginResult.IsSuccess);
    }

    [Fact]
    public void PasswordReset_WithWrongCode_Fails()
    {
        // Arrange
        var signUp = new SignUpRequest("resetwrong@example.com", "resetwrong", "StrongPassword123!");
        _accountService.TryCreateUser(signUp);
        var email = signUp.Email;

        _accountService.InitiatePasswordReset(email);

        // Act
        var resetResult = _accountService.TryResetPassword(email, "WrongCode", "NewStrongPassword456!", out var error);

        // Assert
        Assert.False(resetResult);
        Assert.Equal("Invalid or expired reset code.", error);
    }
}
