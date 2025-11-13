using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using UserManagement.Blazor.Services;
using UserManagement.Shared.DTOs;
using Xunit;

namespace UserManagement.Blazor.Tests.Services;

public class UsersClientTests
{
    [Fact]
    public async Task GetUsersAsync_WhenApiReturnsNull_ReturnsEmptyList()
    {
        // Arrange
        var handler = new StubHandler(_ => new(HttpStatusCode.OK)
        {
            Content = new StringContent("null")
        });
        var http = new HttpClient(handler) { BaseAddress = new("http://localhost/") };
        var client = new UsersClient(http);

        // Act
        var result = await client.GetUsersAsync();

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().NotBeNull();
        result.Items.Should().BeEmpty();
    }

    [Theory]
    [InlineData(true, "api/users/filter?active=true")]
    [InlineData(false, "api/users/filter?active=false")]
    public async Task GetUsersByActiveAsync_UsesExpectedUrl(bool isActive, string expected)
    {
        // Arrange
        HttpRequestMessage? captured = null;
        var handler = new StubHandler(req =>
        {
            captured = req;
            // Return a simple valid payload
            var payload = new UserListDto([new(1, "A", "B", "a@b.com", true, new(2000,1,1))]);
            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            resp.Content = JsonContent.Create(payload);
            return resp;
        });
        var http = new HttpClient(handler) { BaseAddress = new("http://localhost/") };
        var client = new UsersClient(http);

        // Act
        _ = await client.GetUsersByActiveAsync(isActive);

        // Assert
        captured.Should().NotBeNull();
        captured!.Method.Should().Be(HttpMethod.Get);
        captured!.RequestUri!.ToString().Should().EndWith(expected);
    }

    [Fact]
    public async Task DeleteUserAsync_SendsDeleteToExpectedUrl()
    {
        // Arrange
        HttpRequestMessage? captured = null;
        var handler = new StubHandler(req =>
        {
            captured = req;
            return new(HttpStatusCode.NoContent);
        });
        var http = new HttpClient(handler) { BaseAddress = new("http://localhost/") };
        var client = new UsersClient(http);

        // Act
        await client.DeleteUserAsync(42);

        // Assert
        captured.Should().NotBeNull();
        captured!.Method.Should().Be(HttpMethod.Delete);
        captured!.RequestUri!.ToString().Should().EndWith("api/users/42");
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(handler(request));
    }
}
