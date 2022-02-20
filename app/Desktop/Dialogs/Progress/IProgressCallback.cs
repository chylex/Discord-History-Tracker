using System.Threading.Tasks;

namespace DHT.Desktop.Dialogs.Progress {
	public interface IProgressCallback {
		Task Update(string message, int finishedItems, int totalItems);
	}
}
