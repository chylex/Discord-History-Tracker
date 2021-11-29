using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DHT.Desktop.Main.Controls {
	public class FilterPanel : UserControl {
		public FilterPanel() {
			InitializeComponent();
		}

		private void InitializeComponent() {
			AvaloniaXamlLoader.Load(this);
		}

		public void CalendarDatePicker_OnSelectedDateChanged(object? sender, SelectionChangedEventArgs e) {
			if (DataContext is FilterPanelModel model) {
				model.StartDate = this.FindControl<CalendarDatePicker>("StartDatePicker").SelectedDate;
				model.EndDate = this.FindControl<CalendarDatePicker>("EndDatePicker").SelectedDate;
			}
		}
	}
}
