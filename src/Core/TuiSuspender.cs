namespace lazydotnet.Core;

public static class TuiSuspender
{
    private static Func<Func<Task>, Task>? _handler;

    public static void SetHandler(Func<Func<Task>, Task>? handler) => _handler = handler;

    public static async Task RunAsync(Func<Task> action)
    {
        var handler = _handler;
        if (handler != null)
        {
            await handler(action);
            return;
        }
        await action();
    }
}
