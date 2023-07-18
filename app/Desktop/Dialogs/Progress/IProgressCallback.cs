using System.Threading.Tasks;

namespace DHT.Desktop.Dialogs.Progress;

interface IProgressCallback {
	Task Update(string message, int finishedItems, int totalItems);
}
