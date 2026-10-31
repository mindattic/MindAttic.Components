using System.Windows.Input;

namespace MindAttic.Components.Wpf.Mvvm;

/// <summary>An <see cref="ICommand"/> that runs an async delegate (M.COMP-T156): Load, Save, Test. While it runs,
/// <see cref="CanExecute"/> is false and <see cref="IsRunning"/> is true, so a bound button disables itself and a second
/// click can't start a second run; <see cref="CanExecuteChanged"/> is raised as it starts and again as it finishes. An
/// exception the delegate throws is never lost: it is kept in <see cref="Error"/>, passed to the <c>onError</c> callback
/// and raised as <see cref="Failed"/>. Cancellation (<see cref="OperationCanceledException"/>) ends the run quietly.
/// Like <see cref="RelayCommand"/>, the optional predicate is also re-asked whenever WPF's <see cref="CommandManager"/>
/// suggests it.</summary>
public sealed class AsyncRelayCommand : ObservableObject, ICommand
{
    private readonly Func<object?, Task> _execute;
    private readonly Func<object?, bool>? _canExecute;
    private readonly Action<Exception>? _onError;
    private EventHandler? _canExecuteChanged;
    private bool _isRunning;
    private Exception? _error;

    /// <summary>A command running <paramref name="execute"/> with the command parameter.</summary>
    public AsyncRelayCommand(Func<object?, Task> execute, Func<object?, bool>? canExecute = null, Action<Exception>? onError = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
        _onError = onError;
    }

    /// <summary>A command running <paramref name="execute"/>, ignoring the command parameter.</summary>
    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null, Action<Exception>? onError = null)
        : this(_ => (execute ?? throw new ArgumentNullException(nameof(execute)))(),
            canExecute is null ? null : _ => canExecute(), onError) { }

    /// <summary>Raised on start and finish of a run, by <see cref="NotifyCanExecuteChanged"/>, and whenever WPF's
    /// <see cref="CommandManager"/> suggests a requery.</summary>
    public event EventHandler? CanExecuteChanged
    {
        add
        {
            _canExecuteChanged += value;
            CommandManager.RequerySuggested += value;
        }
        remove
        {
            _canExecuteChanged -= value;
            CommandManager.RequerySuggested -= value;
        }
    }

    /// <summary>Raised with the exception when a run fails (after <see cref="Error"/> is set and the callback has run).</summary>
    public event EventHandler<Exception>? Failed;

    /// <summary>True from the start of a run until it finishes.</summary>
    public bool IsRunning
    {
        get => _isRunning;
        private set => Set(ref _isRunning, value);
    }

    /// <summary>The exception the last run threw, or null; cleared when the next run starts.</summary>
    public Exception? Error
    {
        get => _error;
        private set => Set(ref _error, value);
    }

    /// <summary>False while a run is in progress; otherwise the predicate (true when there is none).</summary>
    public bool CanExecute(object? parameter) => !IsRunning && (_canExecute?.Invoke(parameter) ?? true);

    /// <summary>Starts a run (fire and forget: failures land in <see cref="Error"/> and <see cref="Failed"/>).</summary>
    public void Execute(object? parameter) => _ = ExecuteAsync(parameter);

    /// <summary>Runs the delegate unless <see cref="CanExecute"/> is false. The returned task never faults: a failure is
    /// surfaced through <see cref="Error"/>, the callback and <see cref="Failed"/>.</summary>
    public async Task ExecuteAsync(object? parameter = null)
    {
        if (!CanExecute(parameter)) return;
        Error = null;
        IsRunning = true;
        NotifyCanExecuteChanged();
        try
        {
            await _execute(parameter);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Error = ex;
            _onError?.Invoke(ex);
            Failed?.Invoke(this, ex);
        }
        finally
        {
            IsRunning = false;
            NotifyCanExecuteChanged();
        }
    }

    /// <summary>Raises <see cref="CanExecuteChanged"/> now, e.g. after something the predicate reads has changed.</summary>
    public void NotifyCanExecuteChanged() => _canExecuteChanged?.Invoke(this, EventArgs.Empty);
}
