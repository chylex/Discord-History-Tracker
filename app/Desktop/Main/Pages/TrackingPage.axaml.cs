using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace DHT.Desktop.Main.Pages {
	[SuppressMessage("ReSharper", "MemberCanBeInternal")]
	public sealed class TrackingPage : UserControl {
		private bool isCopyingScript;

		public TrackingPage() {
			InitializeComponent();
		}

		private void InitializeComponent() {
			AvaloniaXamlLoader.Load(this);
		}

		public async void CopyTrackingScriptButton_OnClick(object? sender, RoutedEventArgs e) {
			if (DataContext is TrackingPageModel model) {
				var button = this.FindControl<Button>("CopyTrackingScript");
				var originalText = button.Content;
				button.MinWidth = button.Bounds.Width;

				if (await model.OnClickCopyTrackingScript() && !isCopyingScript) {
					isCopyingScript = true;
					button.Content = "Script Copied!";

					await Task.Delay(TimeSpan.FromSeconds(2));
					button.Content = originalText;
					isCopyingScript = false;
				}
			}
		}
	}
}
