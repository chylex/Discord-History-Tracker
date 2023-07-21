using System.Threading.Tasks;

namespace DHT.Desktop.App.Dialogs.Progress;

interface IProgressCallback {
	Task Update(string message, int finishedItems, int totalItems);
}
