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

public class PresetManager
{
    private static readonly Lazy<PresetManager> Lazy =
        new(() => new PresetManager());
    public static PresetManager Instance => Lazy.Value;
    
    public AvaloniaList<PresetEntry> PresetEntries { get; }

    public PresetManager()
    {
        var saved = SettingsProvider.Instance.Presets;
        PresetEntries = saved == null ? new AvaloniaList<PresetEntry>() : new AvaloniaList<PresetEntry>(saved);
    }
    
    #region Preset management
    public void AddOrUpdatePreset(PresetEntry entry, string? originalName)
    {
        var oldEntry = PresetEntries.FirstOrDefault(x => x.Name == originalName);
        var oldEntryIdx = oldEntry == null ? -1 : PresetEntries.IndexOf(oldEntry);
        
        if (oldEntryIdx == -1)
        {
            PresetEntries.Add(entry);
            SettingsProvider.Instance.Presets = PresetEntries.ToArray();
            SettingsProvider.Save();
            return;
        }
        
        PresetEntries[oldEntryIdx] = entry;
        SettingsProvider.Instance.Presets = PresetEntries.ToArray();
        SettingsProvider.Save();
    }
    
    public void DeletePreset(string name)
    {
        PresetEntries.Where(x => x.Name == name).ToList()
            .ForEach(x => PresetEntries.Remove(x));
        SettingsProvider.Instance.Presets = PresetEntries.ToArray();
        SettingsProvider.Save();
    }
    #endregion
}