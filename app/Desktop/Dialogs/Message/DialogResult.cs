using System;

namespace DHT.Desktop.Dialogs.Message;

static class DialogResult {
	public enum All {
		Ok,
		Yes,
		No,
		Cancel
	}

	public enum OkCancel {
		Closed,
		Ok,
		Cancel
	}

	public enum YesNo {
		Closed,
		Yes,
		No
	}

	public enum YesNoCancel {
		Closed,
		Yes,
		No,
		Cancel
	}

	public static OkCancel ToOkCancel(this All? result) {
		return result switch {
			null       => OkCancel.Closed,
			All.Ok     => OkCancel.Ok,
			All.Cancel => OkCancel.Cancel,
			_          => throw new ArgumentException("Cannot convert dialog result " + result + " to ok/cancel.")
		};
	}

	public static YesNo ToYesNo(this All? result) {
		return result switch {
			null    => YesNo.Closed,
			All.Yes => YesNo.Yes,
			All.No  => YesNo.No,
			_       => throw new ArgumentException("Cannot convert dialog result " + result + " to yes/no.")
		};
	}

	public static YesNoCancel ToYesNoCancel(this All? result) {
		return result switch {
			null       => YesNoCancel.Closed,
			All.Yes    => YesNoCancel.Yes,
			All.No     => YesNoCancel.No,
			All.Cancel => YesNoCancel.Cancel,
			_          => throw new ArgumentException("Cannot convert dialog result " + result + " to yes/no/cancel.")
		};
	}
}
