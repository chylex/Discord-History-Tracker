using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace DHT.Desktop.Common;

sealed class NumberValueConverter : IValueConverter {
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
		return value == null ? "-" : string.Format(Program.Culture, "{0:n0}", value);
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
		throw new NotSupportedException();
	}
}
