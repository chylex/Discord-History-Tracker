using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;

namespace DHT.Desktop.App.Windows;

[SuppressMessage("ReSharper", "MemberCanBeInternal")]
public sealed partial class AboutWindow : Window {
	public AboutWindow() {
		InitializeComponent();
	}
}
