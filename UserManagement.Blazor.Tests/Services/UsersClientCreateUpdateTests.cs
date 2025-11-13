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

public class UsersClientCreateUpdateTests
{
    [Fact]
    public async Task CreateUserAsync_ReturnsParsedDto()
    {
        // Arrange
        var expected = new UserListItemDto(99, "F", "L", "f@l.com", true, new System.DateTime(1990,1,1));
        var handler = new StubHandler(_ =>
        {
            var resp = new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = JsonContent.Create(expected)
            };
            return resp;
        });
        var http = new HttpClient(handler) { BaseAddress = new("http://localhost/") };
        var client = new UsersClient(http);

        // Act
        var result = await client.CreateUserAsync(new CreateUserRequestDto());

        // Assert
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task CreateUserAsync_NonSuccess_ThrowsHttpRequestException()
    {
        // Arrange
        var handler = new StubHandler(_ => new(HttpStatusCode.BadRequest));
        var http = new HttpClient(handler) { BaseAddress = new("http://localhost/") };
        var client = new UsersClient(http);

        // Act
        var act = () => client.CreateUserAsync(new CreateUserRequestDto());

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task UpdateUserAsync_ReturnsParsedDto()
    {
        // Arrange
        var expected = new UserListItemDto(7, "U", "P", "u@p.com", false, new System.DateTime(2000,1,1));
        var handler = new StubHandler(_ =>
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(expected)
            };
            return resp;
        });
        var http = new HttpClient(handler) { BaseAddress = new("http://localhost/") };
        var client = new UsersClient(http);

        // Act
        var result = await client.UpdateUserAsync(7, new CreateUserRequestDto());

        // Assert
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task UpdateUserAsync_NonSuccess_ThrowsHttpRequestException()
    {
        // Arrange
        var handler = new StubHandler(_ => new(HttpStatusCode.InternalServerError));
        var http = new HttpClient(handler) { BaseAddress = new("http://localhost/") };
        var client = new UsersClient(http);

        // Act
        var act = () => client.UpdateUserAsync(7, new CreateUserRequestDto());

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetUserLogsAsync_WhenApiReturnsNull_ReturnsEmptyPagedResult()
    {
        // Arrange
        var handler = new StubHandler(_ => new(HttpStatusCode.OK)
        {
            Content = new StringContent("null")
        });
        var http = new HttpClient(handler) { BaseAddress = new("http://localhost/") };
        var client = new UsersClient(http);

        // Act
        var result = await client.GetUserLogsAsync(5);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    private sealed class StubHandler(System.Func<HttpRequestMessage, HttpResponseMessage> handler)
        : HttpMessageHandler
    {
        private readonly System.Func<HttpRequestMessage, HttpResponseMessage> _handler = handler;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_handler(request));
    }
}
