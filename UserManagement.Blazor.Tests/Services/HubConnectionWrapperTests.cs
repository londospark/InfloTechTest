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
        public List<string> InvokedMethods = new();
        public List<object[]> InvokedArgs = new();
        public bool Started { get; private set; }
        public bool Stopped { get; private set; }
        public bool Disposed { get; private set; }
        public HubConnectionState State { get; set; } = HubConnectionState.Disconnected;
        private readonly Dictionary<string, Delegate> _handlers = new();
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
        public void On<T>(string methodName, Action<T> handler)
        {
            _handlers[methodName] = handler;
        }
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

    private class HubConnectionWrapperForTest : IUserLogsHubConnection
    {
        private readonly FakeHubConnection _inner;
        public HubConnectionWrapperForTest(FakeHubConnection inner) => _inner = inner;
        public HubConnectionState State => _inner.State;
        public Task StartAsync() => _inner.StartAsync();
        public Task StopAsync() => _inner.StopAsync();
        public Task InvokeAsync(string method, params object[] args) => _inner.InvokeAsync(method, args);
        public void On<T>(string methodName, Action<T> handler) => _inner.On<T>(methodName, handler);
        public ValueTask DisposeAsync() => _inner.DisposeAsync();
        public void Raise<T>(string methodName, T arg) => _inner.Raise(methodName, arg);
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
