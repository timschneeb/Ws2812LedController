using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Color = Avalonia.Media.Color;

namespace Ws2812LedController.Simulator
{
    public class LedStripControl : Control
    {
        static LedStripControl()
        {
            AffectsRender<LedStripControl>(ColorsProperty);
            AffectsRender<LedStripControl>(PixelSizeProperty);
        }

        public LedStripControl()
        {
            PixelSizeProperty.Changed.Subscribe(OnPixelSizeChanged);
        }

        private void OnPixelSizeChanged(AvaloniaPropertyChangedEventArgs<int> obj)
        {
            MinWidth = obj.NewValue.Value * (Colors?.Length ?? 0);
            MinHeight = obj.NewValue.Value;
        }

        public static readonly StyledProperty<System.Drawing.Color[]?> ColorsProperty =
            AvaloniaProperty.Register<LedStripControl, System.Drawing.Color[]?>(nameof(Colors));        
        
        public static readonly StyledProperty<int> PixelSizeProperty =
            AvaloniaProperty.Register<LedStripControl, int>(nameof(PixelSize), 24);        

        public System.Drawing.Color[]? Colors
        {
            get => GetValue(ColorsProperty);
            set => SetValue(ColorsProperty, value);
        }

        public int PixelSize
        {
            get => GetValue(PixelSizeProperty);
            set => SetValue(PixelSizeProperty, value);
        }
        
        public override void Render(DrawingContext drawingContext)
        {
            for (var i = 0; i < (Colors?.Length ?? 0); i++)
            {
                var color = Color.FromArgb(Colors![i].A, Colors[i].R, Colors[i].G, Colors[i].B);
                var brush = new SolidColorBrush(color);
                var rect = new Rect(i * PixelSize, 0, PixelSize, PixelSize);
                drawingContext.FillRectangle(brush, rect, 6.0f);
            }
            GC.Collect();
        }
    }
}