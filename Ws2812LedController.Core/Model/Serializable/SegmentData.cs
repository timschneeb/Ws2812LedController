using Ws2812LedController.Core.Utils;

namespace Ws2812LedController.Core.Model.Serializable;

public class SegmentData
{
    public SegmentData(LedSegmentController segment, Ref<LedManager> mgr)
    {
        Name = segment.Name;
        Start = segment.SourceSegment.AbsStart;
        Width = segment.SourceSegment.Width;
        UseGammaCorrection = segment.SourceSegment.UseGammaCorrection;
        Brightness = segment.SourceSegment.MaxBrightness;
        InvertX = segment.SourceSegment.InvertX;
        PowerState = segment.CurrentState;

        var mirroredTo = new List<string>();
        foreach (var mirrored in segment.SegmentGroup.MirroredSegments)
        {
            var ctrl = mgr.Value.Segments
                .FirstOrDefault(x => x.SourceSegment.Id == mirrored.Id);
            if (ctrl == null)
            {
                return;
            }
            mirroredTo.Add(ctrl.Name);
        }
        MirroredTo = mirroredTo.ToArray();
        
        Layers = new LayerData[segment.SourceSegment.Layers.Length];
        for (var i = 0; i < segment.SourceSegment.Layers.Length; i++)
        {
            Layers[i] = new LayerData(segment, (LayerId)i);
        }
    }

    public string Name { set; get; }
    public int Start { set; get; }
    public int Width { set; get; }
    public bool UseGammaCorrection { set; get; }
    public byte Brightness { set; get; }
    public bool InvertX { set; get; }
    public PowerState PowerState { set; get; }
    public string[] MirroredTo { set; get; }
    public LayerData[] Layers { set; get; }
}
