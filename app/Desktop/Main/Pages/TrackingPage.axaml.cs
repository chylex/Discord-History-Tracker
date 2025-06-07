using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DHT.Desktop.Main.Pages;

[SuppressMessage("ReSharper", "MemberCanBeInternal")]
public sealed partial class TrackingPage : UserControl {
	private readonly HashSet<Button> copyingButtons = new (ReferenceEqualityComparer.Instance);
	
	public TrackingPage() {
		InitializeComponent();
	}
	
	public async void CopyTrackingScriptButton_OnClick(object? sender, RoutedEventArgs e) {
		await HandleCopyButton(CopyTrackingScript, "Script Copied!", static model => model.OnClickCopyTrackingScript());
	}
	
	public async void CopyConnectionScriptButton_OnClick(object? sender, RoutedEventArgs e) {
		await HandleCopyButton(CopyConnectionCode, "Code Copied!", static model => model.OnClickCopyConnectionCode());
	}
	
	private async Task HandleCopyButton(Button button, string copiedText, Func<TrackingPageModel, Task<bool>> onClick) {
		if (DataContext is TrackingPageModel model) {
			object? originalText = button.Content;
			button.MinWidth = button.Bounds.Width;
			
			if (await onClick(model) && copyingButtons.Add(button)) {
				button.IsEnabled = false;
				button.Content = copiedText;
				
				try {
					await Task.Delay(TimeSpan.FromSeconds(2));
				} finally {
					copyingButtons.Remove(button);
					button.IsEnabled = true;
					button.Content = originalText;
				}
			}
		}
	}
}
