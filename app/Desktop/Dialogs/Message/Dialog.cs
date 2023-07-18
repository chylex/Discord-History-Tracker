using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;

namespace DHT.Desktop.Dialogs.Message;

static class Dialog {
	public static async Task ShowOk(Window owner, string title, string message) {
		if (!Dispatcher.UIThread.CheckAccess()) {
			await Dispatcher.UIThread.InvokeAsync(() => ShowOk(owner, title, message));
			return;
		}

		await new MessageDialog {
			DataContext = new MessageDialogModel {
				Title = title,
				Message = message,
				IsOkVisible = true
			}
		}.ShowDialog<DialogResult.All>(owner);
	}

	public static async Task<DialogResult.OkCancel> ShowOkCancel(Window owner, string title, string message) {
		if (!Dispatcher.UIThread.CheckAccess()) {
			return await Dispatcher.UIThread.InvokeAsync(() => ShowOkCancel(owner, title, message));
		}

		var result = await new MessageDialog {
			DataContext = new MessageDialogModel {
				Title = title,
				Message = message,
				IsOkVisible = true,
				IsCancelVisible = true
			}
		}.ShowDialog<DialogResult.All?>(owner);

		return result.ToOkCancel();
	}

	public static async Task<DialogResult.YesNo> ShowYesNo(Window owner, string title, string message) {
		if (!Dispatcher.UIThread.CheckAccess()) {
			return await Dispatcher.UIThread.InvokeAsync(() => ShowYesNo(owner, title, message));
		}

		var result = await new MessageDialog {
			DataContext = new MessageDialogModel {
				Title = title,
				Message = message,
				IsYesVisible = true,
				IsNoVisible = true
			}
		}.ShowDialog<DialogResult.All?>(owner);

		return result.ToYesNo();
	}

	public static async Task<DialogResult.YesNoCancel> ShowYesNoCancel(Window owner, string title, string message) {
		if (!Dispatcher.UIThread.CheckAccess()) {
			return await Dispatcher.UIThread.InvokeAsync(() => ShowYesNoCancel(owner, title, message));
		}

		var result = await new MessageDialog {
			DataContext = new MessageDialogModel {
				Title = title,
				Message = message,
				IsYesVisible = true,
				IsNoVisible = true,
				IsCancelVisible = true
			}
		}.ShowDialog<DialogResult.All?>(owner);

		return result.ToYesNoCancel();
	}
}
