using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace DHT.Desktop.Common;

sealed class BytesValueConverter : IValueConverter {
	private sealed class Unit {
		private readonly string label;
		private readonly string numberFormat;

		public Unit(string label, int decimalPlaces) {
			this.label = label;
			this.numberFormat = "{0:n" + decimalPlaces + "}";
		}

		public string Format(double size) {
			return string.Format(Program.Culture, numberFormat, size) + " " + label;
		}
	}

	private static readonly Unit[] Units = {
		new ("B", decimalPlaces: 0),
		new ("kB", decimalPlaces: 0),
		new ("MB", decimalPlaces: 1),
		new ("GB", decimalPlaces: 1),
		new ("TB", decimalPlaces: 1)
	};

	private const int Scale = 1000;

	private static string Convert(ulong size) {
		int power = size == 0L ? 0 : (int) Math.Log(size, Scale);
		int unit = power >= Units.Length ? Units.Length - 1 : power;
		return Units[unit].Format(unit == 0 ? size : size / Math.Pow(Scale, unit));
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
