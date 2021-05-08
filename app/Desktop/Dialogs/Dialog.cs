using System.Threading.Tasks;
using Avalonia.Controls;

namespace DHT.Desktop.Dialogs {
	public static class Dialog {
		public static async Task ShowOk(Window owner, string title, string message) {
			await new MessageDialog {
				DataContext = new MessageDialogModel {
					Title = title,
					Message = message,
					IsOkVisible = true
				}
			}.ShowDialog<DialogResult.All>(owner);
		}

		public static async Task<DialogResult.OkCancel> ShowOkCancel(Window owner, string title, string message) {
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
}
