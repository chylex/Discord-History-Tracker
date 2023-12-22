using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using DHT.Desktop.Common;
using DHT.Utils.Models;

namespace DHT.Desktop.Dialogs.Progress;

sealed class ProgressDialogModel : BaseModel {
	public string Title { get; init; } = "";

	public IReadOnlyList<ProgressItem> Items { get; } = Array.Empty<ProgressItem>();

	private readonly TaskRunner? task;

	[Obsolete("Designer")]
	public ProgressDialogModel() {}

	public ProgressDialogModel(TaskRunner task, int progressItems = 1) {
		this.Items = Enumerable.Range(0, progressItems).Select(static _ => new ProgressItem()).ToArray();
		this.task = task;
	}

	internal async Task StartTask() {
		if (task != null) {
			await task(Items.Select(static item => new Callback(item)).ToArray());
		}
	}

	public delegate Task TaskRunner(IReadOnlyList<IProgressCallback> callbacks);

	private sealed class Callback : IProgressCallback {
		private readonly ProgressItem item;

		public Callback(ProgressItem item) {
			this.item = item;
		}

		public async Task Update(string message, int finishedItems, int totalItems) {
			await Dispatcher.UIThread.InvokeAsync(() => {
				item.Message = message;
				item.Items = totalItems == 0 ? string.Empty : finishedItems.Format() + " / " + totalItems.Format();
				item.Progress = totalItems == 0 ? 0 : 100 * finishedItems / totalItems;
			});
		}

		public Task Hide() {
			return Update(string.Empty, 0, 0);
		}
	}
}
