using System.Drawing;
using FluentAvalonia.UI.Controls;
using Ws2812RealtimeDesktopClient.Models;

namespace Ws2812RealtimeDesktopClient.ViewModels
{
    public class EditPaletteDialogViewModel : ViewModelBase
    {
        private readonly ContentDialog _dialog;
        public EditPaletteDialogViewModel(ContentDialog dialog, PaletteEntry? palette)
        {
            _dialog = dialog;

            Name = palette?.Name ?? string.Empty;
            
            var temp = palette?.PaletteColors ?? Array.Empty<Color>();
            
            PaletteColors = new Color[16];
            Array.Fill(PaletteColors, Color.FromArgb(0,0,0,0));
            Array.Copy(temp, 0, PaletteColors, 0, Math.Min(temp.Length, 16));
        }

        public string Name { set; get; }
        public Color[] PaletteColors { set; get; }

        public PaletteEntry ApplyTo(PaletteEntry? oldEntry)
        {
            var entry = oldEntry ?? new PaletteEntry(Name, PaletteColors);
            entry.Name = Name;
            entry.PaletteColors = PaletteColors;
            return entry;
        }
    }
}
