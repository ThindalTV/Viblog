using Viblog.Admin.Services;
using Viblog.Infrastructure.Admin.Services;

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
        void OnConfirm() { return; }
        MessageDialogInfo? capturedDialog = null;
        service.OnDialogChanged += (dialog) =>
        {
            capturedDialog = dialog as MessageDialogInfo;
            return Task.CompletedTask;
        };

        // Act
        service.ShowConfirmation(title, message, OnConfirm, "Yes", "No");

        // Assert
        Assert.NotNull(capturedDialog);
        Assert.Equal(title, capturedDialog.Title);
        Assert.Equal(message, capturedDialog.Message);
        Assert.Equal("Yes", capturedDialog.ConfirmText);
        Assert.Equal("No", capturedDialog.CancelText);
        Assert.True(capturedDialog.ShowConfirm);
        Assert.True(capturedDialog.ShowCancel);
        Assert.NotNull(capturedDialog.OnConfirm);
    }

    [Fact]
    public void ShowConfirmation_DefaultButtonTexts()
    {
        // Arrange
        var service = new DialogService();
        void OnConfirm() { }
        MessageDialogInfo? capturedDialog = null;
        service.OnDialogChanged += (dialog) =>
        {
            capturedDialog = dialog as MessageDialogInfo;
            return Task.CompletedTask;
        };

        // Act
        service.ShowConfirmation("Title", "Message", OnConfirm);

        // Assert
        Assert.NotNull(capturedDialog);
        Assert.Equal("Confirm", capturedDialog.ConfirmText);
        Assert.Equal("Cancel", capturedDialog.CancelText);
    }

    [Fact]
    public void ShowConfirmationAsync_SetsCurrentDialogWithAsyncCallback()
    {
        // Arrange
        var service = new DialogService();
        var title = "Async Confirm";
        var message = "Are you sure?";
        async Task OnConfirmAsync()
        {
            await Task.Delay(1);
        }
        MessageDialogInfo? capturedDialog = null;
        service.OnDialogChanged += (dialog) =>
        {
            capturedDialog = dialog as MessageDialogInfo;
            return Task.CompletedTask;
        };

        // Act
        service.ShowConfirmationAsync(title, message, OnConfirmAsync, "Proceed", "Cancel");

        // Assert
        Assert.NotNull(capturedDialog);
        Assert.Equal(title, capturedDialog.Title);
        Assert.Equal(message, capturedDialog.Message);
        Assert.Equal("Proceed", capturedDialog.ConfirmText);
        Assert.Equal("Cancel", capturedDialog.CancelText);
        Assert.True(capturedDialog.ShowConfirm);
        Assert.True(capturedDialog.ShowCancel);
        Assert.NotNull(capturedDialog.OnConfirmAsync);
    }

    [Fact]
    public void ShowAlert_SetsAlertDialog()
    {
        // Arrange
        var service = new DialogService();
        var title = "Alert";
        var message = "Important information";
        MessageDialogInfo? capturedDialog = null;
        service.OnDialogChanged += (dialog) =>
        {
            capturedDialog = dialog as MessageDialogInfo;
            return Task.CompletedTask;
        };

        // Act
        service.ShowAlert(title, message);

        // Assert
        Assert.NotNull(capturedDialog);
        Assert.Equal(title, capturedDialog.Title);
        Assert.Equal(message, capturedDialog.Message);
        Assert.Equal("OK", capturedDialog.ConfirmText);
        Assert.True(capturedDialog.ShowConfirm);
        Assert.False(capturedDialog.ShowCancel);
    }

    [Fact]
    public void ShowAlert_WithCustomConfirmText()
    {
        // Arrange
        var service = new DialogService();
        MessageDialogInfo? capturedDialog = null;
        service.OnDialogChanged += (dialog) =>
        {
            capturedDialog = dialog as MessageDialogInfo;
            return Task.CompletedTask;
        };

        // Act
        service.ShowAlert("Title", "Message", null, "Got It");

        // Assert
        Assert.NotNull(capturedDialog);
        Assert.Equal("Got It", capturedDialog.ConfirmText);
    }

    [Fact]
    public void ShowAlert_WithConfirmCallback()
    {
        // Arrange
        var service = new DialogService();
        void OnConfirm() { }
        MessageDialogInfo? capturedDialog = null;
        service.OnDialogChanged += (dialog) =>
        {
            capturedDialog = dialog as MessageDialogInfo;
            return Task.CompletedTask;
        };

        // Act
        service.ShowAlert("Title", "Message", OnConfirm);

        // Assert
        Assert.NotNull(capturedDialog);
        Assert.NotNull(capturedDialog.OnConfirm);
    }

    [Fact]
    public void Close_SendsNullDialog()
    {
        // Arrange
        var service = new DialogService();
        DialogInfo? capturedDialog = null;
        var eventCount = 0;
        service.OnDialogChanged += (dialog) =>
        {
            capturedDialog = dialog;
            eventCount++;
            return Task.CompletedTask;
        };
        service.ShowConfirmation("Title", "Message", () => { });

        // Act
        service.Close();

        // Assert
        Assert.Null(capturedDialog);
        Assert.Equal(2, eventCount); // Once for show, once for close
    }

    [Fact]
    public void OnDialogChanged_RaisesEventWhenDialogShown()
    {
        // Arrange
        var service = new DialogService();
        var eventRaised = false;
        service.OnDialogChanged += (dialog) =>
        {
            eventRaised = true;
            return Task.CompletedTask;
        };

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
        service.OnDialogChanged += (dialog) =>
        {
            eventRaised = true;
            return Task.CompletedTask;
        };

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
        MessageDialogInfo? capturedDialog = null;
        service.OnDialogChanged += (dialog) =>
        {
            capturedDialog = dialog as MessageDialogInfo;
            return Task.CompletedTask;
        };
        service.ShowConfirmation("Old Title", "Old Message", () => { });

        // Act
        service.ShowConfirmation("New Title", "New Message", () => { });

        // Assert
        Assert.NotNull(capturedDialog);
        Assert.Equal("New Title", capturedDialog.Title);
        Assert.Equal("New Message", capturedDialog.Message);
    }

    [Fact]
    public void InitialState_NoDialogShown()
    {
        // Arrange
        var service = new DialogService();
        DialogInfo? capturedDialog = null;
        var eventRaised = false;
        service.OnDialogChanged += (dialog) =>
        {
            capturedDialog = dialog;
            eventRaised = true;
            return Task.CompletedTask;
        };

        // Assert - no event raised initially
        Assert.False(eventRaised);
        Assert.Null(capturedDialog);
    }

    [Fact]
    public void MultipleSubscribers_AllReceiveEvent()
    {
        // Arrange
        var service = new DialogService();
        var subscriber1Called = false;
        var subscriber2Called = false;
        service.OnDialogChanged += (dialog) =>
        {
            subscriber1Called = true;
            return Task.CompletedTask;
        };
        service.OnDialogChanged += (dialog) =>
        {
            subscriber2Called = true;
            return Task.CompletedTask;
        };

        // Act
        service.ShowConfirmation("Title", "Message", () => { });

        // Assert
        Assert.True(subscriber1Called);
        Assert.True(subscriber2Called);
    }

    [Fact]
    public async Task ShowConfirmationAsync_ConfirmCallbackExecutes()
    {
        // Arrange
        var service = new DialogService();
        var executed = false;
        async Task OnConfirm()
        {
            await Task.CompletedTask;
            executed = true;
        }
        MessageDialogInfo? capturedDialog = null;
        service.OnDialogChanged += (dialog) =>
        {
            capturedDialog = dialog as MessageDialogInfo;
            return Task.CompletedTask;
        };

        service.ShowConfirmationAsync("Title", "Message", OnConfirm);

        // Act
        var callback = capturedDialog?.OnConfirmAsync;
        if (callback is not null)
        {
            await callback.Invoke();
        }

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
        MessageDialogInfo? capturedDialog = null;
        service.OnDialogChanged += (dialog) =>
        {
            capturedDialog = dialog as MessageDialogInfo;
            return Task.CompletedTask;
        };

        service.ShowConfirmation("Title", "Message", OnConfirm);

        // Act
        capturedDialog?.OnConfirm?.Invoke();

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
        MessageDialogInfo? capturedDialog = null;
        service.OnDialogChanged += (dialog) =>
        {
            capturedDialog = dialog as MessageDialogInfo;
            return Task.CompletedTask;
        };

        // Act
        service.ShowConfirmation("Title", "Message", () => { }, confirmText, cancelText);

        // Assert
        Assert.NotNull(capturedDialog);
        Assert.Equal(confirmText, capturedDialog.ConfirmText);
        Assert.Equal(cancelText, capturedDialog.CancelText);
    }

    [Fact]
    public void DialogInfo_BothSyncAndAsyncCallbacksCanBeNull()
    {
        // Arrange
        var service = new DialogService();
        MessageDialogInfo? capturedDialog = null;
        service.OnDialogChanged += (dialog) =>
        {
            capturedDialog = dialog as MessageDialogInfo;
            return Task.CompletedTask;
        };

        // Act
        service.ShowAlert("Title", "Message");

        // Assert
        Assert.NotNull(capturedDialog);
        Assert.Null(capturedDialog.OnConfirm);
        Assert.Null(capturedDialog.OnConfirmAsync);
    }
}
