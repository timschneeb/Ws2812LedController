using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Ws2812LedController.AudioReactive.Dsp;

namespace Ws2812RealtimeDesktopClient.Controls;

public class FftBinSelectorControl : UserControl
{
    public FftBinSelectorControl()
    {
        DataContext = this;
        
        InitializeComponent();
        
        PropertyChanged += OnPropertyChanged;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == IsOptionalProperty || e.Property == FftBinsProperty)
        {
            var checkbox = this.FindControl<CheckBox>("EnableFft");
            checkbox.IsChecked = FftBins != null || !IsOptional;
            checkbox.IsVisible = IsOptional;
        }
    }
    
    public static readonly StyledProperty<FftCBinSelector?> FftBinsProperty =
        AvaloniaProperty.Register<FftBinSelectorControl, FftCBinSelector?>(nameof(FftBins),
            defaultBindingMode: BindingMode.TwoWay);
		
    public FftCBinSelector? FftBins
    {
        get => GetValue(FftBinsProperty);
        set => SetValue(FftBinsProperty, value);
    }
    
    public static readonly StyledProperty<bool> IsOptionalProperty =
        AvaloniaProperty.Register<FftBinSelectorControl, bool>(nameof(IsOptional),
            defaultBindingMode: BindingMode.OneWay);
		
    public bool IsOptional
    {
        get => GetValue(IsOptionalProperty);
        set => SetValue(IsOptionalProperty, value);
    }

    private void Visual_OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        // We will set the focus into our input field just after it got attached to the visual tree.
        if (sender is InputElement inputElement)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                KeyboardDevice.Instance?.SetFocusedElement(inputElement, NavigationMethod.Unspecified,
                    KeyModifiers.None);
            });
        }
    }

    private void EnableFFT_OnCheckStateChanged(object? sender, RoutedEventArgs e)
    {
        var isChecked = this.FindControl<CheckBox>("EnableFft").IsChecked == true;
        var bins = FftBins ?? new FftCBinSelector(0, 0);
        FftBins = isChecked ? bins : null;
    }
}