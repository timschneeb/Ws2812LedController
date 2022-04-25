using System.Drawing;
using Ws2812RealtimeDesktopClient.ViewModels;

namespace Ws2812RealtimeDesktopClient.Models;

public class PaletteEntry : ViewModelBase
{
    public PaletteEntry(string name, Color[] paletteColors)
    {
        Name = name;
        PaletteColors = paletteColors;
    }

    public string Name { set; get; }
    public Color[] PaletteColors { set; get; }
    
    public void UpdateFromViewModel()
    {
        RaisePropertyChanged(nameof(Name));
        RaisePropertyChanged(nameof(PaletteColors));
    }
}