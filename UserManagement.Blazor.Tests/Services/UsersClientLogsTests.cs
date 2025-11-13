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

public class UsersClientLogsTests
{
    [Fact]
    public async Task GetUserLogsAsync_UsesExpectedUrl_AndParsesPayload()
    {
        // Arrange
        HttpRequestMessage? captured = null;
        var dto = new PagedResultDto<UserLogDto>(
            new System.Collections.Generic.List<UserLogDto>
            {
                new(1, 7, "A", DateTime.UtcNow),
            },
            page: 2,
            pageSize: 3,
            totalCount: 10
        );

        var handler = new StubHandler(req =>
        {
            captured = req;
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(dto)
            };
            return resp;
        });

        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var client = new UsersClient(http);

        // Act
        var result = await client.GetUserLogsAsync(7, page: 2, pageSize: 3);

        // Assert
        captured.Should().NotBeNull();
        captured!.Method.Should().Be(HttpMethod.Get);
        captured.RequestUri!.ToString().Should().EndWith("api/users/7/logs?page=2&pageSize=3");

        result.Should().NotBeNull();
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(3);
        result.TotalCount.Should().Be(10);
        result.Items.Should().HaveCount(1);
        result.Items[0].Message.Should().Be("A");
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(handler(request));
    }
}
