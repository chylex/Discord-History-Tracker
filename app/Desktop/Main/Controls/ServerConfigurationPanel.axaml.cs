using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;

namespace DHT.Desktop.Main.Controls;

[SuppressMessage("ReSharper", "MemberCanBeInternal")]
public sealed partial class ServerConfigurationPanel : UserControl {
	public ServerConfigurationPanel() {
		InitializeComponent();
	}
}
