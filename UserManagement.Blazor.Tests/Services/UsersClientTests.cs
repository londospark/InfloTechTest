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
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
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
            var payload = new UserListDto(new[] { new UserListItemDto(1, "A", "B", "a@b.com", true) });
            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            resp.Content = JsonContent.Create(payload);
            return resp;
        });
        var http = new HttpClient(handler) { BaseAddress = new("http://localhost/") };
        var client = new UsersClient(http);

        // Act
        var _ = await client.GetUsersByActiveAsync(isActive);

        // Assert
        captured.Should().NotBeNull();
        captured!.Method.Should().Be(HttpMethod.Get);
        captured!.RequestUri!.ToString().Should().EndWith(expected);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;
        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) => _handler = handler;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_handler(request));
    }
}
