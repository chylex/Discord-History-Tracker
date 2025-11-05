using System;
using System.Threading.Tasks;
using DHT.Server.Database.Sqlite.Utils;

namespace DHT.Server.Database.Sqlite.Schema;

public interface ISchemaUpgradeCallbacks {
	Task<InitialDatabaseSettings?> GetInitialDatabaseSettings();
	Task<bool> CanUpgrade();
	Task Start(int versionSteps, Func<IProgressReporter, Task> doUpgrade);
	
	public interface IProgressReporter {
		Task NextVersion();
		Task MainWork(string message, int finishedItems, int totalItems);
		Task SubWork(string message, int finishedItems, int totalItems);
	}
}
