using System;
using System.Threading.Tasks;

namespace DHT.Utils.Tasks; 

public static class TaskExtensions {
	public static async Task WaitIgnoringCancellation(this Task task) {
		try {
			await task;
		} catch (OperationCanceledException) {}
	}
}
