using Microsoft.AspNetCore.Mvc;
using Ws2812LedController.Core;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;

namespace Ws2812LedController.HueApi;

public static class Extensions
{
    public static LedSegmentController? FromSegmentName(this Ref<LedManager> mgr, string segmentName)
    {
        if (segmentName == "full")
        {
            return mgr.Value.GetFull();
        }
        
        return mgr.Value.Segments.FirstOrDefault(x => segmentName == x.Name);
    }
    
    public static EffectProperty? FromPropertyName(this LedSegmentController segment, string propName, LayerId layer)
    {
        var desc = EffectDescriptorList.Instance.Create(segment.CurrentEffects[(int)layer]);
        return desc?.Properties.FirstOrDefault(x => x.Name == propName);
    }
}