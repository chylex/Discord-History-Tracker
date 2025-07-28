using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace DHT.Desktop.Dialogs.CheckBox;

sealed class CheckBoxItemList<TKey, TValue> where TKey : notnull {
	private readonly List<INode> rootNodes = [];
	private readonly Dictionary<TKey, List<INode>> parentNodes = [];
	
	public void AddParent(TKey key, string title) {
		if (!parentNodes.ContainsKey(key)) {
			List<INode> children = [];
			rootNodes.Add(new INode.NonLeaf(title, children));
			parentNodes[key] = children;
		}
	}
	
	public void Add(TValue value, string title, bool isChecked = false) {
		rootNodes.Add(new INode.Leaf(title, value, isChecked));
	}
	
	public void Add(TKey key, TValue value, string title, bool isChecked = false) {
		parentNodes.GetValueOrDefault(key, rootNodes).Add(new INode.Leaf(title, value, isChecked));
	}
	
	public ImmutableArray<ICheckBoxItem> ToCheckBoxItems() {
		return [..rootNodes.Select(static node => node.ToCheckBoxItem(null))];
	}
	
	private interface INode {
		ICheckBoxItem ToCheckBoxItem(ICheckBoxItem? parent);
		
		sealed record NonLeaf(string Title, List<INode> Children) : INode {
			public ICheckBoxItem ToCheckBoxItem(ICheckBoxItem? parent) {
				return new ICheckBoxItem.NonLeaf(Title, parent, self => [..Children.Select(child => child.ToCheckBoxItem(self))]);
			}
		}
		
		sealed record Leaf(string Title, TValue Value, bool IsChecked) : INode {
			public ICheckBoxItem ToCheckBoxItem(ICheckBoxItem? parent) {
				return new ICheckBoxItem.Leaf<TValue>(Title, parent, Value, IsChecked);
			}
		}
	}
}
