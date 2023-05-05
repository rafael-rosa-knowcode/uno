﻿#if UNO_HAS_MANAGED_POINTERS
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Input;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using static Microsoft.UI.Xaml.UIElement;

namespace Uno.UI.Xaml.Core;

internal partial class InputManager
{
	internal void RaisePointerEntered(PointerEventArgs args)
		=> _pointerManager.OnPointerEntered(args);

	internal void RaisePointerExited(PointerEventArgs args)
		=> _pointerManager.OnPointerExited(args);

	internal void RaisePointerMoved(PointerEventArgs args)
		=> _pointerManager.OnPointerMoved(args);

	internal void RaisePointerPressed(PointerEventArgs args)
		=> _pointerManager.OnPointerPressed(args);

	internal void RaisePointerReleased(PointerEventArgs args)
		=> _pointerManager.OnPointerReleased(args);

	internal void RaisePointerWheelChanged(PointerEventArgs args)
		=> _pointerManager.OnPointerWheelChanged(args);

	internal void RaisePointerCancelled(PointerEventArgs args)
		=> _pointerManager.OnPointerCancelled(args);

	internal void SetPointerCapture(PointerIdentifier identifier)
		=> _pointerManager.SetPointerCapture(identifier);

	internal void ReleasePointerCapture(PointerIdentifier identifier)
		=> _pointerManager.ReleasePointerCapture(identifier);

	private PointerManager _pointerManager = null!;

	partial void InitializeManagedPointers()
	{
		_pointerManager = new PointerManager(this);
	}

	private class PointerManager
	{
		private static IPointerExtension? _pointerExtension;

		// TODO: Use pointer ID for the predicates
		private static readonly StalePredicate _isOver = new(e => e.IsPointerOver, "IsPointerOver");

		private readonly Dictionary<Pointer, UIElement> _pressedElements = new();

		private readonly InputManager _inputManager;

		public PointerManager(InputManager inputManager)
		{
			if (_pointerExtension is null)
			{
				ApiExtensibility.CreateInstance(typeof(PointerManager), out _pointerExtension); // TODO: Add IPointerExtension implementation to all Skia targets and create instance per XamlRoot https://github.com/unoplatform/uno/issues/8978
			}
			_inputManager = inputManager;

			if (_inputManager._contentRoot.Type == ContentRootType.CoreWindow)
			{
				Microsoft.UI.Xaml.Window.Current.CoreWindow.PointerMoved += (c, e) => OnPointerMoved(e);
				Microsoft.UI.Xaml.Window.Current.CoreWindow.PointerEntered += (c, e) => OnPointerEntered(e);
				Microsoft.UI.Xaml.Window.Current.CoreWindow.PointerExited += (c, e) => OnPointerExited(e);
				Microsoft.UI.Xaml.Window.Current.CoreWindow.PointerPressed += (c, e) => OnPointerPressed(e);
				Microsoft.UI.Xaml.Window.Current.CoreWindow.PointerReleased += (c, e) => OnPointerReleased(e);
				Microsoft.UI.Xaml.Window.Current.CoreWindow.PointerWheelChanged += (c, e) => OnPointerWheelChanged(e);
				Microsoft.UI.Xaml.Window.Current.CoreWindow.PointerCancelled += (c, e) => OnPointerCancelled(e);
			}
		}

		internal void OnPointerWheelChanged(Windows.UI.Core.PointerEventArgs args)
		{
			var (originalSource, _) = HitTest(args);

			// Even if impossible for the Release, we are fallbacking on the RootElement for safety
			// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
			// Note that if another app covers your app, then the OriginalSource on UWP is still the element of your app at the pointer's location.
			originalSource ??= Microsoft.UI.Xaml.Window.Current.Content;

			if (originalSource is null)
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"CoreWindow_PointerWheel ({args.CurrentPoint.Position}) **undispatched**");
				}

