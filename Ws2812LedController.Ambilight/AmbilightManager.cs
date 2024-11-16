using System.Drawing;

namespace Ws2812LedController.Ambilight;

public class AmbilightManager
{
    public AmbilightManager()
    {
        LinearSmoothingOptions.PropertyChanged += (_, _) => UpdateSmoothingOptions();
        
        _processor.DataReady += ProcessorOnDataReady;
    }
    
    public LinearSmoothingOptions LinearSmoothingOptions
    {
        set
        {
            _linearSmoothingOptions = value;
            UpdateSmoothingOptions();
        }
        get => _linearSmoothingOptions;
    }

    private readonly ImageProcessingUnit _processor = new();
    private readonly Dictionary<string, ZoneHost> _zoneData = new();
    private LinearSmoothingOptions _linearSmoothingOptions = new();
    
    public void RegisterZone(LedZone zone, Action<IReadOnlyList<Color>> onDataReady)
    {
        if (_processor.RemoveZone(zone.Name) > 0)
        {
            Console.WriteLine($"AmbilightStripManager.RegisterZone: Existing zone '{zone.Name}' overwritten");
        }
        _processor.AddZone(zone);
        
        _zoneData[zone.Name] = new ZoneHost(onDataReady, _linearSmoothingOptions);
    }

    public void UnregisterZone(LedZone zone)
    {
        _processor.RemoveZone(zone.Name);
        _zoneData.Remove(zone.Name);
    }

    private void UpdateSmoothingOptions()
    {
        foreach (var data in _zoneData)
        {
            data.Value.Update(LinearSmoothingOptions);
        }
    }
    
    private void ProcessorOnDataReady(LedZone zone, Color[] colors1)
    {
        if (!_zoneData[zone.Name].LinearSmoothing.Enabled)
        {
            Notify(zone, colors1);
        }
        else
        {
            _zoneData[zone.Name].LinearSmoothing.PushColors(colors1.ToList());
        }
    }
    
    private void Notify(LedZone zone, IReadOnlyList<Color> colors)
    {
        if (!_zoneData.ContainsKey(zone.Name))
        {
            Console.WriteLine($"AmbilightStripManager.Notify: Callback for zone '{zone.Name}' not found");
            return;
        }
        _zoneData[zone.Name].DataReady.Invoke(colors);
    }
    
    private class ZoneHost
    {
        public ZoneHost(Action<IReadOnlyList<Color>> onDataReady, LinearSmoothingOptions smoothingOptions)
        {
            DataReady = onDataReady;
            Update(smoothingOptions);
            
            LinearSmoothing.DataReady += DataReady;
        }
        
        public Action<IReadOnlyList<Color>> DataReady { get; }
        public LinearSmoothing LinearSmoothing { get; } = new();
        
        public void Update(LinearSmoothingOptions smoothingOptions)
        {
            LinearSmoothing.ApplySettings(smoothingOptions);
        }
    }
}