using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using UserManagement.Blazor.Components;
using Xunit;

namespace UserManagement.Blazor.Tests.Components;

public class ConfirmDialogTests : TestContext
{
    [Fact]
    public void Renders_WithModalAboveBackdrop_UsingHigherZIndex()
    {
        // Arrange
        var cut = Render<ConfirmDialog>(ps => ps
            .Add(p => p.Show, true)
            .Add(p => p.Title, "Confirm deletion")
            .Add(p => p.Message, "Are you sure?")
            .Add(p => p.ConfirmText, "Delete")
            .Add(p => p.CancelText, "Cancel")
        );

        // Act
        var backdrop = cut.Find(".modal-backdrop");
        var modal = cut.Find(".modal");

        // Assert (expected correct layering semantics)
        // Expect explicit z-index values similar to Bootstrap defaults (modal 1050, backdrop 1040)
        modal.GetAttribute("style").Should().Contain("z-index: 1050");
        backdrop.GetAttribute("style").Should().Contain("z-index: 1040");

        // And ensure the modal is rendered after the backdrop in the DOM so it appears above it
        var markup = cut.Markup;
        markup.IndexOf("modal-backdrop").Should().BeLessThan(markup.IndexOf("modal-dialog"));
    }

    [Fact]
    public void Buttons_AreClickable_WhenDialogVisible()
    {
        // Arrange: flags toggled by callbacks
        var confirmed = false;
        var cancelled = false;

        var cut = Render<ConfirmDialog>(ps => ps
            .Add(p => p.Show, true)
            .Add(p => p.ConfirmTestId, "confirm-test")
            .Add(p => p.CancelTestId, "cancel-test")
            .Add(p => p.OnConfirm, EventCallback.Factory.Create(this, () => confirmed = true))
            .Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => cancelled = true))
        );

        // Act: simulate user clicks
        cut.Find("button[data-testid='cancel-test']").Click();
        cut.Find("button[data-testid='confirm-test']").Click();

        // Assert: both callbacks should have been invoked
        confirmed.Should().BeTrue("confirm should be clickable and invoked");
        cancelled.Should().BeTrue("cancel should be clickable and invoked");
    }
}
