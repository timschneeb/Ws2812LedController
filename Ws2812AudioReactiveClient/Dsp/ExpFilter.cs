namespace Ws2812AudioReactiveClient.Dsp;

public class ExpFilter
{
    public double Weighting { set; get; }
    public double Squelch { set; get; }
    public double Value { set; get; }

    public ExpFilter(double weighting = 0.2, double squelch = 0.005)
    {
        Weighting = weighting;
        Squelch = squelch;
    }

    public double Process(double sample)
    {
        var temp = (Weighting * sample + (1.0-Weighting) * Value);
        Value = (temp <= Squelch) ? 0: temp;
        return Value;
    } 
    
    public void Process(ref double[] sample)
    {
        for (var index = 0; index < sample.Length; index++)
        {
            var frame = sample[index];
            var temp = (Weighting * frame + (1.0 - Weighting) * Value);
            Value = (temp <= Squelch) ? 0 : temp;
            
            sample[index] = Value;
        }
    }
}