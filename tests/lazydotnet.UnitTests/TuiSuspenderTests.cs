using FluentAssertions;
using lazydotnet.Core;

namespace lazydotnet.UnitTests;

[Collection("TuiSuspenderSequential")]
public class TuiSuspenderTests : IDisposable
{
    public TuiSuspenderTests() => TuiSuspender.SetHandler(null);

    public void Dispose() => TuiSuspender.SetHandler(null);

    [Fact]
    public async Task RunAsync_NoHandler_InvokesActionDirectly()
    {
        var invoked = false;

        await TuiSuspender.RunAsync(() =>
        {
            invoked = true;
            return Task.CompletedTask;
        });

        invoked.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_WithHandler_DelegatesToHandler()
    {
        var order = new List<string>();

        TuiSuspender.SetHandler(async action =>
        {
            order.Add("before");
            await action();
            order.Add("after");
        });

        await TuiSuspender.RunAsync(() =>
        {
            order.Add("action");
            return Task.CompletedTask;
        });

        order.Should().Equal("before", "action", "after");
    }

    [Fact]
    public async Task RunAsync_HandlerCanWrapWithSuspendResume()
    {
        var suspended = false;
        var actionRanWhileSuspended = false;

        TuiSuspender.SetHandler(async action =>
        {
            suspended = true;
            try { await action(); }
            finally { suspended = false; }
        });

        await TuiSuspender.RunAsync(() =>
        {
            actionRanWhileSuspended = suspended;
            return Task.CompletedTask;
        });

        actionRanWhileSuspended.Should().BeTrue();
        suspended.Should().BeFalse();
    }

    [Fact]
    public async Task RunAsync_HandlerNullCleared_FallsBackToDirect()
    {
        TuiSuspender.SetHandler(_ => Task.CompletedTask);
        TuiSuspender.SetHandler(null);

        var invoked = false;
        await TuiSuspender.RunAsync(() =>
        {
            invoked = true;
            return Task.CompletedTask;
        });

        invoked.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_ActionThrows_HandlerSeesException()
    {
        Exception? caught = null;

        TuiSuspender.SetHandler(async action =>
        {
            try { await action(); }
            catch (Exception ex) { caught = ex; throw; }
        });

        var act = async () => await TuiSuspender.RunAsync(() => throw new InvalidOperationException("boom"));

        await act.Should().ThrowAsync<InvalidOperationException>();
        caught.Should().BeOfType<InvalidOperationException>();
    }
}
