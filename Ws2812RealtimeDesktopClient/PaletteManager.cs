using System.Diagnostics;
using Avalonia.Collections;
using Avalonia.Media;
using Ws2812LedController.Core;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.FastLedCompatibility;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;
using Ws2812LedController.UdpServer;
using Ws2812LedController.UdpServer.Packets;
using Ws2812RealtimeDesktopClient.Models;
using Ws2812RealtimeDesktopClient.Utilities;
using Color = System.Drawing.Color;

namespace Ws2812RealtimeDesktopClient;

public class PaletteManager
{
    private static readonly Lazy<PaletteManager> Lazy =
        new(() => new PaletteManager());
    public static PaletteManager Instance => Lazy.Value;
    
    public AvaloniaList<PaletteEntry> PaletteEntries { get; }

    public PaletteManager()
    {
        var saved = SettingsProvider.Instance.Palettes;
        if (saved == null)
        {
            PaletteEntries = new AvaloniaList<PaletteEntry>();
            InstallDefaultPalettes(true);
            SettingsProvider.Instance.Palettes = PaletteEntries.ToArray();
            SettingsProvider.Save();
        }
        else
        {
            PaletteEntries = new AvaloniaList<PaletteEntry>(saved);
        }
    }
    
    #region Palette management
    public void InstallDefaultPalettes(bool allowOverride)
    {
        var predefNames = typeof(CRGBPalette16.Palette).GetEnumNames();
        for(var i = 0; i < predefNames.Length; i++)
        {
            var name = predefNames[i];
            if (PaletteEntries.Any(x => x.Name == name))
            {
                if (!allowOverride)
                {
                    continue;
                }

                PaletteEntries.First(x => x.Name == name).PaletteColors =
                    new CRGBPalette16((CRGBPalette16.Palette)i).Entries;
            }
            else
            {
                PaletteEntries.Add(new PaletteEntry(name, new CRGBPalette16((CRGBPalette16.Palette)i).Entries));
            }
        }
    }

    public void AddOrUpdatePalette(PaletteEntry entry, string? originalName)
    {
        var oldEntry = PaletteEntries.FirstOrDefault(x => x.Name == originalName);
        var oldEntryIdx = oldEntry == null ? -1 : PaletteEntries.IndexOf(oldEntry);
        
        if (oldEntryIdx == -1)
        {
            PaletteEntries.Add(entry);
            SettingsProvider.Instance.Palettes = PaletteEntries.ToArray();
            SettingsProvider.Save();
            return;
        }
        
        PaletteEntries[oldEntryIdx] = entry;
        SettingsProvider.Instance.Palettes = PaletteEntries.ToArray();
        SettingsProvider.Save();
    }
    
    public void DeletePalette(string name)
    {
        PaletteEntries.Where(x => x.Name == name).ToList()
            .ForEach(x => PaletteEntries.Remove(x));
        SettingsProvider.Instance.Palettes = PaletteEntries.ToArray();
        SettingsProvider.Save();
    }
    #endregion
}