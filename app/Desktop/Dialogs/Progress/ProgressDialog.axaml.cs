using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace DHT.Desktop.Dialogs.Progress;

[SuppressMessage("ReSharper", "MemberCanBeInternal")]
public sealed partial class ProgressDialog : Window {
	private bool isFinished = false;
	private Task progressTask = Task.CompletedTask;

	public ProgressDialog() {
		InitializeComponent();
	}

	public void OnOpened(object? sender, EventArgs e) {
		if (DataContext is ProgressDialogModel model) {
			progressTask = Task.Run(model.StartTask);
			progressTask.ContinueWith(OnFinished, TaskScheduler.FromCurrentSynchronizationContext());
		}
	}

	public void OnClosing(object? sender, WindowClosingEventArgs e) {
		e.Cancel = !isFinished;
	}

	private void OnFinished(Task task) {
		isFinished = true;
		Close();
	}

	public async Task ShowProgressDialog(Window owner) {
		await ShowDialog(owner);
		await progressTask;
	}
}
