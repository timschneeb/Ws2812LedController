using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Ws2812RealtimeDesktopClient.Controls
{
    public class ColorBlockControl : Control
    {
        static ColorBlockControl()
        {
            AffectsRender<ColorBlockControl>(ColorsProperty);
            AffectsRender<ColorBlockControl>(CornerRadiusProperty);
        }
        
        public static readonly StyledProperty<System.Drawing.Color> ColorsProperty =
            AvaloniaProperty.Register<ColorBlockControl, System.Drawing.Color>(nameof(Color));        
        public static readonly StyledProperty<float> CornerRadiusProperty =
            AvaloniaProperty.Register<ColorBlockControl, float>(nameof(CornerRadius), 6.0f);
        
        public System.Drawing.Color Color
        {
            get => GetValue(ColorsProperty);
            set
            {
                SetValue(ColorsProperty, value);
                _brush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(Color.A, Color.R, Color.G, Color.B));
            }
        }

        public float CornerRadius
        {
            get => GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        private IBrush _brush = new SolidColorBrush();
        
        public override void Render(DrawingContext drawingContext)
        {
            var rect = new Rect(0, 0, Width, Height);
            drawingContext.FillRectangle(_brush, rect, CornerRadius);
        }
    }
}