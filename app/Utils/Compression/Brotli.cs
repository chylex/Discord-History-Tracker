using System;
using System.Buffers;
using System.IO;
using System.IO.Compression;

namespace DHT.Utils.Compression {
	public sealed class Brotli {
		private readonly byte[] tempBuffer;

		public Brotli(int bufferSize) {
			tempBuffer = new byte[bufferSize];
		}

		public byte[] Compress(byte[] input) {
			var inputBuffer = new ReadOnlySpan<byte>(input);
			var outputBuffer = new Span<byte>(tempBuffer);

			using var outputStream = new MemoryStream();
			using var brotliEncoder = new BrotliEncoder(10, 11);

			var result = OperationStatus.DestinationTooSmall;

			while (result == OperationStatus.DestinationTooSmall) {
				result = brotliEncoder.Compress(inputBuffer, outputBuffer, out int bytesConsumed, out int bytesWritten, isFinalBlock: false);

				if (result == OperationStatus.InvalidData) {
					throw new InvalidDataException();
				}

				Write(bytesWritten, outputBuffer, outputStream);

				if (bytesConsumed > 0) {
					inputBuffer = inputBuffer[bytesConsumed..];
				}
			}

			result = OperationStatus.DestinationTooSmall;

			while (result == OperationStatus.DestinationTooSmall) {
				result = brotliEncoder.Flush(outputBuffer, out var bytesWritten);

				if (result == OperationStatus.InvalidData) {
					throw new InvalidDataException();
				}

				Write(bytesWritten, outputBuffer, outputStream);
			}

			return outputStream.ToArray();
		}

		private static void Write(int bytes, Span<byte> buffer, MemoryStream outputStream) {
			if (bytes > 0) {
				outputStream.Write(buffer[..bytes]);
			}
		}
	}
}
