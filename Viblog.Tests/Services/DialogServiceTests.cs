using Viblog.Admin.Services;

namespace Viblog.Tests.Services;

/// <summary>
/// Unit tests for DialogService
/// </summary>
public class DialogServiceTests
{
    [Fact]
    public void ShowConfirmation_SetsCurrentDialog()
    {
        // Arrange
        var service = new DialogService();
        var title = "Confirm Action";
        var message = "Are you sure?";
        var confirmCalled = false;
        void OnConfirm() => confirmCalled = true;

        // Act
        service.ShowConfirmation(title, message, OnConfirm, "Yes", "No");

        // Assert
        Assert.NotNull(service.CurrentDialog);
        Assert.Equal(title, service.CurrentDialog.Title);
        Assert.Equal(message, service.CurrentDialog.Message);
        Assert.Equal("Yes", service.CurrentDialog.ConfirmText);
        Assert.Equal("No", service.CurrentDialog.CancelText);
        Assert.True(service.CurrentDialog.ShowConfirm);
        Assert.True(service.CurrentDialog.ShowCancel);
        Assert.NotNull(service.CurrentDialog.OnConfirm);
    }

    [Fact]
    public void ShowConfirmation_DefaultButtonTexts()
    {
        // Arrange
        var service = new DialogService();
        void OnConfirm() { }

        // Act
        service.ShowConfirmation("Title", "Message", OnConfirm);

        // Assert
        Assert.NotNull(service.CurrentDialog);
        Assert.Equal("Confirm", service.CurrentDialog.ConfirmText);
        Assert.Equal("Cancel", service.CurrentDialog.CancelText);
    }

    [Fact]
    public void ShowConfirmationAsync_SetsCurrentDialogWithAsyncCallback()
    {
        // Arrange
        var service = new DialogService();
        var title = "Async Confirm";
        var message = "Are you sure?";
        var asyncCalled = false;
        async Task OnConfirmAsync()
        {
            await Task.Delay(1);
            asyncCalled = true;
        }

        // Act
        service.ShowConfirmationAsync(title, message, OnConfirmAsync, "Proceed", "Cancel");

        // Assert
        Assert.NotNull(service.CurrentDialog);
        Assert.Equal(title, service.CurrentDialog.Title);
        Assert.Equal(message, service.CurrentDialog.Message);
        Assert.Equal("Proceed", service.CurrentDialog.ConfirmText);
        Assert.Equal("Cancel", service.CurrentDialog.CancelText);
        Assert.True(service.CurrentDialog.ShowConfirm);
        Assert.True(service.CurrentDialog.ShowCancel);
        Assert.NotNull(service.CurrentDialog.OnConfirmAsync);
    }

    [Fact]
    public void ShowAlert_SetsAlertDialog()
    {
        // Arrange
        var service = new DialogService();
        var title = "Alert";
        var message = "Important information";

        // Act
        service.ShowAlert(title, message);

        // Assert
        Assert.NotNull(service.CurrentDialog);
        Assert.Equal(title, service.CurrentDialog.Title);
        Assert.Equal(message, service.CurrentDialog.Message);
        Assert.Equal("OK", service.CurrentDialog.ConfirmText);
        Assert.True(service.CurrentDialog.ShowConfirm);
        Assert.False(service.CurrentDialog.ShowCancel);
    }

    [Fact]
    public void ShowAlert_WithCustomConfirmText()
    {
        // Arrange
        var service = new DialogService();

        // Act
        service.ShowAlert("Title", "Message", null, "Got It");

        // Assert
        Assert.NotNull(service.CurrentDialog);
        Assert.Equal("Got It", service.CurrentDialog.ConfirmText);
    }

    [Fact]
    public void ShowAlert_WithConfirmCallback()
    {
        // Arrange
        var service = new DialogService();
        var confirmCalled = false;
        void OnConfirm() => confirmCalled = true;

        // Act
        service.ShowAlert("Title", "Message", OnConfirm);

        // Assert
        Assert.NotNull(service.CurrentDialog);
        Assert.NotNull(service.CurrentDialog.OnConfirm);
    }

