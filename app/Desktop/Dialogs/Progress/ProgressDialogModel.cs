using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using DHT.Desktop.Common;
using DHT.Utils.Models;

namespace DHT.Desktop.Dialogs.Progress;

sealed class ProgressDialogModel : BaseModel {
	public string Title { get; init; } = "";

	private string message = "";

	public string Message {
		get => message;
		private set => Change(ref message, value);
	}

	private string items = "";

	public string Items {
		get => items;
		private set => Change(ref items, value);
	}

	private int progress = 0;

	public int Progress {
		get => progress;
		private set => Change(ref progress, value);
	}

	private readonly TaskRunner? task;

	[Obsolete("Designer")]
	public ProgressDialogModel() {}

	public ProgressDialogModel(TaskRunner task) {
		this.task = task;
	}

	internal async Task StartTask() {
		if (task != null) {
			await task(new Callback(this));
		}
	}

	public delegate Task TaskRunner(IProgressCallback callback);

	private sealed class Callback : IProgressCallback {
		private readonly ProgressDialogModel model;

		public Callback(ProgressDialogModel model) {
			this.model = model;
		}

		async Task IProgressCallback.Update(string message, int finishedItems, int totalItems) {
			await Dispatcher.UIThread.InvokeAsync(() => {
				model.Message = message;
				model.Items = finishedItems.Format() + " / " + totalItems.Format();
				model.Progress = 100 * finishedItems / totalItems;
			});
		}
	}
}
