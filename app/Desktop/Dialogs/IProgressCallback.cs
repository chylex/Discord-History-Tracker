using System.Threading.Tasks;

namespace DHT.Desktop.Dialogs {
	public interface IProgressCallback {
		Task Update(string message, int finishedItems, int totalItems);
	}
}