				return;
			}

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"CoreWindow_PointerWheelChanged [{originalSource.GetDebugName()}]");
			}

			var routedArgs = new PointerRoutedEventArgs(args, originalSource);

			// Second raise the event, either on the OriginalSource or on the capture owners if any
			RaiseUsingCaptures(Wheel, originalSource, routedArgs);
		}

		internal void OnPointerEntered(Windows.UI.Core.PointerEventArgs args)
		{
			var (originalSource, _) = HitTest(args);

			if (originalSource is ImplicitTextBlock)
			{
				global::System.Diagnostics.Debug.WriteLine("Entered");
			}
			// Even if impossible for the Enter, we are fallbacking on the RootElement for safety
			// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
			// Note that if another app covers your app, then the OriginalSource on UWP is still the element of your app at the pointer's location.
			originalSource ??= Microsoft.UI.Xaml.Window.Current.Content;

			if (originalSource is null)
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"CoreWindow_PointerEntered ({args.CurrentPoint.Position}) **undispatched**");
				}

				return;
			}

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"CoreWindow_PointerEntered [{originalSource.GetDebugName()}]");
			}

			var routedArgs = new PointerRoutedEventArgs(args, originalSource);

			Raise(Enter, originalSource, routedArgs);
		}

		internal void OnPointerExited(Windows.UI.Core.PointerEventArgs args)
		{
			// This is how UWP behaves: when out of the bounds of the Window, the root element is used.
			var originalSource = Microsoft.UI.Xaml.Window.Current.Content;
			if (originalSource is null)
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"CoreWindow_PointerExited ({args.CurrentPoint.Position}) Called before window content set.");
				}

				return;
			}

			var overBranchLeaf = VisualTreeHelper.SearchDownForLeaf(originalSource, _isOver);
			if (overBranchLeaf is null)
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"CoreWindow_PointerExited ({args.CurrentPoint.Position}) **undispatched**");
				}

				return;
			}

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"CoreWindow_PointerExited [{overBranchLeaf.GetDebugName()}]");
			}

			var routedArgs = new PointerRoutedEventArgs(args, originalSource);

			Raise(Leave, overBranchLeaf, routedArgs);
			if (!args.CurrentPoint.IsInContact && (PointerDeviceType)args.CurrentPoint.Pointer.Type == PointerDeviceType.Touch)
			{
				// We release the captures on exit when pointer if not pressed
				// Note: for a "Tap" with a finger the sequence is Up / Exited / Lost, so the lost cannot be raised on Up
				ReleaseCaptures(routedArgs);
			}
		}

		internal void OnPointerPressed(Windows.UI.Core.PointerEventArgs args)
		{
			var (originalSource, _) = HitTest(args);

			// Even if impossible for the Pressed, we are fallbacking on the RootElement for safety
			// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
			// Note that if another app covers your app, then the OriginalSource on UWP is still the element of your app at the pointer's location.
			originalSource ??= Microsoft.UI.Xaml.Window.Current.Content;

			if (originalSource is null)
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"CoreWindow_PointerPressed ({args.CurrentPoint.Position}) **undispatched**");
				}

				return;
			}

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"CoreWindow_PointerPressed [{originalSource.GetDebugName()}]");
			}

			var routedArgs = new PointerRoutedEventArgs(args, originalSource);

			_pressedElements[routedArgs.Pointer] = originalSource;
			Raise(Pressed, originalSource, routedArgs);
		}

		internal void OnPointerReleased(Windows.UI.Core.PointerEventArgs args)
		{
			var (originalSource, _) = HitTest(args);

			var isOutOfWindow = originalSource is null;

			// Even if impossible for the Release, we are fallbacking on the RootElement for safety
			// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
			// Note that if another app covers your app, then the OriginalSource on UWP is still the element of your app at the pointer's location.
			originalSource ??= Microsoft.UI.Xaml.Window.Current.Content;

			if (originalSource is null)
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"CoreWindow_PointerReleased ({args.CurrentPoint.Position}) **undispatched**");
				}

				return;
			}

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"CoreWindow_PointerReleased [{originalSource.GetDebugName()}]");
			}

			var routedArgs = new PointerRoutedEventArgs(args, originalSource);

			RaiseUsingCaptures(Released, originalSource, routedArgs);
			if (isOutOfWindow || (PointerDeviceType)args.CurrentPoint.Pointer.Type != PointerDeviceType.Touch)
			{
				// We release the captures on up but only after the released event and processed the gesture
				// Note: For a "Tap" with a finger the sequence is Up / Exited / Lost, so we let the Exit raise the capture lost
				ReleaseCaptures(routedArgs);
			}
			ClearPressedState(routedArgs);
		}

		internal void OnPointerMoved(Windows.UI.Core.PointerEventArgs args)
		{
			var (originalSource, staleBranch) = HitTest(args, _isOver);

			// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
			// Note that if another app covers your app, then the OriginalSource on UWP is still the element of your app at the pointer's location.
			originalSource ??= Microsoft.UI.Xaml.Window.Current.Content;

			if (originalSource is null)
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"CoreWindow_PointerMoved ({args.CurrentPoint.Position}) **undispatched**");
				}

				return;
			}

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"CoreWindow_PointerMoved [{originalSource.GetDebugName()}]");
			}

			var routedArgs = new PointerRoutedEventArgs(args, originalSource);

			// First raise the PointerExited events on the stale branch
			if (staleBranch.HasValue)
			{
				if (Raise(Leave, staleBranch.Value, routedArgs) is { VisualTreeAltered: true })
				{
					// The visual tree has been modified in a way that requires performing a new hit test.
					originalSource = HitTest(args).element ?? Microsoft.UI.Xaml.Window.Current.Content;
				}
			}

			// Second (try to) raise the PointerEnter on the OriginalSource
			// Note: This won't do anything if already over.
			if (Raise(Enter, originalSource, routedArgs) is { VisualTreeAltered: true })
			{
				// The visual tree has been modified in a way that requires performing a new hit test.
				originalSource = HitTest(args).element ?? Microsoft.UI.Xaml.Window.Current.Content;
			}

			// Finally raise the event, either on the OriginalSource or on the capture owners if any
			RaiseUsingCaptures(Move, originalSource, routedArgs);
		}

		internal void OnPointerCancelled(Windows.UI.Core.PointerEventArgs args)
		{
			var (originalSource, _) = HitTest(args);

			// This is how UWP behaves: when out of the bounds of the Window, the root element is use.
			// Note that is another app covers your app, then the OriginalSource on UWP is still the element of your app at the pointer's location.
			originalSource ??= Microsoft.UI.Xaml.Window.Current.Content;

			if (originalSource is null)
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"CoreWindow_PointerCancelled ({args.CurrentPoint.Position}) **undispatched**");
				}

				return;
			}

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"CoreWindow_PointerCancelled [{originalSource.GetDebugName()}]");
			}

			var routedArgs = new PointerRoutedEventArgs(args, originalSource);

			RaiseUsingCaptures(Cancelled, originalSource, routedArgs);
			// Note: No ReleaseCaptures(routedArgs);, the cancel automatically raise it
			ClearPressedState(routedArgs);
		}

		internal void SetPointerCapture(PointerIdentifier uniqueId)
		{
			if (_pointerExtension is not null)
			{
				_pointerExtension.SetPointerCapture(uniqueId, _inputManager._contentRoot.XamlRoot);
			}
			else
			{
				CoreWindow.GetForCurrentThread()!.SetPointerCapture(uniqueId);
			}
		}

		internal void ReleasePointerCapture(PointerIdentifier uniqueId)
		{
			if (_pointerExtension is not null)
			{
				_pointerExtension.ReleasePointerCapture(uniqueId, _inputManager._contentRoot.XamlRoot);
			}
			else
			{
				CoreWindow.GetForCurrentThread()!.ReleasePointerCapture(uniqueId);
			}
		}

		private void ReleaseCaptures(PointerRoutedEventArgs routedArgs)
		{
			if (PointerCapture.TryGet(routedArgs.Pointer, out var capture))
			{
				foreach (var target in capture.Targets.ToList())
				{
					target.Element.ReleasePointerCapture(capture.Pointer.UniqueId, kinds: PointerCaptureKind.Any);
				}
			}
		}

		private void ClearPressedState(PointerRoutedEventArgs routedArgs)
		{
			if (_pressedElements.TryGetValue(routedArgs.Pointer, out var pressedLeaf))
			{
				// We must make sure to clear the pressed state on all elements that was flagged as pressed.
				// This is required as the current originalSource might not be the same as when we pressed (pointer moved),
				// ** OR ** the pointer has been captured by a parent element so we didn't raised to released on the sub elements.

				_pressedElements.Remove(routedArgs.Pointer);

				// Note: The event is propagated silently (public events won't be raised) as it's only to clear internal state
				var ctx = new BubblingContext { IsInternal = true };
				pressedLeaf.OnPointerUp(routedArgs, ctx);
			}
		}

		#region Helpers
		private (UIElement? element, VisualTreeHelper.Branch? stale) HitTest(PointerEventArgs args, StalePredicate? isStale = null)
		{
			if (_inputManager._contentRoot.XamlRoot is null)
			{
				throw new InvalidOperationException("The XamlRoot must be properly initialized for hit testing.");
			}

			return VisualTreeHelper.HitTest(args.CurrentPoint.Position, _inputManager._contentRoot.XamlRoot, isStale: isStale);
		}

		private delegate void RaisePointerEventArgs(UIElement element, PointerRoutedEventArgs args, BubblingContext ctx);
		private readonly record struct PointerEvent(RaisePointerEventArgs Invoke, [CallerMemberName] string Name = "");

		private static readonly PointerEvent Wheel = new((elt, args, ctx) => elt.OnPointerWheel(args, ctx));
		private static readonly PointerEvent Enter = new((elt, args, ctx) => elt.OnPointerEnter(args, ctx));
		private static readonly PointerEvent Leave = new((elt, args, ctx) =>
		{
			elt.OnPointerExited(args, ctx);

			// Even if it's not true, when pointer is leaving an element, we propagate a SILENT (a.k.a. internal) up event to clear the pressed state.
			// Note: This is usually limited only to a given branch (cf. Move)
			// Note: This differs of how we behave on iOS, macOS and Android which does have "implicit capture" while pressed.
			//		 It should only impact the "Pressed" visual states of controls.
			ctx.IsInternal = true;
			args.Handled = false;
			elt.OnPointerUp(args, ctx);
		});
		private static readonly PointerEvent Pressed = new((elt, args, ctx) => elt.OnPointerDown(args, ctx));
		private static readonly PointerEvent Released = new((elt, args, ctx) => elt.OnPointerUp(args, ctx));
		private static readonly PointerEvent Move = new((elt, args, ctx) => elt.OnPointerMove(args, ctx));
		private static readonly PointerEvent Cancelled = new((elt, args, ctx) => elt.OnPointerCancel(args, ctx));

		private static PointerEventDispatchResult Raise(PointerEvent evt, UIElement originalSource, PointerRoutedEventArgs routedArgs)
		{
			routedArgs.Handled = false;
			UIElement.BeginPointerEventDispatch();

			evt.Invoke(originalSource, routedArgs, BubblingContext.Bubble);

			return EndPointerEventDispatch();
		}

		private static PointerEventDispatchResult Raise(PointerEvent evt, VisualTreeHelper.Branch branch, PointerRoutedEventArgs routedArgs)
		{
			routedArgs.Handled = false;
			UIElement.BeginPointerEventDispatch();

			evt.Invoke(branch.Leaf, routedArgs, BubblingContext.BubbleUpTo(branch.Root));

			return UIElement.EndPointerEventDispatch();
		}

		private PointerEventDispatchResult RaiseUsingCaptures(PointerEvent evt, UIElement originalSource, PointerRoutedEventArgs routedArgs)
		{
			routedArgs.Handled = false;
			UIElement.BeginPointerEventDispatch();

			if (PointerCapture.TryGet(routedArgs.Pointer, out var capture))
			{
				var targets = capture.Targets.ToList();
				if (capture.IsImplicitOnly)
				{
					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().Trace($"[Implicit capture] raising event {evt.Name} (args: {routedArgs.GetHashCode():X8}) to original source first [{originalSource.GetDebugName()}]");
					}

					evt.Invoke(originalSource, routedArgs, BubblingContext.Bubble);

					foreach (var target in targets)
					{
						if (this.Log().IsEnabled(LogLevel.Trace))
						{
							this.Log().Trace($"[Implicit capture] raising event {evt.Name} (args: {routedArgs.GetHashCode():X8}) to capture target [{originalSource.GetDebugName()}] (-- no bubbling--)");
						}

						routedArgs.Handled = false;
						evt.Invoke(target.Element, routedArgs, BubblingContext.NoBubbling);
					}
				}
				else
				{
					var explicitTarget = targets.Find(c => c.Kind.HasFlag(PointerCaptureKind.Explicit))!;

					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().Trace($"[Explicit capture] raising event {evt.Name} (args: {routedArgs.GetHashCode():X8}) to capture target [{explicitTarget.Element.GetDebugName()}]");
					}

					evt.Invoke(explicitTarget.Element, routedArgs, BubblingContext.Bubble);

					foreach (var target in targets)
					{
						if (target == explicitTarget)
						{
							continue;
						}

						if (this.Log().IsEnabled(LogLevel.Trace))
						{
							this.Log().Trace($"[Explicit capture] raising event {evt.Name} (args: {routedArgs.GetHashCode():X8}) to alternative (implicit) target [{explicitTarget.Element.GetDebugName()}] (-- no bubbling--)");
						}

						routedArgs.Handled = false;
						evt.Invoke(target.Element, routedArgs, BubblingContext.NoBubbling);
					}
				}
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"[No capture] raising event {evt.Name} (args: {routedArgs.GetHashCode():X8}) to original source [{originalSource.GetDebugName()}]");
				}

				evt.Invoke(originalSource, routedArgs, BubblingContext.Bubble);
			}

			return UIElement.EndPointerEventDispatch();
		}
		#endregion
	}

}
#endif
