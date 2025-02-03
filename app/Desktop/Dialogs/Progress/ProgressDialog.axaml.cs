using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Avalonia.Controls;
using DHT.Desktop.Dialogs.Message;
using DHT.Utils.Logging;

namespace DHT.Desktop.Dialogs.Progress;

[SuppressMessage("ReSharper", "MemberCanBeInternal")]
public sealed partial class ProgressDialog : Window {
	private static readonly Log Log = Log.ForType<ProgressDialog>();
	
	internal static async Task Show(Window owner, string title, Func<ProgressDialog, IProgressCallback, Task> action) {
		var taskCompletionSource = new TaskCompletionSource();
		var dialog = new ProgressDialog();
		
		dialog.DataContext = new ProgressDialogModel(title, async callbacks => {
			try {
				await action(dialog, callbacks[0]);
				taskCompletionSource.SetResult();
			} catch (Exception e) {
				taskCompletionSource.SetException(e);
			}
		});
		
		await dialog.ShowProgressDialog(owner);
		await taskCompletionSource.Task;
	}
	
	internal static async Task ShowIndeterminate(Window owner, string title, string message, Func<ProgressDialog, Task> action) {
		var taskCompletionSource = new TaskCompletionSource();
		var dialog = new ProgressDialog();
		
		dialog.DataContext = new ProgressDialogModel(title, async callbacks => {
			await callbacks[0].UpdateIndeterminate(message);
			try {
				await action(dialog);
				taskCompletionSource.SetResult();
			} catch (Exception e) {
				taskCompletionSource.SetException(e);
			}
		});
		
		await dialog.ShowProgressDialog(owner);
		await taskCompletionSource.Task;
	}
	
	internal static async Task<T> ShowIndeterminate<T>(Window owner, string title, string message, Func<ProgressDialog, Task<T>> action) {
		var taskCompletionSource = new TaskCompletionSource<T>();
		var dialog = new ProgressDialog();
		
		dialog.DataContext = new ProgressDialogModel(title, async callbacks => {
			await callbacks[0].UpdateIndeterminate(message);
			try {
				taskCompletionSource.SetResult(await action(dialog));
			} catch (Exception e) {
				taskCompletionSource.SetException(e);
			}
		});
		
		await dialog.ShowProgressDialog(owner);
		return await taskCompletionSource.Task;
	}
	
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
		try {
			await progressTask;
		} catch (Exception e) {
			Log.Error(e);
			await Dialog.ShowOk(owner, "Unexpected Error", e.Message);
		}
	}
}
