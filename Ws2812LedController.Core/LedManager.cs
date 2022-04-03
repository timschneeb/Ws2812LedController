using System.Collections.ObjectModel;
using System.Diagnostics;

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
    
    public bool UnregisterSegment(string name)
    {
        foreach (var ctrl in _segments.FindAll(x => x.Name == name))
        {
            ctrl.Dispose();
        }
        
        return _segments.RemoveAll(x => x.Name == name) > 0;
    }

    public LedSegmentController? Get(string name)
    {
        return _segments.FirstOrDefault(x => x?.Name == name, null);
    }

    public void MirrorTo(string source, string target)
    {
        var targetCtrl = _segments.FirstOrDefault(x => x.Name == target);
        var sourceCtrl = _segments.FirstOrDefault(x => x.Name == source);
            
        Debug.Assert(targetCtrl != null && sourceCtrl != null);
        
        sourceCtrl.MirrorTo(targetCtrl);
    }
}