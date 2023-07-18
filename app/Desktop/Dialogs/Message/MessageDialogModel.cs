namespace DHT.Desktop.Dialogs.Message;

sealed class MessageDialogModel {
	public string Title { get; init; } = "";
	public string Message { get; init; } = "";

	public bool IsOkVisible { get; init; } = false;
	public bool IsYesVisible { get; init; } = false;
	public bool IsNoVisible { get; init; } = false;
	public bool IsCancelVisible { get; init; } = false;
}
