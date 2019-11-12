﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Extensions.Logging;
using Uno;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Logging;
using Uno.UI.Xaml.Input;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.System;
using Uno.Collections;
using Uno.UI;
using System.Numerics;
using Windows.UI.Input;
using Uno.Foundation.Interop;
using Uno.UI.Xaml;

namespace Windows.UI.Xaml
{
	public partial class UIElement : DependencyObject
	{
		// Ref:
		// https://www.w3.org/TR/pointerevents/
		// https://developer.mozilla.org/en-US/docs/Web/API/PointerEvent

		//private static readonly Dictionary<RoutedEvent, (string domEventName, EventArgsParser argsParser, RoutedEventHandlerWithHandled handler)> _pointerHandlers
		//	= new Dictionary<RoutedEvent, (string, EventArgsParser, RoutedEventHandlerWithHandled)>
		//	{
		//		// Note: we use 'pointerenter' and 'pointerleave' which are not bubbling natively
		//		//		 as on UWP, even if the event are RoutedEvents, PointerEntered and PointerExited
		//		//		 are routed only in some particular cases (entering at once on multiple controls),
		//		//		 it's easier to handle this in managed code.
		//		{PointerEnteredEvent, ("pointerenter", PayloadToEnteredPointerArgs, (snd, args) => ((UIElement)snd).OnNativePointerEnter((PointerRoutedEventArgs)args))},
		//		{PointerExitedEvent, ("pointerleave", PayloadToExitedPointerArgs, (snd, args) => ((UIElement)snd).OnNativePointerExited((PointerRoutedEventArgs)args))},
		//		{PointerPressedEvent, ("pointerdown", PayloadToPressedPointerArgs, (snd, args) => ((UIElement)snd).OnNativePointerDown((PointerRoutedEventArgs)args))},
		//		{PointerReleasedEvent, ("pointerup", PayloadToReleasedPointerArgs, (snd, args) => ((UIElement)snd).OnNativePointerUp((PointerRoutedEventArgs)args))},

		//		{PointerMovedEvent, ("pointermove", PayloadToMovedPointerArgs, (snd, args) => ((UIElement)snd).OnNativePointerMove((PointerRoutedEventArgs)args))},
		//		{PointerCanceledEvent, ("pointercancel", PayloadToCancelledPointerArgs, (snd, args) => ((UIElement)snd).OnNativePointerCancel((PointerRoutedEventArgs)args, isSwallowedBySystem: true))}, //https://www.w3.org/TR/pointerevents/#the-pointercancel-event
		//	};

		partial void OnGestureRecognizerInitialized(GestureRecognizer recognizer)
		{
			// When a gesture recognizer is initialized, we subscribe to pointer events in order to feed it.
			// Note: We subscribe to * all * pointer events in order to maintain a logical internal state of pointers over / press / capture

			//foreach (var pointerEvent in _pointerHandlers.Keys)
			//{
			//	AddPointerHandlerCore(pointerEvent);
			//}

			EnabledNativePointerEvents();
		}

		partial void AddPointerHandler(RoutedEvent routedEvent, int handlersCount, object handler, bool handledEventsToo)
		{
			if (handlersCount != 1 || _registeredRoutedEvents.HasFlag(routedEvent.Flag))
			{
				return;
			}

			EnabledNativePointerEvents();
		}

		private void EnabledNativePointerEvents()
		{
			if (!_registeredRoutedEvents.HasFlag(RoutedEventFlag.PointerEntered))
			{
				InitEventArgs();

				try
				{
					unsafe
					{
						Console.WriteLine($"++++++++++++++++++++++++++++++++++++++++++++++ Created property should be of size typeof: {Marshal.SizeOf(typeof(WindowManagerPointerEventArgs_Return))} | generic: {Marshal.SizeOf<WindowManagerPointerEventArgs_Return>()} | sizeof: {sizeof(WindowManagerPointerEventArgs_Return)} bytes long.");
					}

					Console.WriteLine("******************************** ENABLE EVENTS");
					TSInteropMarshaller.InvokeJS("Uno:enablePointerEvents", new WindowManagerEnablePointerEventsParams {HtmlId = Handle, IsEnabled = true});
					Console.WriteLine("******************************** ENABLE EVENTS ------------------- OK :)");
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
				}

				// In order to ensure valid pressed and over state, we ** must ** subscribe to all pointer events at once.
				_registeredRoutedEvents |= RoutedEventFlagHelper.Pointers;
			}
		}

