using Ws2812LedController.Core.CancellationMethod;

namespace Ws2812LedController.WebApi.Serializable;

public class CancellationMethodData
{
    public Type CancelType { set; get; } = Type.Token;
    public long? Parameter { set; get; }

    public enum Type
    {
        Token,
        Cycle,
        Frame,
        Timeout,
        Once
    }
    
    public BaseCancellationMethod? Inflate()
    {
        try
        {
            return CancelType switch
            {
                Type.Token => new CancellationTokenMethod(),
                Type.Cycle => new CancellationCycleMethod(Convert.ToInt32(Parameter)),
                Type.Frame => new CancellationFrameMethod(Convert.ToInt32(Parameter)),
                Type.Timeout => new CancellationTimeoutMethod(Convert.ToInt32(Parameter)),
                Type.Once => new CancellationOnceMethod(),
                _ => null
            };
        }
        catch (Exception) /* catch conversion exceptions */
        {
            return null;
        }
    }
}

public static class CancellationMethodExtensions
{
    public static CancellationMethodData Deflate(this BaseCancellationMethod method)
    {
        CancellationMethodData.Type cancelType;
        long? parameter = null;
        switch (method.GetType().Name)
        {
            case "CancellationCycleMethod":
                cancelType = CancellationMethodData.Type.Cycle;
                parameter = ((CancellationCycleMethod)method).CycleLimit;
                break;
            case "CancellationFrameMethod":
                cancelType = CancellationMethodData.Type.Frame;
                parameter = ((CancellationFrameMethod)method).FrameLimit;
                break;
            case "CancellationTimeoutMethod":
                cancelType = CancellationMethodData.Type.Timeout;
                parameter = ((CancellationTimeoutMethod)method).Timeout;
                break;
            case "CancellationOnceMethod":
                cancelType = CancellationMethodData.Type.Once;
                break;
            default:
                cancelType = CancellationMethodData.Type.Token;
                break;
        }

        return new CancellationMethodData
        {
            CancelType = cancelType,
            Parameter = parameter
        };
    }
}