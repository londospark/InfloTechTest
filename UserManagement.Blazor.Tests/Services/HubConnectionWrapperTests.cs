using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using UserManagement.Blazor.Services;
using Xunit;

namespace UserManagement.Blazor.Tests.Services;

public class HubConnectionWrapperTests
{
    private class FakeHubConnection : IUserLogsHubConnection
    {
        public List<string> InvokedMethods = [];
        public List<object[]> InvokedArgs = [];
        public bool Started { get; private set; }
        public bool Stopped { get; private set; }
        public bool Disposed { get; private set; }
        public HubConnectionState State { get; set; } = HubConnectionState.Disconnected;
        private readonly Dictionary<string, Delegate> _handlers = [];
        public Task StartAsync()
        {
            Started = true;
            State = HubConnectionState.Connected;
            return Task.CompletedTask;
        }
        public Task StopAsync()
        {
            Stopped = true;
            State = HubConnectionState.Disconnected;
            return Task.CompletedTask;
        }
        public Task InvokeAsync(string methodName, params object[] args)
        {
            InvokedMethods.Add(methodName);
            InvokedArgs.Add(args);
            return Task.CompletedTask;
        }
        public void On<T>(string methodName, Action<T> handler) => _handlers[methodName] = handler;
        public void Raise<T>(string methodName, T arg)
        {
            if (_handlers.TryGetValue(methodName, out var del) && del is Action<T> act)
                act(arg);
        }
        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private class HubConnectionWrapperForTest(HubConnectionWrapperTests.FakeHubConnection inner) : IUserLogsHubConnection
    {
        public HubConnectionState State => inner.State;
        public Task StartAsync() => inner.StartAsync();
        public Task StopAsync() => inner.StopAsync();
        public Task InvokeAsync(string method, params object[] args) => inner.InvokeAsync(method, args);
        public void On<T>(string methodName, Action<T> handler) => inner.On(methodName, handler);
        public ValueTask DisposeAsync() => inner.DisposeAsync();
        public void Raise<T>(string methodName, T arg) => inner.Raise(methodName, arg);
    }

    [Fact]
    public void State_ReturnsInnerState()
    {
        var fake = new FakeHubConnection();
        fake.State = HubConnectionState.Connected;
        var wrapper = new HubConnectionWrapperForTest(fake);
        wrapper.State.Should().Be(HubConnectionState.Connected);
    }

    [Fact]
    public async Task StartAsync_DelegatesToInner()
    {
        var fake = new FakeHubConnection();
        var wrapper = new HubConnectionWrapperForTest(fake);
        await wrapper.StartAsync();
        fake.Started.Should().BeTrue();
        wrapper.State.Should().Be(HubConnectionState.Connected);
    }

    [Fact]
    public async Task StopAsync_DelegatesToInner()
    {
        var fake = new FakeHubConnection();
        var wrapper = new HubConnectionWrapperForTest(fake);
        await wrapper.StopAsync();
        fake.Stopped.Should().BeTrue();
        wrapper.State.Should().Be(HubConnectionState.Disconnected);
    }

    [Fact]
    public async Task InvokeAsync_DelegatesToInner()
    {
        var fake = new FakeHubConnection();
        var wrapper = new HubConnectionWrapperForTest(fake);
        await wrapper.InvokeAsync("Test", 1, "abc");
        fake.InvokedMethods.Should().Contain("Test");
        fake.InvokedArgs[0].Should().Contain(1);
        fake.InvokedArgs[0].Should().Contain("abc");
    }

    [Fact]
    public void On_DelegatesToInner()
    {
        var fake = new FakeHubConnection();
        var wrapper = new HubConnectionWrapperForTest(fake);
        int called = 0;
        wrapper.On<int>("Test", i => called = i);
        wrapper.Raise("Test", 42);
        called.Should().Be(42);
    }

    [Fact]
    public async Task DisposeAsync_DelegatesToInner()
    {
        var fake = new FakeHubConnection();
        var wrapper = new HubConnectionWrapperForTest(fake);
        await wrapper.DisposeAsync();
        fake.Disposed.Should().BeTrue();
    }
}
