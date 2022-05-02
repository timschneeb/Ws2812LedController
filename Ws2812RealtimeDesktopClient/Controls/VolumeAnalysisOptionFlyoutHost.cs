using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using FluentAvalonia.Core;
using FluentAvalonia.UI.Controls;
using Ws2812LedController.AudioReactive.Dsp;
using Ws2812LedController.AudioReactive.Model;
using Ws2812RealtimeDesktopClient.Converters;

namespace Ws2812RealtimeDesktopClient.Controls
{
    public class VolumeAnalysisOptionFlyoutHost : UserControl
    {
		public VolumeAnalysisOptionFlyoutHost()
		{
			VerticalAlignment = VerticalAlignment.Stretch;
			HorizontalAlignment = HorizontalAlignment.Left;

			Content = new TextBlock()
			{
				DataContext = this,
				Margin = new Thickness(8, 0),
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Left,
				[!TextBlock.TextProperty] = new Binding("VolumeAnalysisOption")
				{
					Converter = new FuncValueDynamicConverter<IVolumeAnalysisOption, string>
						(opt => opt?.ToString() ?? "???")
				}
			};
		}
		
		protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
		{
			base.OnAttachedToVisualTree(e);
			Flyout.Closed += OnFlyoutClosed;

			// Dirty hack to automatically open the flyout
			Task.Delay(10).ContinueWith(_ => Dispatcher.UIThread.Post(Open, DispatcherPriority.Loaded));
		}

		protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
		{
			base.OnDetachedFromVisualTree(e);
			Flyout.Closed -= OnFlyoutClosed;
		}

		protected override void OnPointerPressed(PointerPressedEventArgs e)
		{
			base.OnPointerPressed(e);
			
			Open();
		}
		
		public void Open()
		{
			Flyout.Picker.VolumeAnalysisOption = VolumeAnalysisOption;
			Flyout.Picker.UpdateTypeSelection();

			Flyout.ShowAt(this);

			// Keep track of which button the flyout is active on
			_flyoutActive = true;

            FlyoutOpened?.Invoke(this, EventArgs.Empty);
		}
		
		private void OnFlyoutClosed(object? sender, EventArgs e)
		{
            if (_flyoutActive)
            {
	            VolumeAnalysisOption = Flyout.Picker.VolumeAnalysisOption;
	            FlyoutClosed?.Invoke(this, EventArgs.Empty);
                _flyoutActive = false;
            }
        }

		public static readonly StyledProperty<IVolumeAnalysisOption> VolumeAnalysisOptionProperty =
			AvaloniaProperty.Register<VolumeAnalysisOptionFlyoutHost, IVolumeAnalysisOption>(nameof(VolumeAnalysisOption),
				defaultBindingMode: BindingMode.TwoWay);
		
		public IVolumeAnalysisOption VolumeAnalysisOption
		{
			get => GetValue(VolumeAnalysisOptionProperty);
			set => SetValue(VolumeAnalysisOptionProperty, value);
		}
		
		public event TypedEventHandler<VolumeAnalysisOptionFlyoutHost, EventArgs>? FlyoutOpened;
		public event TypedEventHandler<VolumeAnalysisOptionFlyoutHost, EventArgs>? FlyoutClosed;

		private static readonly CustomFlyout<VolumeAnalysisOptionControl> Flyout = new(new VolumeAnalysisOptionControl());
		private bool _flyoutActive;	
	}
}