		private static void InitEventArgs()
		{
			if (_pointerEventArgs == null)
			{
				try
				{
					Console.WriteLine("******************************** INITIALIZING EVENT ARGS");
					(_pointerEventArgs, _pointerEventResult) = TSInteropMarshaller.AllocJSProperties<WindowManagerPointerEventArgs_Return, WindowManagerPointerEventResult_Params>("Uno:initPointerEventsProperties");

					Console.WriteLine("******************************** INITIALIZING EVENT ARGS ------------------- OK :)");
				}
				catch(Exception e)
				{
					Console.Error.WriteLine(e);
				}
			}
		}

		private static TSInteropMarshaller.Property<WindowManagerPointerEventArgs_Return> _pointerEventArgs;
		private static TSInteropMarshaller.Property<WindowManagerPointerEventResult_Params> _pointerEventResult;

		[Preserve]
		public static void DispatchPointerEvent()
		{
			try
			{
				if (_pointerEventArgs.TryRead(out var args))
				{
					Console.WriteLine("RECEIVED ARGS: " + args);

					var sender = GetElementFromHandle(args.SourceHandle);
					var routedArgs = new PointerRoutedEventArgs(sender, args);

					bool handled;
					switch ((NativePointerEvent)args.Event)
					{
						case NativePointerEvent.PointerEnter:
							handled = sender.OnNativePointerEnter(routedArgs);
							break;
						case NativePointerEvent.PointerLeave:
							handled = sender.OnNativePointerExited(routedArgs);
							break;
						case NativePointerEvent.PointerDown:
							handled = sender.OnNativePointerDown(routedArgs);
							break;
						case NativePointerEvent.PointerUp:
							handled = sender.OnNativePointerUp(routedArgs);
							break;
						case NativePointerEvent.PointerMove:
							handled = args.IsOver_HasValue
								? sender.OnNativePointerMoveWithOverCheck(routedArgs, args.IsOver)
								: sender.OnNativePointerMove(routedArgs);
							break;
						case NativePointerEvent.PointerCancel:
							handled = sender.OnNativePointerCancel(routedArgs, isSwallowedBySystem: true);
							break;
						default:
							throw new ArgumentOutOfRangeException($"The event {args.Event} is not supported.");
					}

					_pointerEventResult.Write(new WindowManagerPointerEventResult_Params {Handled = handled});

					Console.WriteLine("SUVVSCFULLY DISPATCHED: handled=" + handled);
				}
			}
			catch (Exception e)
			{
				Console.Error.WriteLine("FAILED TO DISPATCH POINTER: " + e);
			}
		}

		internal enum NativePointerEvent : int
		{
			PointerEnter = 0,
			PointerLeave = 1,
			PointerDown = 2,
			PointerUp = 3,
			PointerMove = 4,
			PointerCancel = 16,
		}

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerEnablePointerEventsParams
		{
			public IntPtr HtmlId;

			public bool IsEnabled;
		}

		//[TSInteropMessage]
		[StructLayout(LayoutKind.Explicit, Pack = 8, Size = 13 * 8)]
		internal struct WindowManagerPointerEventArgs_Return // This us suffixed "_Return" to get the "marshal" method ... we need to fix the generator!
		{
			[FieldOffset( 0 * 8)] public double Timestamp;
			[FieldOffset( 1 * 8)] public int Event; // NativePointerEvent
			[FieldOffset( 2 * 8)] public int SourceHandle;
			[FieldOffset( 3 * 8)] public int OriginalSourceHandle;
			[FieldOffset( 4 * 8)] public int PointerId;
			[FieldOffset( 5 * 8)] public int PointerType; // PointerDeviceType
			[FieldOffset( 6 * 8)] public double RawX;
			[FieldOffset( 7 * 8)] public double RawY;
			[FieldOffset( 8 * 8)] public bool IsCtrlPressed;
			[FieldOffset( 9 * 8)] public bool IsShiftPressed;
			[FieldOffset(10 * 8)] public int PressedButton;
			[FieldOffset(11 * 8)] public bool IsOver_HasValue;
			[FieldOffset(12 * 8)] public bool IsOver;

			/// <inheritdoc />
			public override string ToString()
				=> $"Event:{(NativePointerEvent)Event}"
					+ $"| SourceHandle : {SourceHandle}"
					+ $"| OriginalSourceHandle : {OriginalSourceHandle}"
					+ $"| Timestamp : {Timestamp}"
					+ $"| PointerId : {PointerId}"
					+ $"| PointerType: {(PointerDeviceType)PointerType}"
					+ $"| RawX : {RawX}"
					+ $"| RawY : {RawY}"
					+ $"| IsCtrlPressed : {IsCtrlPressed}"
					+ $"| IsShiftPressed : {IsShiftPressed}"
					+ $"| PressedButton : {PressedButton}"
					+ $"| IsOver_HasValue : {IsOver_HasValue}"
					+ $"| IsOver : {IsOver}";\
		}

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerPointerEventResult_Params
		{
			public bool Handled;
		}

