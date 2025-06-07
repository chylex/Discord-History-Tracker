using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;

namespace DHT.Desktop.Dialogs.Progress;

static class DelayedProgressDialog {
	public static async ValueTask Await(Func<Task> taskProvider, TimeSpan delay, Window window, string progressDialogTitle, string progressDialogDescription) {
		Dispatcher.UIThread.VerifyAccess();
		
		Task task = Task.Run(taskProvider);
		if (task.IsCompleted) {
			return;
		}
		
		// Freeze the UI thread for a short while in case the task finishes quickly.
		_ = Task.WhenAny(Task.Delay(delay), task).GetAwaiter().GetResult();
		
		if (!task.IsCompleted) {
			await ProgressDialog.ShowIndeterminate(window, progressDialogTitle, progressDialogDescription, _ => task);
		}
	}
}
