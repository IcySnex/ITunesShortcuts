using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace ITunesShortcuts.Helpers;

public class ObservableRangeCollection<T> : ObservableCollection<T>
{
    public void AddRange(
        IEnumerable<T> items)
    {
        try
        {
            CheckReentrancy();

            foreach (T item in items)
                Items.Add(item);
        }
        finally
        {
            OnCollectionChanged(new(NotifyCollectionChangedAction.Reset));
        }
    }

    public new void Add(
        T item)
    {
        try
        {
            Items.Add(item);
        }
        finally
        {
            OnCollectionChanged(new(NotifyCollectionChangedAction.Reset));
        }
    }


    public new bool Remove(
        T item)
    {
        try
        {
            return Items.Remove(item);
        }
        finally
        {
            OnCollectionChanged(new(NotifyCollectionChangedAction.Reset));
        }
    }

    public new void RemoveAt(
        int index)
    {
        try
        {
            Items.RemoveAt(index);
        }
        finally
        {
            OnCollectionChanged(new(NotifyCollectionChangedAction.Reset));
        }
    }
}