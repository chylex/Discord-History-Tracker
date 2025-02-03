using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using DHT.Desktop.Common;

namespace DHT.Desktop.Dialogs.Progress;

sealed class ProgressDialogModel {
	public string Title { get; init; } = "";
	
	public IReadOnlyList<ProgressItem> Items { get; } = [];
	
	private readonly TaskRunner? task;
	
	[Obsolete("Designer")]
	public ProgressDialogModel() {}
	
	public ProgressDialogModel(string title, TaskRunner task, int progressItems = 1) {
		this.Title = title;
		this.task = task;
		this.Items = Enumerable.Range(start: 0, progressItems).Select(static _ => new ProgressItem()).ToArray();
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
				item.IsIndeterminate = false;
			});
		}
		
		public async Task UpdateIndeterminate(string message) {
			await Dispatcher.UIThread.InvokeAsync(() => {
				item.Message = message;
				item.Items = string.Empty;
				item.Progress = 0;
				item.IsIndeterminate = true;
			});
		}
		
		public Task Hide() {
			return Update(string.Empty, finishedItems: 0, totalItems: 0);
		}
	}
}
