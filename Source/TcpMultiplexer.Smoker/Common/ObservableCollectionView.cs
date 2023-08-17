using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace TcpMultiplexer.Smoker.Common;

public class ObservableCollectionView<TDst, TSrc> :
    IObservableCollectionView<TDst, TSrc>,
    INotifyCollectionChanged,
    INotifyPropertyChanged,
    IList<TDst>,
    ICollection<TDst>,
    IEnumerable<TDst>,
    IEnumerable,
    IList,
    ICollection,
    IReadOnlyList<TDst>,
    IReadOnlyCollection<TDst>,
    IDisposable
    where TDst : IViewFor<TSrc>, IEquatable<TDst>
{
    private readonly Func<TSrc, TDst> _convertItem;
    private readonly IList<TSrc> _internal;
    private readonly ObservableCollection<TDst> _filtered;
    private Predicate<TDst> _filter;
    private static readonly Predicate<TDst> _trueFilter = (Predicate<TDst>)(x => true);

    public Predicate<TDst> Filter
    {
        get => !(this._filter == ObservableCollectionView<TDst, TSrc>._trueFilter) ? this._filter : (Predicate<TDst>)null;
        set
        {
            this._filter = value == null ? ObservableCollectionView<TDst, TSrc>._trueFilter : value;
            this.Merge();
        }
    }

    private void Merge()
    {
        int index = 0;
        foreach (TSrc src in (IEnumerable<TSrc>)this._internal)
        {
            if (index < this._filtered.Count)
            {
                TDst dst;
                if (this._filter(dst = this._convertItem(src)))
                {
                    if ((object)src != (object)this._filtered[index].Source)
                        this._filtered.Insert(index, dst);
                    ++index;
                }
                else if ((object)src == (object)this._filtered[index].Source)
                    this._filtered.RemoveAt(index);
            }
            else
            {
                TDst dst;
                if (this._filter(dst = this._convertItem(src)))
                {
                    this._filtered.Add(dst);
                    ++index;
                }
            }
        }
        while (index < this._filtered.Count)
            this._filtered.RemoveAt(index);
    }

    public ObservableCollectionView(Func<TSrc, TDst> convertItem, IList<TSrc> src)
    {
        this._convertItem = convertItem;
        this._internal = src;
        this._filtered = new ObservableCollection<TDst>();
        this._filtered.AddRange<TDst>(this._internal.Select<TSrc, TDst>(this._convertItem));
        if (!(src is INotifyCollectionChanged collectionChanged))
            throw new ArgumentException("src must implement INotifyCollectionChanged");
        collectionChanged.CollectionChanged += new NotifyCollectionChangedEventHandler(this.OnSrcCollectionChangesOnCollectionChanged);
        this._filtered.CollectionChanged += (NotifyCollectionChangedEventHandler)((s, e) => this.ViewCollectionChanged(e));
        ((INotifyPropertyChanged)this._filtered).PropertyChanged += (PropertyChangedEventHandler)((s, e) => this.ViewPropertyChanged(e));
        this._filter = ObservableCollectionView<TDst, TSrc>._trueFilter;
    }

    private void OnSrcCollectionChangesOnCollectionChanged(
        object s,
        NotifyCollectionChangedEventArgs e)
    {
        this.SourceCollectionChanged(e);
    }

    private void ViewPropertyChanged(PropertyChangedEventArgs propertyChangedEventArgs)
    {
        PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
        if (propertyChanged == null)
            return;
        propertyChanged((object)this, propertyChangedEventArgs);
    }

    private void ViewCollectionChanged(NotifyCollectionChangedEventArgs args)
    {
        NotifyCollectionChangedEventHandler collectionChanged = this.CollectionChanged;
        if (collectionChanged == null)
            return;
        collectionChanged((object)this, args);
    }

    public bool IsFiltered => this.Filter != null;

    private void SourceCollectionChanged(NotifyCollectionChangedEventArgs args)
    {
        if (args.Action == NotifyCollectionChangedAction.Add)
        {
            TDst[] array = args.NewItems.OfType<TSrc>().Select<TSrc, TDst>(this._convertItem).Where<TDst>((Func<TDst, bool>)(x => this._filter(x))).ToArray<TDst>();
            if (!this.IsFiltered)
            {
                if (args.NewStartingIndex == this._filtered.Count)
                {
                    this._filtered.AddRange<TDst>((IEnumerable<TDst>)array);
                }
                else
                {
                    foreach (TDst dst in ((IEnumerable<TDst>)array).Reverse<TDst>())
                        this._filtered.Insert(args.NewStartingIndex, dst);
                }
            }
            else
                this._filtered.AddRange<TDst>((IEnumerable<TDst>)array);
        }
        else if (args.Action == NotifyCollectionChangedAction.Remove)
        {
            foreach (TDst dst in args.OldItems.OfType<TSrc>().Select<TSrc, TDst>(this._convertItem).Where<TDst>((Func<TDst, bool>)(x => this._filter(x))))
                this._filtered.Remove(dst);
        }
        else if (args.Action == NotifyCollectionChangedAction.Replace)
        {
            if (this.IsFiltered)
                throw new NotSupportedException();
            for (int index = 0; index < args.NewItems.Count; ++index)
                this._filtered[index + args.OldStartingIndex] = this._convertItem((TSrc)args.NewItems[index]);
        }
        else
        {
            if (args.Action != NotifyCollectionChangedAction.Reset)
                return;
            this._filtered.Clear();
            this._filtered.AddRange<TDst>(this._internal.Select<TSrc, TDst>(this._convertItem));
        }
    }

    public void CopyTo(Array array, int index) => ((ICollection)this._filtered).CopyTo(array, index);

    public bool IsSynchronized => ((ICollection)this._filtered).IsSynchronized;

    public object SyncRoot => ((ICollection)this._filtered).SyncRoot;

    public int Add(object value) => ((IList)this._filtered).Add(value);

    public bool Contains(object value) => ((IList)this._filtered).Contains(value);

    public int IndexOf(object value) => ((IList)this._filtered).IndexOf(value);

    public void Insert(int index, object value) => ((IList)this._filtered).Insert(index, value);

    public void Remove(object value) => ((IList)this._filtered).Remove(value);

    public bool IsFixedSize => ((IList)this._filtered).IsFixedSize;

    bool IList.IsReadOnly => false;

    object IList.this[int index]
    {
        get => (object)this[index];
        set => this[index] = (TDst)value;
    }

    public void Add(TDst item) => this._filtered.Add(item);

    public void Clear() => this._filtered.Clear();

    public bool Contains(TDst item) => this._filtered.Contains(item);

    public void CopyTo(TDst[] array, int index) => this._filtered.CopyTo(array, index);

    public IEnumerator<TDst> GetEnumerator()
    {
        foreach (TDst dst in (Collection<TDst>)this._filtered)
            yield return dst;
    }

    public int IndexOf(TDst item) => this._filtered.IndexOf(item);

    public void Insert(int index, TDst item) => this._filtered.Insert(index, item);

    public bool Remove(TDst item) => this._filtered.Remove(item);

    public void RemoveAt(int index) => this._filtered.RemoveAt(index);

    public int Count => this._filtered.Count;

    bool ICollection<TDst>.IsReadOnly => false;

    public TDst this[int index]
    {
        get => this._filtered[index];
        set => this._filtered[index] = value;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public void Move(int oldIndex, int newIndex) => this._filtered.Move(oldIndex, newIndex);

    public event NotifyCollectionChangedEventHandler CollectionChanged;

    IEnumerator IEnumerable.GetEnumerator() => (IEnumerator)this.GetEnumerator();

    public void Dispose() => ((INotifyCollectionChanged)this._internal).CollectionChanged -= new NotifyCollectionChangedEventHandler(this.OnSrcCollectionChangesOnCollectionChanged);
}