using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DHT.Desktop.Main.Pages;

[SuppressMessage("ReSharper", "MemberCanBeInternal")]
public sealed partial class TrackingPage : UserControl {
	private bool isCopyingScript;

	public TrackingPage() {
		InitializeComponent();
	}

	public async void CopyTrackingScriptButton_OnClick(object? sender, RoutedEventArgs e) {
		if (DataContext is TrackingPageModel model) {
			var originalText = CopyTrackingScript.Content;
			CopyTrackingScript.MinWidth = CopyTrackingScript.Bounds.Width;

			if (await model.OnClickCopyTrackingScript() && !isCopyingScript) {
				isCopyingScript = true;
				CopyTrackingScript.Content = "Script Copied!";

				await Task.Delay(TimeSpan.FromSeconds(2));
				CopyTrackingScript.Content = originalText;
				isCopyingScript = false;
			}
		}
	}
}
