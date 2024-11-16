using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Ws2812RealtimeDesktopClient.Controls
{
    public class PaletteViewControl : StackPanel
    {
        static PaletteViewControl()
        {
            AffectsRender<PaletteViewControl>(ColorsProperty);
        }

        public PaletteViewControl()
        {
            Spacing = 4;
            Orientation = Orientation.Horizontal;
            VerticalAlignment = VerticalAlignment.Center;

            PropertyChanged += ((_, args) =>
            {
                if (args.Property.Name != nameof(Colors))
                {
                    return;
                }
                
                var o = args.NewValue;
                if (o is not System.Drawing.Color[] val)
                {
                    Children.Clear();
                    return;
                }
                    
                if (val.Length != Children.Count)
                {
                    Children.Clear();
                    foreach (var color in val)
                    {
                        Children.Add(new ColorBlockControl
                        {
                            Color = color,
                            CornerRadius = 3.0f,
                            Height = 16,
                            Width = 16,
                        });
                    }
                }
                else
                {
                    for (var i = 0; i < val.Length; i++)
                    {
                        var color = val[i];
                        if (Children[i] is ColorBlockControl child) child.Color = color;
                    }
                }
            });
        }
        
        public static readonly StyledProperty<System.Drawing.Color[]> ColorsProperty =
            AvaloniaProperty.Register<ColorBlockControl, System.Drawing.Color[]>(nameof(Colors));        
        
        public System.Drawing.Color[] Colors
        {
            get => GetValue(ColorsProperty);
            set => SetValue(ColorsProperty, value);
        }
    }
}