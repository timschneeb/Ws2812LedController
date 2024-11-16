using Ws2812LedController.Core;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Ambilight.Effects;

[FriendlyName("Base ambilight effect")]
public abstract class BaseAmbilightEffect : BaseEffect, IDisposable
{
    public override int Speed { get; set; } = 1000 / 60;
    
    private readonly ImageProcessingUnit _processingUnit = new();

    public void Dispose()
    {
        _processingUnit.Dispose();
    }
}