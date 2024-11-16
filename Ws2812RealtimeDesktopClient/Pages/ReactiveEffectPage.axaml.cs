using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Media;
using Ws2812LedController.AudioReactive.Dsp;
using Ws2812LedController.AudioReactive.Model;
using Ws2812LedController.Core.FastLedCompatibility;
using Ws2812LedController.Core.Model;
using Ws2812RealtimeDesktopClient.Controls;
using Ws2812RealtimeDesktopClient.Converters;
using Ws2812RealtimeDesktopClient.Models;
using Ws2812RealtimeDesktopClient.Utilities;
using Ws2812RealtimeDesktopClient.ViewModels;
using Color = System.Drawing.Color;
using ComboBox = Avalonia.Controls.ComboBox;

namespace Ws2812RealtimeDesktopClient.Pages
{
    public class ReactiveEffectPage : UserControl
    {
        public ReactiveEffectPage()
        {
            InitializeComponent();

            var context = new ReactiveEffectPageViewModel();
            context.PropertyChanged += OnPropertyChanged;
            context.SelectedAssignment = context.Assignments.FirstOrDefault();
            DataContext = context;

             var grid = this.FindControl<DataGrid>("PropertyGrid");
             var valueColumn = grid.Columns[1] as DataGridTemplateColumn;
             Debug.Assert(valueColumn != null, "Can't find value column");
             valueColumn.CellTemplate = new FuncDataTemplate(_ => true, BuildPreviewControl); 
             valueColumn.CellEditingTemplate = new FuncDataTemplate(_ => true, BuildEditorControl);
        }

        private IControl BuildEditorControl(object itemModel, INameScope _)
        {
            var row = (itemModel as PropertyRow);
            if (row?.Type == typeof(bool))
            {
                return new CheckBox()
                {
                    Margin = new Thickness(8, 0),
                    [!ToggleButton.IsCheckedProperty] = new Binding("Value", BindingMode.TwoWay)
                };
            }
            if (row?.Type == typeof(FftCBinSelector))
            {
                return new FftBinSelectorFlyoutHost()
                {
                    IsOptional = row.IsNullable,
                    [!FftBinSelectorFlyoutHost.FftBinsProperty] = new Binding("Value", BindingMode.TwoWay)
                };
            } 
            if (row?.Type == typeof(IVolumeAnalysisOption))
            {
                return new VolumeAnalysisOptionFlyoutHost()
                {
                    [!VolumeAnalysisOptionFlyoutHost.VolumeAnalysisOptionProperty] = new Binding("Value", BindingMode.TwoWay)
                };
            }
            if (row?.Type == typeof(CRGBPalette16))
            {
                return new ComboBox()
                {
                    Items = PaletteManager.Instance.PaletteEntries.ToArray(),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    DataTemplates =
                    {
                        new FuncDataTemplate(x => true, (x,y) => new PaletteViewControl()
                        {
                            Margin = new Thickness(8, 0),
                            [!PaletteViewControl.ColorsProperty] = new Binding("PaletteColors")
                        }, false)
                    },
                    [!SelectingItemsControl.SelectedItemProperty] = new Binding("Value", BindingMode.TwoWay)
                    {
                        Converter = new FuncValueDynamicConverter<CRGBPalette16, PaletteEntry>
                        (pal => (PaletteEntry)(pal == null ? AvaloniaProperty.UnsetValue : new PaletteEntry("", pal.Entries)), 
                            entry => (CRGBPalette16)(entry == null ? AvaloniaProperty.UnsetValue : new CRGBPalette16(entry.PaletteColors)))
                    }
                };
            }  
            if (row?.Type == typeof(Color))
            {
                return new ColorPickerButton()
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    UseColorWheel = true,
                    UseSpectrum = true,
                    UseColorTriangle = true,
                    ShowAcceptDismissButtons = false,
                    [!ColorPickerButton.ColorProperty] = new Binding("Value", BindingMode.TwoWay)
                    {
                        Converter = new FuncValueDynamicConverter<Color, Color2>
                        (c => Avalonia.Media.Color.FromArgb(c.A, c.R, c.G, c.B),
                            c => Color.FromArgb(c.A, c.R, c.G, c.B))
                    }
                };
            }  
            if (row?.Type.IsEnum ?? false)
            {
                // TODO dynamic type casting using reflection
                IValueConverter? converter = null; 
                if (row.Type == typeof(Edge))
                {
                    converter = new FuncValueDynamicConverter<Edge, int>();
                }
                else
                {
                    throw new NotImplementedException("Enum type not known");
                }
                
                return new ComboBox()
                {
                    Items = row.Type.GetEnumNames(),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    [!SelectingItemsControl.SelectedIndexProperty] = new Binding("Value", BindingMode.TwoWay)
                    {
                        Converter = converter
                    }
                };
            }
            if (row?.Type.IsNumeric() ?? false)
            {
                var rangeAttr = (row.Attributes.FirstOrDefault(x => x is ValueRangeAttribute) as ValueRangeAttribute);
                var min = rangeAttr?.Minimum ?? row.Type.GetField("MinValue")?.GetValue(null);
                var max = rangeAttr?.Maximum ?? row.Type.GetField("MaxValue")?.GetValue(null);
                return new NumberBox()
                {
                    Minimum = Convert.ToDouble(min ?? double.MinValue),
                    Maximum = Convert.ToDouble(max ?? double.MaxValue),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
                    NumberFormatter = (input) =>
                    {
                        if (row.Type != typeof(float) && row.Type != typeof(double))
                        {
                            return Math.Floor(input).ToString(CultureInfo.InvariantCulture);
                        }
                        return input.ToString(CultureInfo.InvariantCulture);
                    },
                    [!NumberBox.ValueProperty] = new Binding("Value", BindingMode.TwoWay)
                    {
                        Converter = new FuncValueDynamicConverter<dynamic,double>
                            (o => Convert.ToDouble(o), d => row.Type.CastToNumberType(d))
                    }
                };
            }
            
