using System.Runtime.Serialization;

namespace Ws2812LedController.AudioReactive.Model;

[Serializable]
public class AgcVolumeAnalysisOption : IVolumeAnalysisOption, ISerializable
{
    public AgcVolumeAnalysisOption(byte intensity = 64)
    {
        Intensity = intensity;
    }

    public byte Intensity { set; get; }
    
    public AgcVolumeAnalysisOption(SerializationInfo info, StreamingContext context)
    {
        Intensity = (byte)(info.GetValue(nameof(Intensity), typeof(byte)) ?? 64);
    }
            
    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue(nameof(Intensity), Intensity);
    }
}

[Serializable]
public class FixedVolumeAnalysisOption : IVolumeAnalysisOption, ISerializable
{
    public FixedVolumeAnalysisOption(int minimumMagnitude = 1000, int maximumMagnitude = 8000)
    {
        MinimumMagnitude = minimumMagnitude;
        MaximumMagnitude = maximumMagnitude;
    }

    public int MinimumMagnitude { set; get; }
    public int MaximumMagnitude { set; get; }
    
    public FixedVolumeAnalysisOption(SerializationInfo info, StreamingContext context)
    {
        MinimumMagnitude = (int)(info.GetValue(nameof(MinimumMagnitude), typeof(int)) ?? 1000);
        MaximumMagnitude = (int)(info.GetValue(nameof(MaximumMagnitude), typeof(int)) ?? 8000);
    }
            
    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue(nameof(MinimumMagnitude), MinimumMagnitude);
        info.AddValue(nameof(MaximumMagnitude), MaximumMagnitude);
    }
}

public interface IVolumeAnalysisOption
{
    
}