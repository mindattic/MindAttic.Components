using System.Windows.Input;

namespace MindAttic.Components.Wpf.Mvvm;

/// <summary>An <see cref="ICommand"/> from a delegate, with or without a parameter. CanExecute is re-asked whenever WPF's
/// <see cref="CommandManager"/> suggests it (focus moves, input), so a bound button enables and disables itself.</summary>
public sealed class RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null) : ICommand
{
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute is null ? null : _ => canExecute()) { }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => execute(parameter);
}
