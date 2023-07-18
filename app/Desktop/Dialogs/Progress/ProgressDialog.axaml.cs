using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace DHT.Desktop.Dialogs.Progress;

[SuppressMessage("ReSharper", "MemberCanBeInternal")]
public sealed partial class ProgressDialog : Window {
	private bool isFinished = false;

	public ProgressDialog() {
		InitializeComponent();
	}

	public void OnOpened(object? sender, EventArgs e) {
		if (DataContext is ProgressDialogModel model) {
			Task.Run(model.StartTask).ContinueWith(OnFinished, TaskScheduler.FromCurrentSynchronizationContext());
		}
	}

	public void OnClosing(object? sender, WindowClosingEventArgs e) {
		e.Cancel = !isFinished;
	}

	private void OnFinished(Task task) {
		isFinished = true;
		Close();
	}
}
