using System.Threading.Tasks;

namespace DHT.Desktop.Dialogs.Progress;

interface IProgressCallback {
	Task Update(string message, long finishedItems, long totalItems);
	Task UpdateIndeterminate(string message);
	Task Hide();
}
