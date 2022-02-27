using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DHT.Desktop.Main.Controls {
	[SuppressMessage("ReSharper", "MemberCanBeInternal")]
	public sealed class FilterPanel : UserControl {
		private CalendarDatePicker StartDatePicker => this.FindControl<CalendarDatePicker>("StartDatePicker");
		private CalendarDatePicker EndDatePicker => this.FindControl<CalendarDatePicker>("EndDatePicker");

		public FilterPanel() {
			InitializeComponent();
		}

		private void InitializeComponent() {
			AvaloniaXamlLoader.Load(this);

			var culture = Program.Culture;
			foreach (var picker in new CalendarDatePicker[] { StartDatePicker, EndDatePicker }) {
				picker.FirstDayOfWeek = culture.DateTimeFormat.FirstDayOfWeek;
				picker.SelectedDateFormat = CalendarDatePickerFormat.Custom;
				picker.CustomDateFormatString = culture.DateTimeFormat.ShortDatePattern;
				picker.Watermark = culture.DateTimeFormat.ShortDatePattern;
			}
		}

		public void CalendarDatePicker_OnSelectedDateChanged(object? sender, SelectionChangedEventArgs e) {
			if (DataContext is FilterPanelModel model) {
				model.StartDate = StartDatePicker.SelectedDate;
				model.EndDate = EndDatePicker.SelectedDate;
			}
		}
	}
}
