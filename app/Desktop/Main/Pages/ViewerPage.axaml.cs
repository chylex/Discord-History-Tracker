using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DHT.Desktop.Main.Pages {
	public class ViewerPage : UserControl {
		public ViewerPage() {
			InitializeComponent();
		}

		private void InitializeComponent() {
			AvaloniaXamlLoader.Load(this);
		}

		public void CalendarDatePicker_OnSelectedDateChanged(object? sender, SelectionChangedEventArgs e) {
			if (DataContext is ViewerPageModel model) {
				model.StartDate = this.FindControl<CalendarDatePicker>("StartDatePicker").SelectedDate;
				model.EndDate = this.FindControl<CalendarDatePicker>("EndDatePicker").SelectedDate;
			}
		}
	}
}
