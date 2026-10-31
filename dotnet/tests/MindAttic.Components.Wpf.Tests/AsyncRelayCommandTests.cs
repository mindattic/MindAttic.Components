using MindAttic.Components.Wpf.Mvvm;
using Xunit;

namespace MindAttic.Components.Wpf.Tests;

/// <summary>M.COMP-T156: AsyncRelayCommand disables itself while running and never loses an exception.</summary>
public sealed class AsyncRelayCommandTests
{
    [Fact]
    public async Task CanExecute_is_false_while_running_and_true_again_after()
    {
        var gate = new TaskCompletionSource();
        var command = new AsyncRelayCommand(() => gate.Task);
        Assert.True(command.CanExecute(null));

        var run = command.ExecuteAsync();
        Assert.True(command.IsRunning);
        Assert.False(command.CanExecute(null));

        gate.SetResult();
        await run;
        Assert.False(command.IsRunning);
        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public async Task CanExecuteChanged_is_raised_on_start_and_on_finish()
    {
        var gate = new TaskCompletionSource();
        var command = new AsyncRelayCommand(() => gate.Task);
        var seen = new List<bool>();
        command.CanExecuteChanged += (_, _) => seen.Add(command.IsRunning);

        var run = command.ExecuteAsync();
        Assert.Equal([true], seen);
        gate.SetResult();
        await run;
        Assert.Equal([true, false], seen);
    }

    [Fact]
    public async Task A_second_execute_while_running_does_not_start_a_second_run()
    {
        var gate = new TaskCompletionSource();
        var runs = 0;
        var command = new AsyncRelayCommand(() => { runs++; return gate.Task; });

        var first = command.ExecuteAsync();
        command.Execute(null);
        await command.ExecuteAsync();
        gate.SetResult();
        await first;
        Assert.Equal(1, runs);
    }

    [Fact]
    public async Task An_exception_is_kept_in_Error_passed_to_the_callback_and_raised_as_Failed()
    {
        var boom = new InvalidOperationException("save failed");
        Exception? callback = null, raised = null;
        var command = new AsyncRelayCommand(async () => { await Task.Yield(); throw boom; }, onError: ex => callback = ex);
        command.Failed += (_, ex) => raised = ex;

        await command.ExecuteAsync();

        Assert.Same(boom, command.Error);
        Assert.Same(boom, callback);
        Assert.Same(boom, raised);
        Assert.False(command.IsRunning);
        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public async Task A_synchronous_throw_is_surfaced_too_and_the_next_run_clears_Error()
    {
        var fail = true;
        var command = new AsyncRelayCommand(() => fail ? throw new InvalidOperationException("sync") : Task.CompletedTask);

        await command.ExecuteAsync();
        Assert.IsType<InvalidOperationException>(command.Error);

        fail = false;
        await command.ExecuteAsync();
        Assert.Null(command.Error);
    }

    [Fact]
    public async Task Cancellation_ends_the_run_without_an_error()
    {
        var command = new AsyncRelayCommand(() => Task.FromCanceled(new CancellationToken(true)));
        var failed = false;
        command.Failed += (_, _) => failed = true;

        await command.ExecuteAsync();
        Assert.Null(command.Error);
        Assert.False(failed);
        Assert.False(command.IsRunning);
    }

    [Fact]
    public async Task The_predicate_gates_execution_and_receives_the_parameter()
    {
        object? got = null;
        var command = new AsyncRelayCommand(p => { got = p; return Task.CompletedTask; }, p => p is "go");

        Assert.False(command.CanExecute("stop"));
        await command.ExecuteAsync("stop");
        Assert.Null(got);

        Assert.True(command.CanExecute("go"));
        await command.ExecuteAsync("go");
        Assert.Equal("go", got);
    }

    [Fact]
    public async Task IsRunning_and_Error_raise_PropertyChanged()
    {
        var command = new AsyncRelayCommand(() => throw new InvalidOperationException());
        var names = new List<string?>();
        command.PropertyChanged += (_, e) => names.Add(e.PropertyName);

        await command.ExecuteAsync();
        Assert.Equal([nameof(AsyncRelayCommand.IsRunning), nameof(AsyncRelayCommand.Error), nameof(AsyncRelayCommand.IsRunning)], names);
    }

    [Fact]
    public void NotifyCanExecuteChanged_raises_the_event()
    {
        var command = new AsyncRelayCommand(() => Task.CompletedTask, () => true);
        var raised = 0;
        command.CanExecuteChanged += (_, _) => raised++;
        command.NotifyCanExecuteChanged();
        Assert.Equal(1, raised);
    }
}
