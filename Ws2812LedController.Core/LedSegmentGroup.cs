using System.Diagnostics;
using System.Drawing;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core;

/* Contains one or more LED segments of the same width */
public class LedSegmentGroup
{
    /* Absolute end index */
    public int RelEnd => Width - 1;
    public int Width { private set; get; }
    public List<LedSegment> Segments => _segments;
    public LedSegment MasterSegment => _segments[0];

    public List<LedSegment> MirroredSegments => 
        _segments.Count <= 1 ? new List<LedSegment>() : _segments.GetRange(1, _segments.Count);

    private readonly List<LedSegment> _segments = new();

    public LedSegmentGroup(LedSegment master, LedSegment[]? mirrored = null)
    {
        _segments.Add(master);
        _segments.AddRange((mirrored ?? Array.Empty<LedSegment>()).ToList());
        Width = master.Width;
    }

    public void AddMirror(LedSegment segment)
    {
        Debug.Assert(segment.Width >= Width, "LedSegmentGroup.AddMirror: Target and source segment have different widths");
        Debug.Assert(_segments.All(x => x.Id != segment.Id), "Segment already added as a mirror target");
        _segments.Add(segment);
    }

    public bool RemoveMirror(LedSegment segment)
    {
        Debug.Assert(_segments[0] == segment, "Cannot remove master segment");
        return _segments.RemoveAll(x => x == segment) > 0;
    }
    
    public void RemoveAllMirrors()
    {
        if (_segments.Count <= 1)
        {
            return;
        }
        _segments.RemoveRange(1, _segments.Count);
    }
    
    public void SetPixel(int i, Color color, LayerId layer )//= LayerId.BaseLayer)
    {
        foreach (var segment in Segments)
        {
            segment.Layers[(int)layer].SetPixel(i, color);
        }
    }

    /* We expect all the segments to carry the same data, so just take the master strip */
    public Color PixelAt(int i, LayerId layer )//= LayerId.BaseLayer)
    {
        return _segments[0].Layers[(int)layer].PixelAt(i);
    }
    
    public void CopyPixels(int destIndex, int srcIndex, int length, LayerId layer )//= LayerId.BaseLayer)
    {
        foreach (var segment in Segments)
        {
            segment.Layers[(int)layer].CopyPixels(destIndex, srcIndex, length);
        }
    }
    
    public void Fill(int start, int length, Color color, LayerId layer )//= LayerId.BaseLayer)
    {
        foreach (var segment in Segments)
        {
            segment.Layers[(int)layer].Fill(start, length, color);
        }
    }
    
    public void Clear(Color? color, LayerId layer)
    {
        foreach (var segment in Segments)
        {
            segment.Layers[(int)layer].Clear(color);
        }
    }
    
    public void Clear(LayerId layer)
    {
        Clear(null, layer);
    }

    public void FillAllLayers(Color color)
    {
        for (var i = 0; i < typeof(LayerId).GetEnumNames().Length; i++)
        {
            Clear(color, (LayerId)i);
        }
    }
    
    /* We expect all the segments to carry the same data, so just take the master strip */
    public byte[] DumpBytes(LayerId layer )//= LayerId.BaseLayer)
    {
        return _segments[0].Layers[(int)layer].DumpBytes();
    }

    public void UpdateBytes(byte[] bytes, LayerId layer )//= LayerId.BaseLayer)
    {
        foreach (var segment in Segments)
        {
            segment.Layers[(int)layer].UpdateBytes(bytes);
        }
    }
}