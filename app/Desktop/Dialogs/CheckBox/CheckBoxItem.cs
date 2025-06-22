using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using PropertyChanged.SourceGenerator;

namespace DHT.Desktop.Dialogs.CheckBox;

partial interface ICheckBoxItem : INotifyPropertyChanged {
	public string Title { get; }
	public bool? IsChecked { get; set; }
	
	public ImmutableArray<ICheckBoxItem> Children { get; }
	
	void NotifyIsCheckedChanged();
	
	public static IEnumerable<ICheckBoxItem> GetAllRecursively(IEnumerable<ICheckBoxItem> items) {
		Stack<ICheckBoxItem> stack = new Stack<ICheckBoxItem>(items);
		
		while (stack.TryPop(out var item)) {
			yield return item;
			
			foreach (ICheckBoxItem child in item.Children) {
				stack.Push(child);
			}
		}
	}
	
	sealed class NonLeaf : ICheckBoxItem {
		public string Title { get; }
		
		public bool? IsChecked {
			get {
				if (Children.Count(static child => child.IsChecked == true) == Children.Length) {
					return true;
				}
				else if (Children.Count(static child => child.IsChecked == false) == Children.Length) {
					return false;
				}
				else {
					return null;
				}
			}
			
			set {
				foreach (ICheckBoxItem child in Children) {
					if (child is Leaf leaf) {
						leaf.SetCheckedFromParent(value);
					}
					else {
						child.IsChecked = value;
					}
				}
				
				NotifyIsCheckedChanged();
				parent?.NotifyIsCheckedChanged();
			}
		}
		
		public ImmutableArray<ICheckBoxItem> Children { get; }
		
		public event PropertyChangedEventHandler? PropertyChanged;
		
		private readonly ICheckBoxItem? parent;
		
		public NonLeaf(string title, ICheckBoxItem? parent, Func<ICheckBoxItem, ImmutableArray<ICheckBoxItem>> children) {
			this.parent = parent;
			this.Title = title;
			this.Children = children(this);
		}
		
		public void NotifyIsCheckedChanged() {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
		}
	}
	
	partial class Leaf(string title, ICheckBoxItem? parent, bool isChecked) : ICheckBoxItem {
		public string Title { get; } = title;
		
		public ImmutableArray<ICheckBoxItem> Children => ImmutableArray<ICheckBoxItem>.Empty;
		
		public readonly ICheckBoxItem? parent = parent;
		
		[Notify]
		private bool? isChecked = isChecked;
		
		private bool notifyParent = true;
		
		public void SetCheckedFromParent(bool? isChecked) {
			notifyParent = false;
			IsChecked = isChecked;
			notifyParent = true;
		}
		
		private void OnIsCheckedChanged() {
			if (notifyParent) {
				parent?.NotifyIsCheckedChanged();
			}
		}
		
		void ICheckBoxItem.NotifyIsCheckedChanged() {
			OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsChecked)));
		}
	}
	
	sealed class Leaf<T>(string title, ICheckBoxItem? parent, T value, bool isChecked) : Leaf(title, parent, isChecked) {
		public T Value => value;
	}
}
