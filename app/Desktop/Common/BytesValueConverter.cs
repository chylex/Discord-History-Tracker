using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace DHT.Desktop.Common {
	sealed class BytesValueConverter : IValueConverter {
		private static readonly string[] Units = {
			"B",
			"kB",
			"MB",
			"GB",
			"TB"
		};

		private const int Scale = 1000;

		private static string Convert(ulong size) {
			int power = size == 0L ? 0 : (int) Math.Log(size, Scale);
			int unit = power >= Units.Length ? Units.Length - 1 : power;
			if (unit == 0) {
				return string.Format(Program.Culture, "{0:n0}", size) + " " + Units[unit];
			}
			else {
				double humanReadableSize = size / Math.Pow(Scale, unit);
				return string.Format(Program.Culture, "{0:n0}", humanReadableSize) + " " + Units[unit];
			}
		}

		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
			if (value is long size and >= 0L) {
				return Convert((ulong) size);
			}
			else if (value is ulong usize) {
				return Convert(usize);
			}
			else {
				return "-";
			}
		}

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
			throw new NotSupportedException();
		}
	}
}
