// https://gist.github.com/MattiasBuelens/496fc1d37adb50a733edd43853f2f60e/088f061ab79b296f29225467ae9ba86ff990195d

ReadableStream.prototype.values ??= function({ preventCancel = false } = {}) {
	const reader = this.getReader();
	return {
		async next() {
			try {
				const result = await reader.read();
				if (result.done) {
					reader.releaseLock();
				}
				return result;
			} catch (e) {
				reader.releaseLock();
				throw e;
			}
		},
		async return(value) {
			if (!preventCancel) {
				const cancelPromise = reader.cancel(value);
				reader.releaseLock();
				await cancelPromise;
			}
			else {
				reader.releaseLock();
			}
			return { done: true, value };
		},
		[Symbol.asyncIterator]() {
			return this;
		}
	};
};

ReadableStream.prototype[Symbol.asyncIterator] ??= ReadableStream.prototype.values;
