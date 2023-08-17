using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace TcpMultiplexer.Smoker.Common
{
    public class ObservableCollectionView<T> :
    INotifyCollectionChanged,
    INotifyPropertyChanged,
    IList<T>,
    ICollection<T>,
    IEnumerable<T>,
    IEnumerable,
    IList,
    ICollection,
    IReadOnlyList<T>,
    IReadOnlyCollection<T>,
    IDisposable
    {
        private readonly IList<T> _internal;
        private readonly ObservableCollection<T> _filtered;
        private Predicate<T> _filter;

        public Predicate<T> Filter
        {
            get => this._filter;
            set
            {
                this._filter = value == null ? (Predicate<T>)(x => true) : value;
                this.Merge();
            }
        }

        private void Merge()
        {
            int index = 0;
            foreach (T obj in (IEnumerable<T>)this._internal)
            {
                if (index < this._filtered.Count)
                {
                    if (this._filter(obj))
                    {
                        if ((object)obj != (object)this._filtered[index])
                            this._filtered.Insert(index, obj);
                        ++index;
                    }
                    else if ((object)obj == (object)this._filtered[index])
                        this._filtered.RemoveAt(index);
                }
                else if (this._filter(obj))
                {
                    this._filtered.Add(obj);
                    ++index;
                }
            }
            while (index < this._filtered.Count)
                this._filtered.RemoveAt(index);
        }

        public IList<T> Source => this._internal;

        public ObservableCollectionView(IList<T> src = null)
        {
            this._internal = src ?? (IList<T>)new ObservableCollection<T>();
            this._filtered = new ObservableCollection<T>();
            this._filtered.AddRange<T>((IEnumerable<T>)this._internal);
            if (!(this._internal is INotifyCollectionChanged collectionChanged))
                throw new ArgumentException("Source collection must implement INotifyCollectionChanged");
            collectionChanged.CollectionChanged += new NotifyCollectionChangedEventHandler(this.SourceCollectionChanged);
            this._filtered.CollectionChanged += new NotifyCollectionChangedEventHandler(this.ViewCollectionChanged);
            ((INotifyPropertyChanged)this._filtered).PropertyChanged += new PropertyChangedEventHandler(this.ViewPropertyChanged);
            this._filter = (Predicate<T>)(x => true);
        }

        private void ViewPropertyChanged(object s, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if (propertyChanged == null)
                return;
            propertyChanged((object)this, propertyChangedEventArgs);
        }

        private void ViewCollectionChanged(object s, NotifyCollectionChangedEventArgs args)
        {
            NotifyCollectionChangedEventHandler collectionChanged = this.CollectionChanged;
            if (collectionChanged == null)
                return;
            collectionChanged((object)this, args);
        }

        private void SourceCollectionChanged(object s, NotifyCollectionChangedEventArgs args)
        {
            if (args.Action == NotifyCollectionChangedAction.Add)
                this._filtered.AddRange<T>((IEnumerable<T>)args.NewItems.OfType<T>().Where<T>((Func<T, bool>)(x => this._filter(x))).ToArray<T>());
            else if (args.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (T obj in args.OldItems.OfType<T>().Where<T>((Func<T, bool>)(x => this._filter(x))))
                    this._filtered.Remove(obj);
            }
            else if (args.Action == NotifyCollectionChangedAction.Replace)
            {
                for (int index1 = 0; index1 < args.NewItems.Count; ++index1)
                {
                    T newItem = (T)args.NewItems[index1];
                    int index2 = this._filtered.IndexOf(newItem);
                    if (index2 >= 0)
                        this._filtered[index2] = newItem;
                }
            }
            else
            {
                if (args.Action != NotifyCollectionChangedAction.Reset)
                    return;
                this._filtered.Clear();
                this._filtered.AddRange<T>(this._internal.Where<T>((Func<T, bool>)(x => this._filter(x))));
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
            set => this[index] = (T)value;
        }

        public void Add(T item) => this._filtered.Add(item);

        public void Clear() => this._filtered.Clear();

        public bool Contains(T item) => this._filtered.Contains(item);

        public void CopyTo(T[] array, int index) => this._filtered.CopyTo(array, index);

        public IEnumerator<T> GetEnumerator()
        {
            foreach (T obj in (Collection<T>)this._filtered)
                yield return obj;
        }

        public int IndexOf(T item) => this._filtered.IndexOf(item);

        public void Insert(int index, T item) => this._filtered.Insert(index, item);

        public bool Remove(T item) => this._filtered.Remove(item);

        public void RemoveAt(int index) => this._filtered.RemoveAt(index);

        public int Count => this._filtered.Count;

        bool ICollection<T>.IsReadOnly => false;

        public T this[int index]
        {
            get => this._filtered[index];
            set => this._filtered[index] = value;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Move(int oldIndex, int newIndex) => this._filtered.Move(oldIndex, newIndex);

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        IEnumerator IEnumerable.GetEnumerator() => (IEnumerator)this.GetEnumerator();

        public void Dispose()
        {
            if (!(this._internal is INotifyCollectionChanged collectionChanged))
                return;
            collectionChanged.CollectionChanged -= new NotifyCollectionChangedEventHandler(this.SourceCollectionChanged);
        }
    }
    public static class Extensions
    {
        public static void AddRange<T>(this IList<T> list, IEnumerable<T> other)
        {
            if (list is List<T> objList)
            {
                objList.AddRange(other);
            }
            else
            {
                foreach (T obj in other)
                    list.Add(obj);
            }
        }

        public static IEnumerable<T> For<T>(this IReadOnlyList<T> list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    T item = default(T);
                    bool isOk = true;
                    try
                    {
                        item = list[i];
                    }
                    catch
                    {
                        isOk = false;
                    }

                    if (isOk)
                        yield return item;
                    else yield break;
                }
            }
        
        private static readonly CultureInfo EN_US = new CultureInfo("en-US");
       
        public static string ToDebugString(this string s)
        {
            return s == null ? "null" : s.Replace("\n", "\\n").Replace("\t", "\\t");
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string AsJs(this double value)
        {
            return $"{value.ToString(EN_US)}";
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string AsPx(this int value)
        {
            return $"{value}px";
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string AsPx(this double value)
        {
            return $"{value.ToString(EN_US)}px";
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string AsPx(this double? value)
        {
            return $"{(value??0).ToString(EN_US)}px";
        }
    }
}