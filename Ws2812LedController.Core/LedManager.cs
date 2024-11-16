using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;

namespace Ws2812LedController.Core;

public class LedManager
{
    private readonly Ref<LedStrip> _strip;
    private readonly List<LedSegmentController> _segments = new();
    private readonly LedSegmentController _full;
    public ReadOnlyCollection<LedSegmentController> Segments => _segments.AsReadOnly();
    public LedStrip Strip => _strip.Value;

    public class PowerStateChangedEventArgs(PowerState state, string segmentName)
    {
        public PowerState State { get; } = state;
        public string SegmentName { get; } = segmentName;
    }

    public event EventHandler<PowerStateChangedEventArgs>? SegmentPowerStateChanged;
    
    public LedManager(Ref<LedStrip> strip)
    {
        _strip = strip;
        _full = new LedSegmentController("full", _strip.Value.FullSegment);
    }
    
    public void RegisterSegment(string name, LedSegment segment)
    {
        Debug.Assert(_segments.All(x => x.Name != name), "Segment name already registered");
        
        var ctrl = new LedSegmentController(name, segment);
        ctrl.PowerStateChanged += (sender, args) => 
            SegmentPowerStateChanged?.Invoke(sender, new PowerStateChangedEventArgs(args, name));
        _segments.Add(ctrl);
    }
    
    public void RegisterSegment(string name, int start, int width)
    {
        RegisterSegment(name, _strip.Value.CreateSegment(start, width));
    }
    
    public async Task<bool> UnregisterSegmentAsync(string name)
    {
        foreach (var ctrl in _segments.FindAll(x => x.Name == name))
        {
            await ctrl.TerminateLoopAsync(1000);
            ctrl.SegmentGroup.FillAllLayers(Color.FromArgb(0,0,0,0));
            _strip.Value.RemoveSegment(ctrl.SourceSegment);
            ctrl.Dispose();
        }
        
        return _segments.RemoveAll(x => x.Name == name) > 0;
    }
    
    public async Task UnregisterAllSegmentsAsync()
    {
        foreach (var ctrl in _segments)
        {
            await ctrl.TerminateLoopAsync(1000);
            ctrl.SegmentGroup.FillAllLayers(Color.FromArgb(0,0,0,0));
            _strip.Value.RemoveSegment(ctrl.SourceSegment);
            ctrl.Dispose();
        }
        
        _segments.Clear();
    }

    public bool RenameSegment(string oldName, string newName)
    {
        var ctrl = Get(oldName);
        if (ctrl == null)
        {
            return false;
        }
        ctrl.Name = newName;
        return true;
    }
    
    public LedSegmentController? Get(string name)
    {
        return _segments.FirstOrDefault(x => x?.Name == name, null);
    }
    
    public LedSegmentController GetFull()
    {
        return _full;
    }

    public bool IsPowered() => _segments.Any(x => x.IsPowered);
    
    public Task PowerAllAsync(bool state)
    {
        return Task.WhenAll((from seg in _segments select seg.PowerAsync(state)).ToList());
    }
    
    public Task PowerAllAsync(bool state, params LedSegmentController[] ctrls)
    {
        return Task.WhenAll((from seg in ctrls select seg.PowerAsync(state)).ToList());
    }
    
    public Task PowerAllAsync(bool state, params string[] names)
    {
        return Task.WhenAll(names.Select(name => Get(name)?.PowerAsync(state)).Where(task => task != null).Cast<Task>().ToList());
    }

    public bool MirrorTo(string source, string target)
    {
        var targetCtrl = _segments.FirstOrDefault(x => x.Name == target);
        var sourceCtrl = _segments.FirstOrDefault(x => x.Name == source);

        if (targetCtrl == null || sourceCtrl == null)
        {
            return false; 
        }

        sourceCtrl.MirrorTo(targetCtrl);
        return true;
    }

    public void RemoveAllMirrors()
    {
        foreach (var segment in _segments)
        {
            segment.RemoveAllMirrors();
        }
    }
}