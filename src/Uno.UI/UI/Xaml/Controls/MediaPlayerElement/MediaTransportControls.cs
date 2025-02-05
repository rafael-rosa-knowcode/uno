using System;
using System.Timers;
using Uno.UI.Converters;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Uno.UI.Xaml.Controls.MediaPlayer.Internal;

#if __IOS__
using UIKit;
#elif __MACOS__
using AppKit;
#elif __ANDROID__
using Uno.UI;
#endif

namespace Windows.UI.Xaml.Controls
{
	[TemplatePart(Name = "RootGrid", Type = typeof(Grid))]
	[TemplatePart(Name = "PlayPauseButton", Type = typeof(Button))]
	[TemplatePart(Name = "PlayPauseButtonOnLeft", Type = typeof(Button))]
	[TemplatePart(Name = "VolumeMuteButton", Type = typeof(Button))]
	[TemplatePart(Name = "AudioMuteButton", Type = typeof(Button))]
	[TemplatePart(Name = "VolumeSlider", Type = typeof(Slider))]
	[TemplatePart(Name = "FullWindowButton", Type = typeof(Button))]
	[TemplatePart(Name = "CastButton", Type = typeof(Button))]
	[TemplatePart(Name = "ZoomButton", Type = typeof(Button))]
	[TemplatePart(Name = "PlaybackRateButton", Type = typeof(Button))]
	[TemplatePart(Name = "PlaybackRateButton", Type = typeof(Button))]
	[TemplatePart(Name = "SkipForwardButton", Type = typeof(Button))]
	[TemplatePart(Name = "NextTrackButton", Type = typeof(Button))]
	[TemplatePart(Name = "FastForwardButton", Type = typeof(Button))]
	[TemplatePart(Name = "RewindButton", Type = typeof(Button))]
	[TemplatePart(Name = "PreviousTrackButton", Type = typeof(Button))]
	[TemplatePart(Name = "SkipBackwardButton", Type = typeof(Button))]
	[TemplatePart(Name = "StopButton", Type = typeof(Button))]
	[TemplatePart(Name = "AudioTracksSelectionButton", Type = typeof(Button))]
	[TemplatePart(Name = "CCSelectionButton", Type = typeof(Button))]
	[TemplatePart(Name = "TimeElapsedElement", Type = typeof(TextBlock))]
	[TemplatePart(Name = "TimeRemainingElement", Type = typeof(TextBlock))]
	[TemplatePart(Name = "ProgressSlider", Type = typeof(Slider))]
	[TemplatePart(Name = "BufferingProgressBar", Type = typeof(ProgressBar))]
	[TemplatePart(Name = "DownloadProgressIndicator", Type = typeof(ProgressBar))]
	public partial class MediaTransportControls : Control
	{
		private const string RootGridName = "RootGrid";
		private const string PlayPauseButtonName = "PlayPauseButton";
		private const string PlayPauseButtonOnLeftName = "PlayPauseButtonOnLeft";
		private const string VolumeMuteButtonName = "VolumeMuteButton";
		private const string AudioMuteButtonName = "AudioMuteButton";
		private const string VolumeSliderName = "VolumeSlider";
		private const string FullWindowButtonName = "FullWindowButton";
		private const string CastButtonName = "CastButton";
		private const string ZoomButtonName = "ZoomButton";
		private const string PlaybackRateButtonName = "PlaybackRateButton";
		private const string SkipForwardButtonName = "SkipForwardButton";
		private const string RepeatVideoButtonName = "RepeatVideoButton";
		private const string NextTrackButtonName = "NextTrackButton";
		private const string FastForwardButtonName = "FastForwardButton";
		private const string RewindButtonName = "RewindButton";
		private const string PreviousTrackButtonName = "PreviousTrackButton";
		private const string SkipBackwardButtonName = "SkipBackwardButton";
		private const string StopButtonName = "StopButton";
		private const string AudioTracksSelectionButtonName = "AudioTracksSelectionButton";
		private const string CCSelectionButtonName = "CCSelectionButton";
		private const string TimeElapsedElementName = "TimeElapsedElement";
		private const string TimeRemainingElementName = "TimeRemainingElement";
		private const string ProgressSliderName = "ProgressSlider";
		private const string BufferingProgressBarName = "BufferingProgressBar";
		private const string TimelineContainerName = "MediaTransportControls_Timeline_Border";
		private const string HorizontalThumbName = "HorizontalThumb";
		private const string DownloadProgressIndicatorName = "DownloadProgressIndicator";
		private const string CompactOverlayButtonName = "CompactOverlayButton";