		//private static PointerRoutedEventArgs PayloadToEnteredPointerArgs(object snd, string payload) => PayloadToPointerArgs(snd, payload, isInContact: false, canBubble: false);
		//private static PointerRoutedEventArgs PayloadToPressedPointerArgs(object snd, string payload) => PayloadToPointerArgs(snd, payload, isInContact: true, pressed: true);
		//private static PointerRoutedEventArgs PayloadToMovedPointerArgs(object snd, string payload) => PayloadToPointerArgs(snd, payload, isInContact: true);
		//private static PointerRoutedEventArgs PayloadToReleasedPointerArgs(object snd, string payload) => PayloadToPointerArgs(snd, payload, isInContact: true, pressed: false);
		//private static PointerRoutedEventArgs PayloadToExitedPointerArgs(object snd, string payload) => PayloadToPointerArgs(snd, payload, isInContact: false, canBubble: false);
		//private static PointerRoutedEventArgs PayloadToCancelledPointerArgs(object snd, string payload) => PayloadToPointerArgs(snd, payload, isInContact: false, pressed: false);

		//private static PointerRoutedEventArgs PayloadToPointerArgs(object snd, string payload, bool isInContact, bool canBubble = true, bool? pressed = null)
		//{
		//	var parts = payload?.Split(';');
		//	if (parts?.Length != 10)
		//	{
		//		return null;
		//	}

		//	var pointerId = uint.Parse(parts[0], CultureInfo.InvariantCulture);
		//	var x = double.Parse(parts[1], CultureInfo.InvariantCulture);
		//	var y = double.Parse(parts[2], CultureInfo.InvariantCulture);
		//	var ctrl = parts[3] == "1";
		//	var shift = parts[4] == "1";
		//	var button = int.Parse(parts[5], CultureInfo.InvariantCulture); // -1: none, 0:main, 1:middle, 2:other (commonly main=left, other=right)
		//	var typeStr = parts[6];
		//	var srcHandle = int.Parse(parts[7]);
		//	var timestamp = double.Parse(parts[8], CultureInfo.InvariantCulture);
		//	var isOver = parts[9] == "null" ? default(bool?) : bool.Parse(parts[9]);

		//	var position = new Point(x, y);
		//	var pointerType = ConvertPointerTypeString(typeStr);
		//	var key =
		//		button == 0 ? VirtualKey.LeftButton
		//		: button == 1 ? VirtualKey.MiddleButton
		//		: button == 2 ? VirtualKey.RightButton
		//		: VirtualKey.None; // includes -1 == none
		//	var keyModifiers = VirtualKeyModifiers.None;
		//	if (ctrl) keyModifiers |= VirtualKeyModifiers.Control;
		//	if (shift) keyModifiers |= VirtualKeyModifiers.Shift;
		//	var update = PointerUpdateKind.Other;
		//	if (pressed.HasValue)
		//	{
		//		if (pressed.Value)
		//		{
		//			update = key == VirtualKey.LeftButton ? PointerUpdateKind.LeftButtonPressed
		//				: key == VirtualKey.MiddleButton ? PointerUpdateKind.MiddleButtonPressed
		//				: key == VirtualKey.RightButton ? PointerUpdateKind.RightButtonPressed
		//				: PointerUpdateKind.Other;
		//		}
		//		else
		//		{
		//			update = key == VirtualKey.LeftButton ? PointerUpdateKind.LeftButtonReleased
		//				: key == VirtualKey.MiddleButton ? PointerUpdateKind.MiddleButtonReleased
		//				: key == VirtualKey.RightButton ? PointerUpdateKind.RightButtonReleased
		//				: PointerUpdateKind.Other;
		//		}
		//	}
		//	var src = GetElementFromHandle(srcHandle) ?? (UIElement)snd;

		//	return new PointerRoutedEventArgs(
		//		timestamp,
		//		pointerId,
		//		pointerType,
		//		position,
		//		isInContact,
		//		key,
		//		keyModifiers,
		//		update,
		//		src,
		//		canBubble);
		//}

		//private static PointerDeviceType ConvertPointerTypeString(string typeStr)
		//{
		//	PointerDeviceType type;
		//	switch (typeStr.ToUpper())
		//	{
		//		case "MOUSE":
		//		default:
		//			type = PointerDeviceType.Mouse;
		//			break;
		//		case "PEN":
		//			type = PointerDeviceType.Pen;
		//			break;
		//		case "TOUCH":
		//			type = PointerDeviceType.Touch;
		//			break;
		//	}

		//	return type;
		//}

		#region Capture
		partial void OnManipulationModeChanged(ManipulationModes _, ManipulationModes newMode)
			=> SetStyle("touch-action", newMode == ManipulationModes.None ? "none" : "auto");

