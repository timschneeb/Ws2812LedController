using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;

namespace Ws2812LedController.Core;

public class LedManager
{
    private readonly List<LedSegmentController> _segments = new();
    public ReadOnlyCollection<LedSegmentController> Segments => _segments.AsReadOnly();

    public void RegisterSegment(string name, LedSegment segment)
    {
        Debug.Assert(_segments.All(x => x.Name != name), "Segment name already registered");
        
        _segments.Add(new LedSegmentController(name, segment));
    }
    
    public void RegisterSegment(string name, LedStrip strip, int start, int width)
    {
        RegisterSegment(name, strip.CreateSegment(start, width));
    }
    
    public async Task<bool> UnregisterSegmentAsync(string name, LedStrip? strip)
    {
        foreach (var ctrl in _segments.FindAll(x => x.Name == name))
        {
            await ctrl.TerminateLoopAsync(1000);
            ctrl.SegmentGroup.FillAllLayers(Color.FromArgb(0,0,0,0));
            strip?.RemoveSegment(ctrl.SourceSegment);
            ctrl.Dispose();
        }
        
        return _segments.RemoveAll(x => x.Name == name) > 0;
    }
    
    public async Task UnregisterAllSegmentsAsync(LedStrip? strip)
    {
        foreach (var ctrl in _segments)
        {
            await ctrl.TerminateLoopAsync(1000);
            ctrl.SegmentGroup.FillAllLayers(Color.FromArgb(0,0,0,0));
            strip?.RemoveSegment(ctrl.SourceSegment);
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

    public Task PowerAllAsync(bool state, params LedSegmentController[] ctrls)
    {
        return Task.WhenAll((from seg in ctrls select seg.PowerAsync(state)).ToList());
    }
    
    public Task PowerAllAsync(bool state, params string[] names)
    {
        return Task.WhenAll(names.Select(name => Get(name)?.PowerAsync(state)).Where(task => task != null).Cast<Task>().ToList());
    }

    public void MirrorTo(string source, string target)
    {
        var targetCtrl = _segments.FirstOrDefault(x => x.Name == target);
        var sourceCtrl = _segments.FirstOrDefault(x => x.Name == source);
            
        Debug.Assert(targetCtrl != null && sourceCtrl != null, "Source/target not found");
        
        sourceCtrl.MirrorTo(targetCtrl);
    }

    public void RemoveAllMirrors()
    {
        foreach (var segment in _segments)
        {
            segment.RemoveAllMirrors();
        }
    }
}