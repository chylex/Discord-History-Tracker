using System;
using System.Collections.Generic;

namespace DHT.Utils.Collections;

public static class LinqExtensions {
	public static IEnumerable<TItem> DistinctByKeyStable<TItem, TKey>(this IEnumerable<TItem> collection, Func<TItem, TKey> getKeyFromItem) where TKey : IEquatable<TKey> {
		HashSet<TKey>? seenKeys = null;

		foreach (var item in collection) {
			seenKeys ??= new HashSet<TKey>();

			if (seenKeys.Add(getKeyFromItem(item))) {
				yield return item;
			}
		}
	}
}
