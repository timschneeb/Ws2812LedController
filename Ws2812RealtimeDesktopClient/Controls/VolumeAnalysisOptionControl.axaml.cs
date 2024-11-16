using System.Collections;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Ws2812LedController.AudioReactive.Model;

namespace Ws2812RealtimeDesktopClient.Controls;

public class VolumeAnalysisOptionControl : UserControl
{
    public VolumeAnalysisOptionControl()
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
        if (e.Property == VolumeAnalysisOptionProperty)
        {
            UpdateTypeSelection();
        } 
        if (e.Property == SelectedOptionProperty)
        {
            // Ignore initial set
            if (e.OldValue != null)
            {
                VolumeAnalysisOption = SelectedOption == 1 ? new FixedVolumeAnalysisOption() : new AgcVolumeAnalysisOption();
            }
        }
        
        if (e.Property == VolumeAnalysisOptionProperty || e.Property == SelectedOptionProperty)
        {
            IsAgc = SelectedOption == 0;
            IsFixed = SelectedOption == 1;
        }
    }

    public void UpdateTypeSelection()
    {
        var newValue = VolumeAnalysisOption.GetType() == typeof(FixedVolumeAnalysisOption) ? 1 : 0;
        if (newValue != SelectedOption)
        {
            SelectedOption = newValue;
        }
    }
    
    public static readonly StyledProperty<IVolumeAnalysisOption> VolumeAnalysisOptionProperty =
        AvaloniaProperty.Register<VolumeAnalysisOptionControl, IVolumeAnalysisOption>(nameof(VolumeAnalysisOption),
            defaultBindingMode: BindingMode.TwoWay, defaultValue: new AgcVolumeAnalysisOption());
		
    public IVolumeAnalysisOption VolumeAnalysisOption
    {
        get => GetValue(VolumeAnalysisOptionProperty);
        set => SetValue(VolumeAnalysisOptionProperty, value);
    } 
    
    public static readonly StyledProperty<int?> SelectedOptionProperty =
        AvaloniaProperty.Register<VolumeAnalysisOptionControl, int?>(nameof(SelectedOption),
            defaultBindingMode: BindingMode.TwoWay, defaultValue: null);
		
    public int? SelectedOption
    {
        get => GetValue(SelectedOptionProperty);
        set => SetValue(SelectedOptionProperty, value);
    }

    public static readonly StyledProperty<bool> IsFixedProperty =
        AvaloniaProperty.Register<VolumeAnalysisOptionControl, bool>(nameof(IsFixed),
            defaultBindingMode: BindingMode.OneWay);
		
    public bool IsFixed
    {
        get => GetValue(IsFixedProperty);
        set => SetValue(IsFixedProperty, value);
    }
    
    public static readonly StyledProperty<bool> IsAgcProperty =
        AvaloniaProperty.Register<VolumeAnalysisOptionControl, bool>(nameof(IsAgc),
            defaultBindingMode: BindingMode.OneWay);
		
    public bool IsAgc
    {
        get => GetValue(IsAgcProperty);
        set => SetValue(IsAgcProperty, value);
    }
    
    public AvaloniaList<string> OptionTypes => new()
    {
        "Automatic gain control", /* index 0 */
        "Fixed magnitude limits", /* index 1 */
    };

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
}