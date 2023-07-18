using System.Collections.Generic;

namespace DHT.Utils.Collections;

public sealed class MultiDictionary<TKey, TValue> where TKey : notnull {
	private readonly Dictionary<TKey, List<TValue>> dict = new();

	public void Add(TKey key, TValue value) {
		if (!dict.TryGetValue(key, out var list)) {
			dict[key] = list = new List<TValue>();
		}

		list.Add(value);
	}

	public List<TValue>? GetListOrNull(TKey key) {
		return dict.TryGetValue(key, out var list) ? list : null;
	}
}
