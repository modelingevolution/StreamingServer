using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace TcpMultiplexer.Smoker.Common;

public interface IObservableCollectionView<TDst, TSrc> :
    INotifyCollectionChanged,
    INotifyPropertyChanged,
    IList<TDst>,
    ICollection<TDst>,
    IEnumerable<TDst>,
    IEnumerable,
    IList,
    ICollection,
    IReadOnlyList<TDst>,
    IReadOnlyCollection<TDst>
    where TDst : IViewFor<TSrc>, IEquatable<TDst>
{
}