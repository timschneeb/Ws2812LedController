using Ws2812LedController.AudioReactive.Effects.Base;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;

namespace Ws2812RealtimeDesktopClient.Models;

public class ReactiveEffectDescriptorList
{
    private static readonly Lazy<ReactiveEffectDescriptorList> Lazy =
        new(() => new ReactiveEffectDescriptorList());

    public static ReactiveEffectDescriptorList Instance => Lazy.Value;
    
    public EffectDescriptor[] Descriptors { get; }

    private ReactiveEffectDescriptorList()
    {
        var descs = EffectDescriptorList.Enumerate(typeof(BaseAudioReactiveEffect).Assembly, "Ws2812LedController.AudioReactive.Effects");
        foreach (var desc in descs)
        {
            desc.EffectType = EffectType.AudioReactive;
        }

        Descriptors = descs;
    }
}