using System;
using System.Threading.Tasks;

namespace DHT.Server.Database.Sqlite.Utils;

public interface ISchemaUpgradeCallbacks {
	Task<bool> CanUpgrade();
	Task Start(int versionSteps, Func<IProgressReporter, Task> doUpgrade);

	public interface IProgressReporter {
		Task NextVersion();
		Task MainWork(string message, int finishedItems, int totalItems);
		Task SubWork(string message, int finishedItems, int totalItems);
	}
}
