using Xunit;
using Bunit;
using FluentAssertions;
using UserManagement.Blazor;

namespace UserManagement.Blazor.Tests
{
    public class ProgramTests : BunitContext
    {
        [Fact]
        public void App_Starts_WithoutError()
        {
            // Arrange & Act: Render the root component
            var cut = Render<App>();

            // Assert: The root component markup is not null or empty
            cut.Markup.Should().NotBeNullOrWhiteSpace();
        }
    }
}
