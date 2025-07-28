using System;
using System.Collections;
using System.Reflection;
using Avalonia.Interactivity;

namespace DHT.Desktop.Common;

static class AvaloniaReflection {
	private static FieldInfo InteractiveEventHandlersField { get; } = typeof(Interactive).GetField("_eventHandlers", BindingFlags.Instance | BindingFlags.NonPublic)!;
	
	public static void Check() {
		if (InteractiveEventHandlersField == null) {
			throw new InvalidOperationException("Missing field: " + nameof(InteractiveEventHandlersField));
		}
		
		if (InteractiveEventHandlersField.FieldType.ToString() != "System.Collections.Generic.Dictionary`2[Avalonia.Interactivity.RoutedEvent,System.Collections.Generic.List`1[Avalonia.Interactivity.Interactive+EventSubscription]]") {
			throw new InvalidOperationException("Invalid field type: " + nameof(InteractiveEventHandlersField) + " = " + InteractiveEventHandlersField.FieldType);
		}
	}
	
	public static IList? GetEventHandler(Interactive target, RoutedEvent routedEvent) {
		IDictionary? eventHandlers = (IDictionary?) InteractiveEventHandlersField.GetValue(target);
		return (IList?) eventHandlers?[routedEvent];
	}
}