		private Grid _rootGrid;
		private Button _playPauseButton;
		private Button _playPauseButtonOnLeft;
		private Button _volumeMuteButton;
		private Button _audioMuteButton;
		private Slider _volumeSlider;
		private Button _fullWindowButton;
		private Button _castButton;
		private Button _zoomButton;
		private Button _playbackRateButton;
		private Button _skipForwardButton;
		private Button _repeatVideoButton;
		private Button _nextTrackButton;
		private Button _fastForwardButton;
		private Button _rewindButton;
		private Button _compactOverlayButton;
		private Button _previousTrackButton;
		private Button _skipBackwardButton;
		private Button _stopButton;
		private Button _audioTracksSelectionButton;
		private Button _ccSelectionButton;
		private TextBlock _timeElapsedElement;
		private TextBlock _timeRemainingElement;
		private Slider _progressSlider;
		private ProgressBar _bufferingProgressBar;
		private Border _timelineContainer;
		private ProgressBar _downloadProgressIndicator;

		private Timer _controlsVisibilityTimer;
		private bool _wasPlaying;
		private bool _isInteractive;
		private MediaPlayerElement _mpe;

		public MediaTransportControls() : base()
		{
			_controlsVisibilityTimer = new Timer()
			{
				AutoReset = false,
				Interval = 3000
			};
			_controlsVisibilityTimer.Elapsed += ControlsVisibilityTimerElapsed;
			DefaultStyleKey = typeof(MediaTransportControls);
		}

		internal void SetMediaPlayerElement(MediaPlayerElement mediaPlayerElement)
		{
			_mpe = mediaPlayerElement;
		}

		private void ControlsVisibilityTimerElapsed(object sender, ElapsedEventArgs args)
		{
			if (ShowAndHideAutomatically)
			{
				Hide();
			}

			_controlsVisibilityTimer.Stop();
		}

		private void ResetControlsVisibilityTimer()
		{
			if (ShowAndHideAutomatically)
			{
				_controlsVisibilityTimer.Stop();
				_controlsVisibilityTimer.Start();
			}
		}

