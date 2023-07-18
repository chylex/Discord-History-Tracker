namespace DHT.Desktop.Common;

static class TextFormat {
	public static string Format(this int number) {
		return number.ToString("N0", Program.Culture);
	}

	public static string Format(this long number) {
		return number.ToString("N0", Program.Culture);
	}

	public static string Pluralize(this int number, string singular) {
		return number.Format() + "\u00A0" + (number == 1 ? singular : singular + "s");
	}

	public static string Pluralize(this long number, string singular) {
		return number.Format() + "\u00A0" + (number == 1 ? singular : singular + "s");
	}
}
