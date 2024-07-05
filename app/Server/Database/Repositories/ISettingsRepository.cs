using System;
using System.Threading.Tasks;
using DHT.Server.Data.Settings;

namespace DHT.Server.Database.Repositories;

public interface ISettingsRepository {
	Task Set<T>(SettingsKey<T> key, T value);
	
	Task Set(Func<ISetter, Task> setter);
	
	Task<T?> Get<T>(SettingsKey<T> key, T? defaultValue);
	
	interface ISetter {
		Task Set<T>(SettingsKey<T> key, T value);
	}
	
	internal sealed class Dummy : ISettingsRepository {
		public Task Set<T>(SettingsKey<T> key, T value) {
			return Task.CompletedTask;
		}

		public Task Set(Func<ISetter, Task> setter) {
			return Task.CompletedTask;
		}

		public Task<T?> Get<T>(SettingsKey<T> key, T? defaultValue) {
			return Task.FromResult(defaultValue);
		}
	}
}
