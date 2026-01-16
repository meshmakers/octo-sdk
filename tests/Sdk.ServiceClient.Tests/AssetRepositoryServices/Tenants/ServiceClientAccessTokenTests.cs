using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants;

namespace Sdk.ServiceClient.Tests.AssetRepositoryServices.Tenants;

public class ServiceClientAccessTokenTests
{
    [Fact]
    public void AccessToken_InitialValue_IsNull()
    {
        // Arrange & Act
        var sut = new ServiceClientAccessToken();

        // Assert
        Assert.Null(sut.AccessToken);
    }

    [Fact]
    public void AccessToken_SetValue_ReturnsSetValue()
    {
        // Arrange
        var sut = new ServiceClientAccessToken();
        const string expectedToken = "test-token-123";

        // Act
        sut.AccessToken = expectedToken;

        // Assert
        Assert.Equal(expectedToken, sut.AccessToken);
    }

    [Fact]
    public void AccessToken_SetDifferentValue_RaisesAccessTokenUpdatedEvent()
    {
        // Arrange
        var sut = new ServiceClientAccessToken();
        var eventRaised = false;
        sut.AccessTokenUpdated += (_, _) => eventRaised = true;

        // Act
        sut.AccessToken = "new-token";

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void AccessToken_SetSameValue_DoesNotRaiseAccessTokenUpdatedEvent()
    {
        // Arrange
        var sut = new ServiceClientAccessToken();
        sut.AccessToken = "same-token";
        var eventCount = 0;
        sut.AccessTokenUpdated += (_, _) => eventCount++;

        // Act
        sut.AccessToken = "same-token";

        // Assert
        Assert.Equal(0, eventCount);
    }

    [Fact]
    public void AccessToken_SetMultipleDifferentValues_RaisesEventEachTime()
    {
        // Arrange
        var sut = new ServiceClientAccessToken();
        var eventCount = 0;
        sut.AccessTokenUpdated += (_, _) => eventCount++;

        // Act
        sut.AccessToken = "token-1";
        sut.AccessToken = "token-2";
        sut.AccessToken = "token-3";

        // Assert
        Assert.Equal(3, eventCount);
    }

    [Fact]
    public void AccessToken_SetToNull_RaisesEvent()
    {
        // Arrange
        var sut = new ServiceClientAccessToken();
        sut.AccessToken = "initial-token";
        var eventRaised = false;
        sut.AccessTokenUpdated += (_, _) => eventRaised = true;

        // Act
        sut.AccessToken = null;

        // Assert
        Assert.True(eventRaised);
        Assert.Null(sut.AccessToken);
    }

    [Fact]
    public void AccessToken_SetFromNullToValue_RaisesEvent()
    {
        // Arrange
        var sut = new ServiceClientAccessToken();
        var eventRaised = false;
        sut.AccessTokenUpdated += (_, _) => eventRaised = true;

        // Act
        sut.AccessToken = "new-token";

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void AccessToken_NoEventSubscribers_DoesNotThrow()
    {
        // Arrange
        var sut = new ServiceClientAccessToken();

        // Act & Assert - Should not throw
        var exception = Record.Exception(() => sut.AccessToken = "test-token");
        Assert.Null(exception);
    }

    [Fact]
    public void AccessToken_EventSenderIsInstance_SenderIsCorrect()
    {
        // Arrange
        var sut = new ServiceClientAccessToken();
        object? capturedSender = null;
        sut.AccessTokenUpdated += (sender, _) => capturedSender = sender;

        // Act
        sut.AccessToken = "token";

        // Assert
        Assert.Same(sut, capturedSender);
    }

    [Fact]
    public void AccessToken_EventArgsAreEmpty_ArgsAreEventArgsEmpty()
    {
        // Arrange
        var sut = new ServiceClientAccessToken();
        EventArgs? capturedArgs = null;
        sut.AccessTokenUpdated += (_, args) => capturedArgs = args;

        // Act
        sut.AccessToken = "token";

        // Assert
        Assert.Same(EventArgs.Empty, capturedArgs);
    }
}