            return new TextBlock()
            {
                Background = new SolidColorBrush(Colors.Red),
                [!TextBlock.TextProperty] = new Binding("Value", BindingMode.TwoWay)
            };
        }

        private IControl BuildPreviewControl(object itemModel, INameScope _)
        {
            var row = (itemModel as PropertyRow);
            
            Console.WriteLine($"ReactiveEffectPage.BuildPreviewControl: {row?.Name} ({row?.Type})");

            if (row?.Type == typeof(CRGBPalette16))
            {
                return new PaletteViewControl()
                {
                    Margin = new Thickness(8, 0),
                    Colors = ((CRGBPalette16?)row.Value)?.Entries ?? Array.Empty<Color>()
                };
            }  
            if (row?.Type == typeof(Color))
            {
                return new ColorBlockControl()
                {
                    Margin = new Thickness(8, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    CornerRadius = 3.0f,
                    Height = 16,
                    Width = 16,
                    Color = (Color)(row.Value ?? Color.Black)
                };
            }
            return new TextBlock()
            {
                Margin = new Thickness(8,0),
                VerticalAlignment = VerticalAlignment.Center,
                Text = row == null ? string.Empty : ConvertToString(row),
            };
        }

        private string ConvertToString(PropertyRow row)
        {
            if (row.Type.IsNumeric())
            {
                return row.Value?.ToString() ?? string.Empty;
            }
            
            if (row.Value?.GetType().IsEnum ?? false)
            {
                return row.Value.GetType().GetEnumNames()[(int)row.Value];
            }

            if (row.Type == typeof(FftCBinSelector))
            {
                return row.Value == null ? "Disabled" : ((FftCBinSelector)row.Value).ToString();
            }
            
            switch (row.Value)
            {
                case bool o:
                    return o ? "True" : "False";
                case FixedVolumeAnalysisOption:
                case AgcVolumeAnalysisOption:
                    return row.Value.ToString() ?? "???";
                default:
                {
                    return $"{row.Value} (Unsupported type)";
                }
            }
        }

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ReactiveEffectPageViewModel.Assignments))
            {
                if (DataContext is ReactiveEffectPageViewModel vm)
                {
                    SettingsProvider.Instance.ReactiveEffectAssignments = vm.Assignments.ToArray();
                    SettingsProvider.Save();
                }
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void PropertyGrid_OnCellEditEnded(object? sender, DataGridCellEditEndedEventArgs e)
        {     
            if (DataContext is ReactiveEffectPageViewModel vm)
            {
                SettingsProvider.Instance.ReactiveEffectAssignments = vm.Assignments.ToArray();
                SettingsProvider.Save();
                
                Console.WriteLine("PropertyGrid_OnCellEditEnded ------------------- " + Random.Shared.Next());
                if (vm.SelectedAssignment != null)
                {
                    RemoteStripManager.Instance.UpdateEffectProperties(vm.SelectedAssignment);
                }
            }
        }
    }
}
