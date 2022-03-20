using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DHT.Desktop.Main.Pages {
	[SuppressMessage("ReSharper", "MemberCanBeInternal")]
	public sealed class ViewerPage : UserControl {
		public ViewerPage() {
			InitializeComponent();
		}

		private void InitializeComponent() {
			AvaloniaXamlLoader.Load(this);
		}

		public void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e) {
			if (DataContext is ViewerPageModel model) {
				model.SetPageVisible(true);
			}
		}

		public void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e) {
			if (DataContext is ViewerPageModel model) {
				model.SetPageVisible(false);
			}
		}
	}
}
