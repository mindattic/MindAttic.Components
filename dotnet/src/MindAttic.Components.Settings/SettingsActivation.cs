using System.IO.Pipes;
using System.Text;

namespace MindAttic.Components.Settings;

/// <summary>
/// The single-instance protocol between an app's cog and MindAttic.Vault.Desktop: one line
/// "open {appId}" written to a per-user named pipe, answered by "ok" once the Desktop has come
/// to the front on that tab, or "unknown" when it has no such tab. Both sides live here so they
/// cannot drift apart.
/// </summary>
public static class SettingsActivation
{
    /// <summary>The pipe name for the current Windows user (named pipes are machine-wide).</summary>
    public static string DefaultPipeName => "MindAttic.Vault.Desktop.Activate." + Environment.UserName;

    /// <summary>The Desktop's command-line switch that opens it at a tab: <c>--tab {appId}</c>.</summary>
    public const string TabSwitch = "--tab";

    /// <summary>Reads the app id from a Desktop command line (<c>--tab idiotproof</c> or
    /// <c>--tab=idiotproof</c>); null when absent or invalid.</summary>
    public static string? TabFromArgs(IReadOnlyList<string> args)
    {
        for (var i = 0; i < args.Count; i++)
        {
            string? value = null;
            if (args[i] == TabSwitch && i + 1 < args.Count) value = args[i + 1];
            else if (args[i].StartsWith(TabSwitch + "=", StringComparison.Ordinal)) value = args[i][(TabSwitch.Length + 1)..];
            if (value is not null) return SettingsTabDiscovery.IsValidAppId(value) ? value : null;
        }
        return null;
    }

    /// <summary>Asks a running Desktop to show <paramref name="appId"/>. Returns its answer
    /// ("ok" or "unknown"), or null when no Desktop is listening.</summary>
    public static async Task<string?> TryActivateAsync(string appId, TimeSpan timeout, string? pipeName = null, CancellationToken ct = default)
    {
        if (!SettingsTabDiscovery.IsValidAppId(appId)) throw new ArgumentException($"'{appId}' is not a valid settings tab id.", nameof(appId));
        using var timer = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timer.CancelAfter(timeout);
        try
        {
            await using var pipe = new NamedPipeClientStream(".", pipeName ?? DefaultPipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            await pipe.ConnectAsync(timer.Token);
            using var writer = new StreamWriter(pipe, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
            using var reader = new StreamReader(pipe, Encoding.UTF8, leaveOpen: true);
            await writer.WriteLineAsync("open " + appId);
            return (await reader.ReadLineAsync(timer.Token))?.Trim();
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return null;
        }
        catch (Exception ex) when (ex is IOException or TimeoutException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}

/// <summary>
/// The Desktop's side of <see cref="SettingsActivation"/>: listens on the pipe and calls
/// <c>activate(appId)</c> for each "open" request, answering "ok" when it returns true.
/// Creating it throws <see cref="IOException"/> or <see cref="UnauthorizedAccessException"/> when
/// another instance already owns the pipe, which is how the Desktop knows it is the second copy.
/// </summary>
public sealed class SettingsActivationListener : IAsyncDisposable
{
    private readonly string pipeName;
    private readonly Func<string, Task<bool>> activate;
    private readonly CancellationTokenSource stop = new();
    private readonly Task loop;
    private NamedPipeServerStream? waiting;

    /// <summary>Starts listening. <paramref name="activate"/> runs on a pool thread; marshal to the UI yourself.</summary>
    public SettingsActivationListener(Func<string, Task<bool>> activate, string? pipeName = null)
    {
        this.pipeName = pipeName ?? SettingsActivation.DefaultPipeName;
        this.activate = activate;
        // The first server is created here so a second copy fails now rather than on the loop.
        waiting = NewServer(firstInstance: true);
        loop = Task.Run(RunAsync);
    }

    private NamedPipeServerStream NewServer(bool firstInstance) => new(
        pipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte,
        PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly | (firstInstance ? PipeOptions.FirstPipeInstance : PipeOptions.None));

    private async Task RunAsync()
    {
        while (!stop.IsCancellationRequested)
        {
            var server = waiting ?? NewServer(firstInstance: false);
            waiting = null;
            try
            {
                await server.WaitForConnectionAsync(stop.Token);
                _ = HandleAsync(server);
            }
            catch (OperationCanceledException)
            {
                await server.DisposeAsync();
                return;
            }
            catch (IOException)
            {
                await server.DisposeAsync();
            }
        }
    }

    private async Task HandleAsync(NamedPipeServerStream server)
    {
        await using (server)
        {
            try
            {
                using var reader = new StreamReader(server, Encoding.UTF8, leaveOpen: true);
                using var writer = new StreamWriter(server, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(stop.Token);
                deadline.CancelAfter(TimeSpan.FromSeconds(10));
                var line = (await reader.ReadLineAsync(deadline.Token))?.Trim() ?? "";
                var appId = line.StartsWith("open ", StringComparison.Ordinal) ? line[5..].Trim() : null;
                var shown = SettingsTabDiscovery.IsValidAppId(appId) && await activate(appId!);
                await writer.WriteLineAsync(shown ? "ok" : "unknown");
            }
            catch (Exception ex) when (ex is IOException or OperationCanceledException or ObjectDisposedException)
            {
            }
        }
    }

    /// <summary>Stops listening.</summary>
    public async ValueTask DisposeAsync()
    {
        await stop.CancelAsync();
        try { await loop; } catch (OperationCanceledException) { }
        stop.Dispose();
    }
}