		partial void CapturePointerNative(Pointer pointer)
		{
			var command = "Uno.UI.WindowManager.current.setPointerCapture(" + HtmlId + ", " + pointer.PointerId + ");";
			WebAssemblyRuntime.InvokeJS(command);

			if (pointer.PointerDeviceType != PointerDeviceType.Mouse)
			{
				SetStyle("touch-action", "none");
			}
		}

		partial void ReleasePointerNative(Pointer pointer)
		{
			var command = "Uno.UI.WindowManager.current.releasePointerCapture(" + HtmlId + ", " + pointer.PointerId + ");";
			WebAssemblyRuntime.InvokeJS(command);

			if (pointer.PointerDeviceType != PointerDeviceType.Mouse && ManipulationMode != ManipulationModes.None)
			{
				SetStyle("touch-action", "auto");
			}
		}
		#endregion

		#region HitTestVisibility
		internal void UpdateHitTest()
		{
			this.CoerceValue(HitTestVisibilityProperty);
		}

		private enum HitTestVisibility
		{
			/// <summary>
			/// The element and its children can't be targeted by hit-testing.
			/// </summary>
			/// <remarks>
			/// This occurs when IsHitTestVisible="False", IsEnabled="False", or Visibility="Collapsed".
			/// </remarks>
			Collapsed,

			/// <summary>
			/// The element can't be targeted by hit-testing.
			/// </summary>
			/// <remarks>
			/// This usually occurs if an element doesn't have a Background/Fill.
			/// </remarks>
			Invisible,

			/// <summary>
			/// The element can be targeted by hit-testing.
			/// </summary>
			Visible,
		}

		/// <summary>
		/// Represents the final calculated hit-test visibility of the element.
		/// </summary>
		/// <remarks>
		/// This property should never be directly set, and its value should always be calculated through coercion (see <see cref="CoerceHitTestVisibility(DependencyObject, object, bool)"/>.
		/// </remarks>
		private static readonly DependencyProperty HitTestVisibilityProperty =
			DependencyProperty.Register(
				"HitTestVisibility",
				typeof(HitTestVisibility),
				typeof(UIElement),
				new FrameworkPropertyMetadata(
					HitTestVisibility.Visible,
					FrameworkPropertyMetadataOptions.Inherits,
					coerceValueCallback: (s, e) => CoerceHitTestVisibility(s, e),
					propertyChangedCallback: (s, e) => OnHitTestVisibilityChanged(s, e)
				)
			);

		/// <summary>
		/// This calculates the final hit-test visibility of an element.
		/// </summary>
		/// <returns></returns>
		private static object CoerceHitTestVisibility(DependencyObject dependencyObject, object baseValue)
		{
			var element = (UIElement)dependencyObject;

			// The HitTestVisibilityProperty is never set directly. This means that baseValue is always the result of the parent's CoerceHitTestVisibility.
			var baseHitTestVisibility = (HitTestVisibility)baseValue;

			// If the parent is collapsed, we should be collapsed as well. This takes priority over everything else, even if we would be visible otherwise.
			if (baseHitTestVisibility == HitTestVisibility.Collapsed)
			{
				return HitTestVisibility.Collapsed;
			}

			// If we're not locally hit-test visible, visible, or enabled, we should be collapsed. Our children will be collapsed as well.
			if (!element.IsHitTestVisible || element.Visibility != Visibility.Visible || !element.IsEnabledOverride())
			{
				return HitTestVisibility.Collapsed;
			}

			// If we're not hit (usually means we don't have a Background/Fill), we're invisible. Our children will be visible or not, depending on their state.
			if (!element.IsViewHit())
			{
				return HitTestVisibility.Invisible;
			}

			// If we're not collapsed or invisible, we can be targeted by hit-testing. This means that we can be the source of pointer events.
			return HitTestVisibility.Visible;
		}

		private static void OnHitTestVisibilityChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			if (dependencyObject is UIElement element && args.NewValue is HitTestVisibility hitTestVisibility)
			{
				if (hitTestVisibility == HitTestVisibility.Visible)
				{
					// By default, elements have 'pointer-event' set to 'auto' (see Uno.UI.css .uno-uielement class).
					// This means that they can be the target of hit-testing and will raise pointer events when interacted with.
					// This is aligned with HitTestVisibilityProperty's default value of Visible.
					element.SetStyle("pointer-events", "auto");
				}
				else
				{
					// If HitTestVisibilityProperty is calculated to Invisible or Collapsed,
					// we don't want to be the target of hit-testing and raise any pointer events.
					// This is done by setting 'pointer-events' to 'none'.
					element.SetStyle("pointer-events", "none");
				}
			}
		}
		#endregion
	}
}
