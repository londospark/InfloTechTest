using Microsoft.AspNetCore.Components;

namespace UserManagement.Blazor.Tests.TestHelpers;

/// <summary>
/// Test NavigationManager for unit testing Blazor components
/// </summary>
public sealed class TestNavigationManager : NavigationManager
{
    public TestNavigationManager(string baseUri = "http://localhost/", string uri = "http://localhost/") => Initialize(baseUri, uri);

    protected override void NavigateToCore(string uri, bool forceLoad) => Uri = ToAbsoluteUri(uri).ToString();

    public new void NavigateTo(string uri, bool forceLoad = false) => NavigateToCore(uri, forceLoad);
}
