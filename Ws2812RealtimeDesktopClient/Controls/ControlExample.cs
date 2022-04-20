using System.Diagnostics;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Indentation.CSharp;
using FluentAvalonia.Core;
using FluentAvalonia.Styling;
using FluentAvalonia.UI.Controls;
using Ws2812RealtimeDesktopClient.Services;
using Button = FluentAvalonia.UI.Controls.Button;

namespace Ws2812RealtimeDesktopClient.Controls
{
    public class ControlExample : HeaderedContentControl
    {
        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);

           if (change.Property == BoundsProperty)
            {
                var wid = change.NewValue.GetValueOrDefault<Rect>().Width;

                PseudoClasses.Set(":adaptiveW", wid < 725);
                PseudoClasses.Set(":small", wid < 500);
            }
        }
    }
}