		private void CancelControlsVisibilityTimer()
		{
			Show();
			_controlsVisibilityTimer.Stop();
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			UnbindMediaPlayer();

			var trueToVisible = new FromNullableBoolToVisibilityConverter();

			_playPauseButton = this.GetTemplateChild(PlayPauseButtonName) as Button;

			_playPauseButtonOnLeft = this.GetTemplateChild(PlayPauseButtonOnLeftName) as Button;

			_volumeMuteButton = this.GetTemplateChild(VolumeMuteButtonName) as Button;
			_volumeMuteButton?.SetBinding(Button.VisibilityProperty, new Binding { Path = "IsVolumeButtonVisible", Source = this, Mode = BindingMode.OneWay, FallbackValue = Visibility.Collapsed, Converter = trueToVisible });
			_volumeMuteButton?.SetBinding(Button.IsEnabledProperty, new Binding { Path = "IsVolumeEnabled", Source = this, Mode = BindingMode.OneWay, FallbackValue = true });

			_audioMuteButton = this.GetTemplateChild(AudioMuteButtonName) as Button;

			_volumeSlider = this.GetTemplateChild(VolumeSliderName) as Slider;
			if (_volumeSlider != null)
			{
				_volumeSlider.Maximum = 100;
				_volumeSlider.Value = 100;
			}

			_fullWindowButton = this.GetTemplateChild(FullWindowButtonName) as Button;
			if (_fullWindowButton != null)
			{
				_fullWindowButton.SetBinding(Button.VisibilityProperty, new Binding { Path = "IsFullWindowButtonVisible", Source = this, Mode = BindingMode.OneWay, FallbackValue = Visibility.Collapsed, Converter = trueToVisible });
				_fullWindowButton.SetBinding(Button.IsEnabledProperty, new Binding { Path = "IsFullWindowEnabled", Source = this, Mode = BindingMode.OneWay, FallbackValue = true });
				_fullWindowButton.Tapped -= FullWindowButtonTapped;
				_fullWindowButton.Tapped += FullWindowButtonTapped;
			}

			_castButton = this.GetTemplateChild(CastButtonName) as Button;

			_zoomButton = this.GetTemplateChild(ZoomButtonName) as Button;

			if (_zoomButton != null)
			{
				_zoomButton?.SetBinding(Button.VisibilityProperty, new Binding { Path = "IsZoomButtonVisible", Source = this, Mode = BindingMode.OneWay, FallbackValue = Visibility.Collapsed, Converter = trueToVisible });
				_zoomButton?.SetBinding(Button.IsEnabledProperty, new Binding { Path = "IsZoomEnabled", Source = this, Mode = BindingMode.OneWay, FallbackValue = true });
				_zoomButton.Tapped -= ZoomButtonTapped;
				_zoomButton.Tapped += ZoomButtonTapped;
			}

			_playbackRateButton = this.GetTemplateChild(PlaybackRateButtonName) as Button;
			_playbackRateButton?.SetBinding(Button.VisibilityProperty, new Binding { Path = "IsPlaybackRateButtonVisible", Source = this, Mode = BindingMode.OneWay, FallbackValue = Visibility.Collapsed, Converter = trueToVisible });
			_playbackRateButton?.SetBinding(Button.IsEnabledProperty, new Binding { Path = "IsPlaybackRateEnabled", Source = this, Mode = BindingMode.OneWay, FallbackValue = true });
			_playbackRateButton.Tapped -= PlaybackRateButtonTapped;
			_playbackRateButton.Tapped += PlaybackRateButtonTapped;

			_compactOverlayButton = this.GetTemplateChild(CompactOverlayButtonName) as Button;
			_compactOverlayButton?.SetBinding(Button.VisibilityProperty, new Binding { Path = "IsCompactOverlayButtonVisible", Source = this, Mode = BindingMode.OneWay, FallbackValue = Visibility.Collapsed, Converter = trueToVisible });
			_compactOverlayButton?.SetBinding(Button.IsEnabledProperty, new Binding { Path = "IsCompactOverlayEnabled", Source = this, Mode = BindingMode.OneWay, FallbackValue = true });
			_compactOverlayButton.Tapped -= UpdateMediaTransportControlMode;
			_compactOverlayButton.Tapped += UpdateMediaTransportControlMode;

			_repeatVideoButton = this.GetTemplateChild(RepeatVideoButtonName) as Button;
			_repeatVideoButton?.SetBinding(Button.VisibilityProperty, new Binding { Path = "IsRepeatButtonVisible", Source = this, Mode = BindingMode.OneWay, FallbackValue = Visibility.Collapsed, Converter = trueToVisible });
			_repeatVideoButton?.SetBinding(Button.IsEnabledProperty, new Binding { Path = "IsRepeatEnabled", Source = this, Mode = BindingMode.OneWay, FallbackValue = true });
			_repeatVideoButton.Tapped -= IsRepeatEnabledButtonTapped;
			_repeatVideoButton.Tapped += IsRepeatEnabledButtonTapped;

			_skipForwardButton = this.GetTemplateChild(SkipForwardButtonName) as Button;
			_skipForwardButton?.SetBinding(Button.VisibilityProperty, new Binding { Path = "IsSkipForwardButtonVisible", Source = this, Mode = BindingMode.OneWay, FallbackValue = Visibility.Collapsed, Converter = trueToVisible });
			_skipForwardButton?.SetBinding(Button.IsEnabledProperty, new Binding { Path = "IsSkipForwardEnabled", Source = this, Mode = BindingMode.OneWay, FallbackValue = true });

			_nextTrackButton = this.GetTemplateChild(NextTrackButtonName) as Button;
			_nextTrackButton?.SetBinding(Button.VisibilityProperty, new Binding { Path = "IsNextTrackButtonVisible", Source = this, Mode = BindingMode.OneWay, FallbackValue = Visibility.Collapsed, Converter = trueToVisible });
			_nextTrackButton.Tapped -= NextTrackButtonTapped;
			_nextTrackButton.Tapped += NextTrackButtonTapped;

			_previousTrackButton = this.GetTemplateChild(PreviousTrackButtonName) as Button;
			_previousTrackButton?.SetBinding(Button.VisibilityProperty, new Binding { Path = "IsPreviousTrackButtonVisible", Source = this, Mode = BindingMode.OneWay, FallbackValue = Visibility.Collapsed, Converter = trueToVisible });
			_previousTrackButton.Tapped -= PreviousTrackButtonTapped;
			_previousTrackButton.Tapped += PreviousTrackButtonTapped;

			_fastForwardButton = this.GetTemplateChild(FastForwardButtonName) as Button;
			_fastForwardButton?.SetBinding(Button.VisibilityProperty, new Binding { Path = "IsFastForwardButtonVisible", Source = this, Mode = BindingMode.OneWay, FallbackValue = Visibility.Collapsed, Converter = trueToVisible });
			_fastForwardButton?.SetBinding(Button.IsEnabledProperty, new Binding { Path = "IsFastForwardEnabled", Source = this, Mode = BindingMode.OneWay, FallbackValue = true });

			_rewindButton = this.GetTemplateChild(RewindButtonName) as Button;
			_rewindButton?.SetBinding(Button.VisibilityProperty, new Binding { Path = "IsFastRewindButtonVisible", Source = this, Mode = BindingMode.OneWay, FallbackValue = Visibility.Collapsed, Converter = trueToVisible });
			_rewindButton?.SetBinding(Button.IsEnabledProperty, new Binding { Path = "IsFastRewindEnabled", Source = this, Mode = BindingMode.OneWay, FallbackValue = true });

			_skipBackwardButton = this.GetTemplateChild(SkipBackwardButtonName) as Button;
			_skipBackwardButton?.SetBinding(Button.VisibilityProperty, new Binding { Path = "IsSkipBackwardButtonVisible", Source = this, Mode = BindingMode.OneWay, FallbackValue = Visibility.Collapsed, Converter = trueToVisible });
			_skipBackwardButton?.SetBinding(Button.IsEnabledProperty, new Binding { Path = "IsSkipBackwardEnabled", Source = this, Mode = BindingMode.OneWay, FallbackValue = true });

			_stopButton = this.GetTemplateChild(StopButtonName) as Button;
			_stopButton?.SetBinding(Button.VisibilityProperty, new Binding { Path = "IsStopButtonVisible", Source = this, Mode = BindingMode.OneWay, FallbackValue = Visibility.Collapsed, Converter = trueToVisible });
			_stopButton?.SetBinding(Button.IsEnabledProperty, new Binding { Path = "IsStopEnabled", Source = this, Mode = BindingMode.OneWay, FallbackValue = true });

			_audioTracksSelectionButton = this.GetTemplateChild(AudioTracksSelectionButtonName) as Button;

			_ccSelectionButton = this.GetTemplateChild(CCSelectionButtonName) as Button;

			_timeElapsedElement = this.GetTemplateChild(TimeElapsedElementName) as TextBlock;

			_timeRemainingElement = this.GetTemplateChild(TimeRemainingElementName) as TextBlock;

			_progressSlider = this.GetTemplateChild(ProgressSliderName) as Slider;
			PropertyChangedCallback callback = OnSliderTemplateChanged;
			_progressSlider?.RegisterDisposablePropertyChangedCallback(Slider.TemplateProperty, callback);

			_bufferingProgressBar = this.GetTemplateChild(BufferingProgressBarName) as ProgressBar;

			_timelineContainer = this.GetTemplateChild(TimelineContainerName) as Border;
			_timelineContainer?.SetBinding(Border.VisibilityProperty, new Binding { Path = "IsSeekBarVisible", Source = this, Mode = BindingMode.OneWay, FallbackValue = Visibility.Collapsed, Converter = trueToVisible });
			_timelineContainer?.SetBinding(Border.IsEnabledProperty, new Binding { Path = "IsSeekEnabled", Source = this, Mode = BindingMode.OneWay, FallbackValue = true });

			_downloadProgressIndicator = _progressSlider?.GetTemplateChild(DownloadProgressIndicatorName) as ProgressBar;

			UpdateMediaTransportControlMode();

			_rootGrid = this.GetTemplateChild(RootGridName) as Grid;
			if (_rootGrid != null)
			{
				_rootGrid.Tapped -= OnRootGridTapped;
				_rootGrid.Tapped += OnRootGridTapped;
			}

			if (_mediaPlayer != null)
			{
				BindMediaPlayer();
			}
		}

