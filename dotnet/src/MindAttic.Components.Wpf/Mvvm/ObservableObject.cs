using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MindAttic.Components.Wpf.Mvvm;

/// <summary>A view model base: <see cref="Set{T}"/> stores a field and raises <see cref="PropertyChanged"/> only when the
/// value changed; <see cref="Raise"/> announces a computed property. Lifted from the copies the Orchestrator and Workflow
/// Desktop each kept (M.COMP-T11).</summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Sets <paramref name="field"/> and raises PropertyChanged for <paramref name="name"/>; false (and no event)
    /// when the value is unchanged.</summary>
    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        Raise(name);
        return true;
    }

    /// <summary>Raises PropertyChanged for <paramref name="name"/> (the calling property by default).</summary>
    protected void Raise([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
