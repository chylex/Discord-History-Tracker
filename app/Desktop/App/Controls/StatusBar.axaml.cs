using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;

namespace DHT.Desktop.App.Controls;

[SuppressMessage("ReSharper", "MemberCanBeInternal")]
public sealed partial class StatusBar : UserControl {
	public StatusBar() {
		InitializeComponent();
	}
}
