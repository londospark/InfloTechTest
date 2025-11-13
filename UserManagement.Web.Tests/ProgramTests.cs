using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;

namespace UserManagement.Web.Tests
{
    public class ProgramTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
    {
        [Fact]
        public async Task GetScalar_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = factory.CreateClient();

            // Act
            var response = await client.GetAsync("/scalar");

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Redirect);
        }
    }
}
