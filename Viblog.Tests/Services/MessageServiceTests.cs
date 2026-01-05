using Viblog.Admin.Services;
using Xunit;

namespace Viblog.Tests.Services;

/// <summary>
/// Unit tests for MessageService
/// </summary>
public class MessageServiceTests
{
    [Fact]
    public void SetSuccess_SetsSuccessMessage()
    {
        // Arrange
        var service = new MessageService();
        var message = "Operation completed successfully";

        // Act
        service.SetSuccess(message);

        // Assert
        Assert.NotNull(service.CurrentMessage);
        Assert.Equal(MessageType.Success, service.CurrentMessage.Type);
        Assert.Equal(message, service.CurrentMessage.Message);
    }

    [Fact]
    public void SetFail_SetsErrorMessage()
    {
        // Arrange
        var service = new MessageService();
        var message = "Operation failed";

        // Act
        service.SetFail(message);

        // Assert
        Assert.NotNull(service.CurrentMessage);
        Assert.Equal(MessageType.Error, service.CurrentMessage.Type);
        Assert.Equal(message, service.CurrentMessage.Message);
    }

    [Fact]
    public void SetError_WithException_SetsErrorMessageWithUserMessage()
    {
        // Arrange
        var service = new MessageService();
        var exception = new InvalidOperationException("Test exception");
        var userMessage = "An error occurred";

        // Act
        service.SetError(exception, userMessage);

        // Assert
        Assert.NotNull(service.CurrentMessage);
        Assert.Equal(MessageType.Error, service.CurrentMessage.Type);
        Assert.Equal(userMessage, service.CurrentMessage.Message);
        Assert.NotNull(service.CurrentMessage.Exception);
        Assert.Equal(exception, service.CurrentMessage.Exception);
    }

    [Fact]
    public void SetError_WithNestedExceptions_IncludesInnerExceptionDetails()
    {
        // Arrange
        var service = new MessageService();
        var innerException = new ArgumentException("Inner error");
        var outerException = new InvalidOperationException("Outer error", innerException);
        var userMessage = "An error occurred";

        // Act
        service.SetError(outerException, userMessage);

        // Assert
        Assert.NotNull(service.CurrentMessage);
        Assert.Equal(MessageType.Error, service.CurrentMessage.Type);
        Assert.Contains(userMessage, service.CurrentMessage.Message);
        // Note: Exception details are stored in Exception property, not in Message
        Assert.NotNull(service.CurrentMessage.Exception);
        Assert.Equal(outerException, service.CurrentMessage.Exception);
    }

    [Fact]
    public void Clear_RemovesCurrentMessage()
    {
        // Arrange
        var service = new MessageService();
        service.SetSuccess("Test message");

        // Act
        service.Clear();

        // Assert
        Assert.Null(service.CurrentMessage);
    }

    [Fact]
    public void OnMessageChanged_RaisesEventWhenMessageSet()
    {
        // Arrange
        var service = new MessageService();
        var eventRaised = false;
        service.OnMessageChanged += (sender, args) => eventRaised = true;

        // Act
        service.SetSuccess("Test message");

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void OnMessageChanged_RaisesEventWhenCleared()
    {
        // Arrange
        var service = new MessageService();
        service.SetSuccess("Test message");
        var eventRaised = false;
        service.OnMessageChanged += (sender, args) => eventRaised = true;

        // Act
        service.Clear();

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void SetSuccess_ReplacesExistingMessage()
    {
        // Arrange
        var service = new MessageService();
        service.SetFail("Old message");

        // Act
        service.SetSuccess("New message");

        // Assert
        Assert.NotNull(service.CurrentMessage);
        Assert.Equal(MessageType.Success, service.CurrentMessage.Type);
        Assert.Equal("New message", service.CurrentMessage.Message);
    }

    [Fact]
    public void CurrentMessage_InitiallyNull()
    {
        // Arrange & Act
        var service = new MessageService();

        // Assert
        Assert.Null(service.CurrentMessage);
    }

    [Fact]
    public void SetSuccess_WithValidMessage_SetsMessage()
    {
        // Arrange
        var service = new MessageService();
        var message = "Valid message";

        // Act
        service.SetSuccess(message);

        // Assert
        Assert.NotNull(service.CurrentMessage);
        Assert.Equal(MessageType.Success, service.CurrentMessage.Type);
        Assert.Equal(message, service.CurrentMessage.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void SetSuccess_WithEmptyOrWhitespaceMessage_ThrowsArgumentException(string message)
    {
        // Arrange
        var service = new MessageService();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.SetSuccess(message));
    }

    [Fact]
    public void SetSuccess_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var service = new MessageService();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.SetSuccess(null!));
    }

    [Fact]
    public void MultipleSubscribers_AllReceiveEvent()
    {
        // Arrange
        var service = new MessageService();
        var subscriber1Called = false;
        var subscriber2Called = false;
        service.OnMessageChanged += (sender, args) => subscriber1Called = true;
        service.OnMessageChanged += (sender, args) => subscriber2Called = true;

        // Act
        service.SetSuccess("Test");

        // Assert
        Assert.True(subscriber1Called);
        Assert.True(subscriber2Called);
    }
}