		private void FullWindowButtonTapped(object sender, RoutedEventArgs e)
		{
			_mpe.IsFullWindow = !_mpe.IsFullWindow;
			UpdateFullscreenButtonStyle();
		}
		private void PlaybackRateButtonTapped(object sender, RoutedEventArgs e)
		{
			_mpe.MediaPlayer.PlaybackRate += 0.25;
		}
		private void IsRepeatEnabledButtonTapped(object sender, RoutedEventArgs e)
		{
			_mpe.MediaPlayer.IsLoopingEnabled = !_mpe.MediaPlayer.IsLoopingEnabled;
		}
		private void PreviousTrackButtonTapped(object sender, RoutedEventArgs e)
		{
			_mediaPlayer.PlaybackSession.Position = TimeSpan.Zero;
			_mpe.MediaPlayer.PlaybackSession.Position = TimeSpan.Zero;
		}
		private void NextTrackButtonTapped(object sender, RoutedEventArgs e)
		{
			_mediaPlayer.PlaybackSession.Position = _mediaPlayer.PlaybackSession.NaturalDuration;
			_mpe.MediaPlayer.PlaybackSession.Position = _mediaPlayer.PlaybackSession.NaturalDuration;
		}

		private void UpdateFullscreenButtonStyle()
		{
			if (_mpe.IsFullWindow)
			{
				VisualStateManager.GoToState(this, "FullWindowState", false);
			}
			else
			{
				VisualStateManager.GoToState(this, "NonFullWindowState", false);
			}
		}

