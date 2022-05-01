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
using Ws2812RealtimeDesktopClient.Converters;

namespace Ws2812RealtimeDesktopClient.Controls
{
    public class FftBinSelectorFlyoutHost : UserControl
    {
	    public event TypedEventHandler<FftBinSelectorFlyoutHost, FftCBinSelector?>? FftBinsChanged;

		public FftBinSelectorFlyoutHost()
		{
			//DataContext = this;
			
			VerticalAlignment = VerticalAlignment.Stretch;
			HorizontalAlignment = HorizontalAlignment.Left;

			Content = new TextBlock()
			{
				DataContext = this,
				Margin = new Thickness(8, 0),
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Left,
				[!TextBlock.TextProperty] = new Binding("FftBins")
				{
					Converter = new FuncValueDynamicConverter<FftCBinSelector?, string>
						(bins => bins?.ToString() ?? "Disabled")
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

		protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
		{
			base.OnPropertyChanged(change);
			if (change.Property == FftBinsProperty)
			{
				FftBinsChanged?.Invoke(this, change.NewValue.GetValueOrDefault<FftCBinSelector?>());
			}
		}

		public void Open()
		{
			Flyout.Picker.FftBins = FftBins;
			Flyout.Picker.IsOptional = IsOptional;

			Flyout.ShowAt(this);

			// Keep track of which button the flyout is active on
			_flyoutActive = true;

            FlyoutOpened?.Invoke(this, EventArgs.Empty);
		}
		
		private void OnFlyoutClosed(object? sender, EventArgs e)
		{
            if (_flyoutActive)
            {
	            var bins = Flyout.Picker.FftBins;
	            if (bins == null && !IsOptional)
	            {
		            bins = new FftCBinSelector(0, 0);
	            }

	            if (bins != null && bins.Start > bins.End)
	            {
		            _ = new ContentDialog()
		            {
			            Content = "The last bin index must be lesser or equal to the first bin index",
			            Title = "Error",
			            PrimaryButtonText = "Close"
		            }.ShowAsync();
	            }
	            else
	            {
		            FftBins = bins;
	            }
	            
                FlyoutClosed?.Invoke(this, EventArgs.Empty);
                _flyoutActive = false;
            }
        }

		public static readonly StyledProperty<FftCBinSelector?> FftBinsProperty =
			AvaloniaProperty.Register<FftBinSelectorFlyoutHost, FftCBinSelector?>(nameof(FftBins),
				defaultBindingMode: BindingMode.TwoWay);
		
		public FftCBinSelector? FftBins
		{
			get => GetValue(FftBinsProperty);
			set => SetValue(FftBinsProperty, value);
		}
		    
		public static readonly StyledProperty<bool> IsOptionalProperty =
			AvaloniaProperty.Register<FftBinSelectorFlyoutHost, bool>(nameof(IsOptional),
				defaultBindingMode: BindingMode.OneWay);
		
		public bool IsOptional
		{
			get => GetValue(IsOptionalProperty);
			init => SetValue(IsOptionalProperty, value);
		}
		
		public event TypedEventHandler<FftBinSelectorFlyoutHost, EventArgs>? FlyoutOpened;
		public event TypedEventHandler<FftBinSelectorFlyoutHost, EventArgs>? FlyoutClosed;

		private static readonly CustomFlyout<FftBinSelectorControl> Flyout = new(new FftBinSelectorControl());
		private bool _flyoutActive;	
	}
}
