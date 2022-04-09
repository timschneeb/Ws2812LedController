using FftSharp;
using Ws2812LedController.Core.Utils;

namespace Ws2812AudioReactiveClient.Dsp;

public static class FftExtensions
{
    public static double FftAdd(this double[] buffer, int from, int to) {
        var i = from;
        double result = 0;
        while ( i <= to) {
            result += buffer[i++];
        }
        return result;
    }
    
    public static double FftMean(this double[] buffer, int from, int to)
    {
        var add = FftAdd(buffer, from, to);
        return add / (to - from + 1);
    }  
    
    public static double Mean(this double[] buffer)
    {
        if (buffer.Length < 1)
        {
            return 0;
        }
        
        var add = 0.0;
        foreach (var t in buffer)
        {
            add += t;
        }

        return add / (buffer.Length);
    }
    
    public static double[] FftMeanWithFreq(this double[] buffer, int from, int to, double[]? freq = null)
    {
        var ret = new double[3];
        freq ??= Transform.FFTfreq(48000, buffer.Length);
        var add = FftAdd(buffer, from, to);
        ret[0] = add / (to - from + 1);
        ret[1] = freq[from];
        ret[2] = freq[to];
        return ret;
    }
    
    public static double? MajorPeak(this double[] vD, int samples, double samplingFrequency)
    {
        double maxY = 0;
        
        ushort indexOfMaxY = 0;
        //If sampling_frequency = 2 * max_frequency in signal,
        //value would be stored at position samples/2
        for (ushort i = 1; i < ((samples >> 1) /*+ 1*/); i++)
        {
            if ((vD[i - 1] < vD[i]) && (vD[i] > vD[i + 1]))
            {
                if (vD[i] > maxY)
                {
                    maxY = vD[i];
                    indexOfMaxY = i;
                }
            }
        }

        if (indexOfMaxY < 1)
        {
            return null;
        }
        
        var delta = 0.5 * ((vD[indexOfMaxY - 1] - vD[indexOfMaxY + 1]) / (vD[indexOfMaxY - 1] - (2.0 * vD[indexOfMaxY]) + vD[indexOfMaxY + 1]));
        var interpolatedX = ((indexOfMaxY + delta) * samplingFrequency) / (samples - 1);
        if (indexOfMaxY == (samples >> 1)) //To improve calculation on edge values
        {
            interpolatedX = ((indexOfMaxY + delta) * samplingFrequency) / (samples);
        }
        // returned value: interpolated frequency peak apex
        return (interpolatedX);
    }


    public static bool MajorPeak(this double[] vD, int samples, double samplingFrequency, ref double f, ref double v)
    {
        double maxY = 0;
        ushort indexOfMaxY = 0;
        //If sampling_frequency = 2 * max_frequency in signal,
        //value would be stored at position samples/2
        for (ushort i = 1; i < ((samples >> 1) + 1); i++)
        {
            if ((vD[i - 1] < vD[i]) && (vD[i] > vD[i + 1]))
            {
                if (vD[i] > maxY)
                {
                    maxY = vD[i];
                    indexOfMaxY = i;
                }
            }
        }
        
        if (indexOfMaxY < 1)
        {
            return false;
        }
        
        var delta = 0.5 * ((vD[indexOfMaxY - 1] - vD[indexOfMaxY + 1]) / (vD[indexOfMaxY - 1] - (2.0 * vD[indexOfMaxY]) + vD[indexOfMaxY + 1]));
        var interpolatedX = ((indexOfMaxY + delta) * samplingFrequency) / (samples - 1);
        if (indexOfMaxY == (samples >> 1)) //To improve calculation on edge values
        {
            interpolatedX = ((indexOfMaxY + delta) * samplingFrequency) / (samples);
        }
        // returned value: interpolated frequency peak apex
        f = interpolatedX;
        v = Math.Abs(vD[indexOfMaxY - 1] - (2.0 * vD[indexOfMaxY]) + vD[indexOfMaxY + 1]);

        return true;
    }
}