		private void ZoomButtonTapped(object sender, RoutedEventArgs e)
		{
			if (_mpe.Stretch == Stretch.Uniform)
			{
				_mpe.Stretch = Stretch.UniformToFill;
			}
			else
			{
				_mpe.Stretch = Stretch.Uniform;
			}
		}

		public void Show()
		{
			_isInteractive = true;

			_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				VisualStateManager.GoToState(this, "ControlPanelFadeIn", false);
			});
		}

		public void Hide()
		{
			_isInteractive = false;

			_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				if (_mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Buffering || _mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
				{
					VisualStateManager.GoToState(this, "ControlPanelFadeOut", false);
				}
			});
		}

		private void OnRootGridTapped(object sender, TappedRoutedEventArgs e)
		{
			if (_isInteractive)
			{
				_controlsVisibilityTimer.Stop();
				Hide();
			}
			else
			{
				Show();

				if (ShowAndHideAutomatically)
				{
					ResetControlsVisibilityTimer();
				}
			}
		}
		private void UpdateMediaTransportControlMode(object sender, RoutedEventArgs e)
		{
			IsCompact = !IsCompact;
			UpdateMediaTransportControlMode();
		}
		private void UpdateMediaTransportControlMode()
		{
			VisualStateManager.GoToState(this, IsCompact ? "CompactMode" : "NormalMode", true);
		}

		private static void OnIsCompactChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			VisualStateManager.GoToState((MediaTransportControls)dependencyObject, (bool)args.NewValue ? "CompactMode" : "NormalMode", false);
		}

		private static void OnShowAndHideAutomaticallyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			if ((bool)args.NewValue)
			{
				((MediaTransportControls)dependencyObject).ResetControlsVisibilityTimer();
			}
			else
			{
				((MediaTransportControls)dependencyObject).CancelControlsVisibilityTimer();
			}
		}

		private static void OnIsSeekBarVisibleChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			((MediaTransportControls)dependencyObject)._timelineContainer.Visibility = (bool)args.NewValue ? Visibility.Visible : Visibility.Collapsed;
		}

		private static void OnIsSeekEnabledChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			VisualStateManager.GoToState(((MediaTransportControls)dependencyObject)._progressSlider, (bool)args.NewValue ? "Normal" : "Disabled", false);
		}
	}
}
