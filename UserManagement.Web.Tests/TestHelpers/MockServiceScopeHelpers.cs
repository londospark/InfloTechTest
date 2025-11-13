using System;
using Microsoft.Extensions.DependencyInjection;

namespace UserManagement.Web.Tests.TestHelpers;

/// <summary>
/// Helper methods for creating mock service scopes
/// </summary>
public static class MockServiceScopeHelpers
{
    /// <summary>
    /// Creates a mock IServiceScopeFactory with the provided service provider
    /// </summary>
    public static Mock<IServiceScopeFactory> CreateMockScopeFactory(IServiceProvider serviceProvider)
    {
        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProvider);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        return scopeFactory;
    }

    /// <summary>
    /// Creates a mock IServiceScopeFactory with services built from the provided collection
    /// </summary>
    public static Mock<IServiceScopeFactory> CreateMockScopeFactory(ServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider();
        return CreateMockScopeFactory(serviceProvider);
    }

    /// <summary>
    /// Creates a mock IServiceScopeFactory with a single service registered
    /// </summary>
    public static Mock<IServiceScopeFactory> CreateMockScopeFactory<TService>(TService service) where TService : class
    {
        var services = new ServiceCollection();
        services.AddSingleton(service);
        return CreateMockScopeFactory(services);
    }

    /// <summary>
    /// Creates a strict mock IServiceScopeFactory that expects no calls (for negative testing)
    /// </summary>
    public static Mock<IServiceScopeFactory> CreateStrictMockScopeFactory() => new(MockBehavior.Strict);
}
