using System.Collections.ObjectModel;

namespace MindAttic.Components.Wpf.Mvvm;

/// <summary>Refreshes a bound <see cref="ObservableCollection{T}"/> in place, so the controls showing it keep their state.</summary>
public static class CollectionSync
{
    /// <summary>
    /// Makes <paramref name="target"/> hold exactly <paramref name="desired"/>, in order, by moving existing items rather
    /// than re-adding them, so bound controls keep their state (focus, selection, typed text, hover) across refreshes.
    /// </summary>
    public static void ReorderTo<T>(this ObservableCollection<T> target, IReadOnlyList<T> desired) where T : class
    {
        for (var i = 0; i < desired.Count; i++)
        {
            if (i < target.Count && ReferenceEquals(target[i], desired[i])) continue;
            var at = IndexOf(target, desired[i], i + 1);
            if (at >= 0) target.Move(at, i);
            else target.Insert(i, desired[i]);
        }
        while (target.Count > desired.Count) target.RemoveAt(target.Count - 1);
    }

    /// <summary>Keyed sync: reuses rows whose key survives (passing each to <paramref name="update"/>), creates new ones
    /// with <paramref name="create"/>, drops the rest, and orders them as <paramref name="source"/>. A key repeated in
    /// <paramref name="source"/> keeps only its first item, so one row is never placed twice.</summary>
    public static void SyncBy<TRow, TSource, TKey>(
        this ObservableCollection<TRow> target, IEnumerable<TSource> source,
        Func<TSource, TKey> sourceKey, Func<TRow, TKey> rowKey,
        Func<TSource, TRow> create, Action<TRow, TSource> update) where TRow : class where TKey : notnull
    {
        var existing = new Dictionary<TKey, TRow>();
        foreach (var r in target) existing.TryAdd(rowKey(r), r);
        var seen = new HashSet<TKey>();
        var desired = new List<TRow>();
        foreach (var s in source)
        {
            if (!seen.Add(sourceKey(s))) continue;
            if (existing.TryGetValue(sourceKey(s), out var row)) update(row, s);
            else row = create(s);
            desired.Add(row);
        }
        target.ReorderTo(desired);
    }

    private static int IndexOf<T>(ObservableCollection<T> items, T item, int start) where T : class
    {
        for (var i = start; i < items.Count; i++)
            if (ReferenceEquals(items[i], item)) return i;
        return -1;
    }
}
