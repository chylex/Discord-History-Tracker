using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace DHT.Desktop.Dialogs.File;

static class FileDialogs {
	public static async Task<string[]> OpenFiles(this IStorageProvider storageProvider, FilePickerOpenOptions options) {
		return (await storageProvider.OpenFilePickerAsync(options)).ToLocalPaths();
	}

	public static async Task<string?> SaveFile(this IStorageProvider storageProvider, FilePickerSaveOptions options) {
		return (await storageProvider.SaveFilePickerAsync(options))?.ToLocalPath();
	}

	public static FilePickerFileType CreateFilter(string name, string[] extensions) {
		return new FilePickerFileType(name) {
			Patterns = extensions.Select(static ext => "*." + ext).ToArray()
		};
	}

	public static Task<IStorageFolder?> GetSuggestedStartLocation(Window window, string? suggestedDirectory) {
		return suggestedDirectory == null ? Task.FromResult<IStorageFolder?>(null) : window.StorageProvider.TryGetFolderFromPathAsync(suggestedDirectory);
	}

	private static string ToLocalPath(this IStorageFile file) {
		return file.TryGetLocalPath() ?? throw new NotSupportedException("Local filesystem is not supported.");
	}

	private static string[] ToLocalPaths(this IReadOnlyList<IStorageFile> files) {
		return files.Select(ToLocalPath).ToArray();
	}
}
