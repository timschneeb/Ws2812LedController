namespace Ws2812LedController.Core.Model.Serializable;

public class LayerData
{
    public LayerData(LedSegmentController ctrl, LayerId id)
    {
        Name = id;
        Priority = (int)id;
        CurrentEffect = ctrl.CurrentEffects[(int)id]?.GetType().Name;
    }

    public LayerId Name { set; get; }
    public int Priority { set; get; }
    public string? CurrentEffect { set; get; }
}
