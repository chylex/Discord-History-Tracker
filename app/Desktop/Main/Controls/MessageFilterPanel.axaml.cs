using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;

namespace DHT.Desktop.Main.Controls;

[SuppressMessage("ReSharper", "MemberCanBeInternal")]
public sealed partial class MessageFilterPanel : UserControl {
	public MessageFilterPanel() {
		InitializeComponent();

		var culture = Program.Culture;
		foreach (var picker in new CalendarDatePicker[] { StartDatePicker, EndDatePicker }) {
			picker.FirstDayOfWeek = culture.DateTimeFormat.FirstDayOfWeek;
			picker.SelectedDateFormat = CalendarDatePickerFormat.Custom;
			picker.CustomDateFormatString = culture.DateTimeFormat.ShortDatePattern;
			picker.Watermark = culture.DateTimeFormat.ShortDatePattern;
		}
	}

	public void CalendarDatePicker_OnSelectedDateChanged(object? sender, SelectionChangedEventArgs e) {
		if (DataContext is MessageFilterPanelModel model) {
			model.StartDate = StartDatePicker.SelectedDate;
			model.EndDate = EndDatePicker.SelectedDate;
		}
	}
}
