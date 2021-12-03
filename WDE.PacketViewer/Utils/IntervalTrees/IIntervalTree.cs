using System;
using System.Runtime.Versioning;

namespace WDE.PacketViewer.Utils.IntervalTrees;

[RequiresPreviewFeatures]
internal interface IIntervalTree<TKey, TValue> where TKey : INumber<TKey> where TValue : struct
{
    void Add(TKey from, TKey to, TValue value);
    Interval<TKey, TValue>? Query(TKey at);
    Interval<TKey, TValue>? QueryBefore(TKey at);
    Interval<TKey, TValue>? QueryAfter(TKey at);
}