    [Fact]
    public void Close_ClearsCurrentDialog()
    {
        // Arrange
        var service = new DialogService();
        service.ShowConfirmation("Title", "Message", () => { });

        // Act
        service.Close();

        // Assert
        Assert.Null(service.CurrentDialog);
    }

    [Fact]
    public void OnDialogChanged_RaisesEventWhenDialogShown()
    {
        // Arrange
        var service = new DialogService();
        var eventRaised = false;
        service.OnDialogChanged += (sender, args) => eventRaised = true;

        // Act
        service.ShowConfirmation("Title", "Message", () => { });

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void OnDialogChanged_RaisesEventWhenDialogClosed()
    {
        // Arrange
        var service = new DialogService();
        service.ShowConfirmation("Title", "Message", () => { });
        var eventRaised = false;
        service.OnDialogChanged += (sender, args) => eventRaised = true;

        // Act
        service.Close();

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void ShowConfirmation_ReplacesExistingDialog()
    {
        // Arrange
        var service = new DialogService();
        service.ShowConfirmation("Old Title", "Old Message", () => { });

        // Act
        service.ShowConfirmation("New Title", "New Message", () => { });

        // Assert
        Assert.NotNull(service.CurrentDialog);
        Assert.Equal("New Title", service.CurrentDialog.Title);
        Assert.Equal("New Message", service.CurrentDialog.Message);
    }

    [Fact]
    public void CurrentDialog_InitiallyNull()
    {
        // Arrange & Act
        var service = new DialogService();

        // Assert
        Assert.Null(service.CurrentDialog);
    }

    [Fact]
    public void MultipleSubscribers_AllReceiveEvent()
    {
        // Arrange
        var service = new DialogService();
        var subscriber1Called = false;
        var subscriber2Called = false;
        service.OnDialogChanged += (sender, args) => subscriber1Called = true;
        service.OnDialogChanged += (sender, args) => subscriber2Called = true;

        // Act
        service.ShowConfirmation("Title", "Message", () => { });

        // Assert
        Assert.True(subscriber1Called);
        Assert.True(subscriber2Called);
    }

    [Fact]
    public void ShowConfirmationAsync_ConfirmCallbackExecutes()
    {
        // Arrange
        var service = new DialogService();
        var executed = false;
        async Task OnConfirm()
        {
            await Task.CompletedTask;
            executed = true;
        }

        service.ShowConfirmationAsync("Title", "Message", OnConfirm);

        // Act
        var callback = service.CurrentDialog?.OnConfirmAsync;
        callback?.Invoke().Wait();

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public void ShowConfirmation_ConfirmCallbackExecutes()
    {
        // Arrange
        var service = new DialogService();
        var executed = false;
        void OnConfirm() => executed = true;

        service.ShowConfirmation("Title", "Message", OnConfirm);

        // Act
        service.CurrentDialog?.OnConfirm?.Invoke();

        // Assert
        Assert.True(executed);
    }

    [Theory]
    [InlineData("Custom Confirm", "Custom Cancel")]
    [InlineData("Yes", "No")]
    [InlineData("Proceed", "Back")]
    public void ShowConfirmation_CustomButtonTexts(string confirmText, string cancelText)
    {
        // Arrange
        var service = new DialogService();

        // Act
        service.ShowConfirmation("Title", "Message", () => { }, confirmText, cancelText);

        // Assert
        Assert.NotNull(service.CurrentDialog);
        Assert.Equal(confirmText, service.CurrentDialog.ConfirmText);
        Assert.Equal(cancelText, service.CurrentDialog.CancelText);
    }

    [Fact]
    public void DialogInfo_BothSyncAndAsyncCallbacksCanBeNull()
    {
        // Arrange
        var service = new DialogService();

        // Act
        service.ShowAlert("Title", "Message");

        // Assert
        Assert.NotNull(service.CurrentDialog);
        Assert.Null(service.CurrentDialog.OnConfirm);
        Assert.Null(service.CurrentDialog.OnConfirmAsync);
    }
}
