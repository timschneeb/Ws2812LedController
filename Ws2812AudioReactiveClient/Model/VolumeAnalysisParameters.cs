namespace Ws2812AudioReactiveClient.Model;

public class AgcVolumeAnalysisOption : IVolumeAnalysisOption
{
    public AgcVolumeAnalysisOption(byte intensity = 64)
    {
        Intensity = intensity;
    }

    public byte Intensity { set; get; }
}


public class FixedVolumeAnalysisOption : IVolumeAnalysisOption
{
    public FixedVolumeAnalysisOption(int minimumMagnitude = 1000, int maximumMagnitude = 8000)
    {
        MinimumMagnitude = minimumMagnitude;
        MaximumMagnitude = maximumMagnitude;
    }

    public int MinimumMagnitude { set; get; }
    public int MaximumMagnitude { set; get; }
}

public interface IVolumeAnalysisOption
{
    
}