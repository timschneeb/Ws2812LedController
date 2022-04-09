using System.Collections.Concurrent;
using System.Diagnostics;
using Aubio;
using Aubio.Spectral;
using FftSharp;
using NWaves.FeatureExtractors;
using NWaves.FeatureExtractors.Base;
using NWaves.FeatureExtractors.Multi;
using NWaves.FeatureExtractors.Options;
using NWaves.Filters;
using NWaves.Filters.Fda;
using NWaves.Signals.Builders;
using Ws2812AudioReactiveClient.Dsp;
using Ws2812LedController.Core.Effects.Base;

namespace Ws2812AudioReactiveClient.Effects;

public abstract class BaseAudioReactiveEffect : IEffect
{
    protected readonly ConcurrentQueue<double[][]> SamplesQueue = new();
    public override int Speed { set; get; } = 1000 / 60;
    public virtual int MinVolume { set; get; } = -70;
    public virtual double Multiplier { set; get; } = 1;

    private double[] _nullBuffer = Array.Empty<double>();

    private readonly ExpFilter _expFilter;
    
    protected BaseAudioReactiveEffect()
    {
        _expFilter = new ExpFilter();
    }
    
    protected override void Begin()
    {
        _timeSinceStart.Restart();
        AudioProviderService.Instance.NewSamples += OnNewSamples;
        base.Begin();
    }

    protected override void End()
    {
        _timeSinceStart.Reset();
        AudioProviderService.Instance.NewSamples -= OnNewSamples;
        base.End();
    } 
    
    public override void Reset()
    {
        SampleAvg = 0;
        _sample = 0;
        _micLev = 0;
        Array.Fill(_peakTime, 0);
        _timeSinceStart.Reset();
        base.Reset();
    }

    /* Smoothed peak detection */
    protected readonly Stopwatch _timeSinceStart = new();
    private double _sample; // Current sample.
    private long[] _peakTime = new long[16 + 1];

   /** Volume peak check */
    protected bool IsPeak(double sample, int squelch = 7, int triggerVolume = 11)
    {
        _micLev = ((_micLev * 31) + sample) / 32;                      // Smooth it out over the last 32 samples for automatic centering.
        sample -= _micLev;                                            // Let's center it to 0 now.
        sample = Math.Abs(sample);                                         // And get the absolute value of each sample.
        _sample = sample < 0.02 ? 0 : (_sample + sample) / 2;     // Using a ternary operator, the resultant sample is either 0 or it's a bit smoothed out with the last sample.

        SampleAvg = ((SampleAvg * 31) + _sample) / 32;               // Smooth it out over the last 32 samples.
        
        if (_sample > (SampleAvg + triggerVolume) && _timeSinceStart.ElapsedMilliseconds > (_peakTime[16] + 50))
        {
            _peakTime[16] = _timeSinceStart.ElapsedMilliseconds;
            return true;
        }
        
        return false;
    }

   private double _micLev = 0; // Used to convert returned value to have '0' as minimum.
   //private double _sample = 0; // Used to convert returned value to have '0' as minimum.
    protected double SampleAvg = 0; // Smoothed Average.

    private void Smooth(ref double[] buffer)
    {
        /*for (var index = 0; index < buffer.Length; index++)
        {
            _micLev = ((_micLev * 31) +  buffer[index]) / 32; 
            // Smooth it out over the last 32 samples for automatic centering
            buffer[index] -= _micLev; // Let's center it to 0 now
            buffer[index] = Math.Abs( buffer[index]); // And get the absolute value of each sample
            _sample = buffer[index];
        }
        
        SampleAvg = (SampleAvg * 15 + buffer.FirstOrDefault()) / 16;*/
        if (buffer.Length < 1)
        {
            return;
        }
        var micIn = buffer[0];                                              // Current sample starts with negative values and large values, which is why it's 16 bit signed.
      
  

        
    }
    
    /* FFT calculation */
    protected FftOptions? FftOptions;

    protected readonly double[] FftBins = new double[16];
    protected readonly double[] FftAvg = new double[16];
    
    private readonly double[][] _fftBinBuffer = new double[16][];
    private readonly double[] _fftPinkAdj =
    {
        1,1.17142591661794,1.47884429079972,1.76196329534098,2.17953539006265,2.62126673958848,3.44609031341581,4.48351183007203,6.2246968291675,8.69568659154196,12.1973195560931,17.4190218987397,25.4925190458713,36.0335128075597,51.8826964891306,113.143509248435
    };
    
    private void DoFFT(double[] buffer)
    {
        if (buffer.Length < 1)
        {
            return;
        }
        
        if (FftOptions == null)
        {
            // return;
        }

        // Let's work with u16 values in this area, so I don't need to rewrite existing stuff
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] *= 65536;
        }
        
        var window = new FftSharp.Windows.Hanning();
        buffer = window.Apply(buffer);

        // magnitude (units²) as real numbers
        var fftMag = Transform.FFTmagnitude(buffer);
        var freq = Transform.FFTfreq(48000, fftMag.Length);

        double f = 0, v = 0;
        var found = fftMag.MajorPeak(buffer.Length - 1, 48000, ref f, ref v);
        if (found)
        {
            //Console.WriteLine($"{Math.Round(f,4)}Hz\t\t=\t{Math.Round(v,4)}");
        }
        else
        {
            //Console.WriteLine("No major peak");
        }
        
        _fftBinBuffer[0] = (fftMag.FftMeanWithFreq(1,2,freq));       // 93 - 187 (48000Hz only)
        _fftBinBuffer[1] = (fftMag.FftMeanWithFreq(2,3,freq));       // 187 - 280
        _fftBinBuffer[2] = (fftMag.FftMeanWithFreq(3,5,freq));       // 280 - 467
        _fftBinBuffer[3] = (fftMag.FftMeanWithFreq(5,7,freq));       // 467 - 654
        _fftBinBuffer[4] = (fftMag.FftMeanWithFreq(7,10,freq));      // 654 - 934
        _fftBinBuffer[5] = (fftMag.FftMeanWithFreq(10,14,freq));     // 934 - 1307
        _fftBinBuffer[6] = (fftMag.FftMeanWithFreq(14,19,freq));     // 1307 - 1774
        _fftBinBuffer[7] = (fftMag.FftMeanWithFreq(19,26,freq));     // 1774 - 2428
        _fftBinBuffer[8] = (fftMag.FftMeanWithFreq(26,35,freq));     // 2428 - 3268
        _fftBinBuffer[9] = (fftMag.FftMeanWithFreq(35,46,freq));     // 3268 - 4296
        _fftBinBuffer[10] = (fftMag.FftMeanWithFreq(46,62,freq));    // 4296 - 5790
        _fftBinBuffer[11] = (fftMag.FftMeanWithFreq(62,82,freq));    // 5790 - 7658
        _fftBinBuffer[12] = (fftMag.FftMeanWithFreq(82,109,freq));   // 7658 - 10179
        _fftBinBuffer[13] = (fftMag.FftMeanWithFreq(109,145,freq));  // 10179 - 13541
        _fftBinBuffer[14] = (fftMag.FftMeanWithFreq(145,192,freq));  // 13541 - 17930
        _fftBinBuffer[15] = (fftMag.FftMeanWithFreq(192, 255,freq)); // 17930 - 23813
        
        for (var i=0; i<16; i++) {
            _fftBinBuffer[i][0] *= _fftPinkAdj[i];
            _fftBinBuffer[i][0] = _fftBinBuffer[i][0] * 1 /* multiplier/gain */ / 40 + _fftBinBuffer[i][0]/16.0;
        }
        
        for (var i=0; i < 16; i++) 
        {
            FftBins[i] = _fftBinBuffer[i][0];
            FftAvg[i] = (float)FftBins[i]*.05 + (1-.05)*FftAvg[i];
        }

      /* for (var index = 0; index < _fftBinBuffer.Length; index += 1)
       {
           Console.WriteLine($"[{index}]\t{Math.Round(_fftBinBuffer[index][1])}..{Math.Round(_fftBinBuffer[index][2])}Hz\t=\t{Math.Round(_fftBinBuffer[index][0], 4)}");
       }
       Console.WriteLine("-------------------");*/
    }

    private void RemoveDC(ref double[] buffer)
    {
        double mean = 0;
        for (var i = 0; i < buffer.Length; i++)
        {
            mean += buffer[i];
        }
        mean /= buffer.Length;
        
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] -= mean;
        }
    }
    
    
    private double[] _privateBuffer = new double[512];
    private double[] Preprocess(double[] input)
    {
        if (input.Length < 1)
        {
            return input;
        }
        
        Array.Copy(input, _privateBuffer, input.Length);
        
        // Using an exponential filter to smooth out the signal
        _expFilter.Process(ref _privateBuffer);
        
        // Calculate the current volume for silence detection
        var volume = Volume.DbSpl(_privateBuffer);
        if (double.IsInfinity(volume))
        {
            volume = 0.0f;
        }
        
        if (volume < MinVolume)
        {
            Array.Fill(_privateBuffer, 0);
        }
        
        Smooth(ref _privateBuffer);
        //RemoveDC(ref _privateBuffer);
        //DoFFT(_privateBuffer);
        

        // Apply multiplier
        for (var index = 0; index < _privateBuffer.Length; index++)
        {
            _privateBuffer[index] = (float)(Multiplier * _privateBuffer[index]);
        }

        return _privateBuffer;
    }

    protected double FindMaxSample(double[] buffer)
    {
        var maxSample = 0.0;
        foreach (var sample in buffer)
        {
            if (sample > maxSample)
            {
                maxSample = sample;
            }
        }

        return maxSample;
    }
    
    protected int NextSample(ref double[] processed, bool oneFrame = true)
    {
        return NextSample(ref processed, ref _nullBuffer!, oneFrame);
    }

    protected int NextSample(ref double[] processed, ref double[] raw, bool oneFrame = true)
    {
        var samples = Array.Empty<double>();
        if (oneFrame)
        {
            SamplesQueue.TryDequeue(out var floats);
            if (!SamplesQueue.IsEmpty)
            {
                SamplesQueue.Clear();
            }
            var sample = floats is { Length: > 0 } ? floats[0] : null;
            if (sample != null)
            {
                samples = sample;
            }
        }
        else
        {
            while (!SamplesQueue.IsEmpty)
            {
                SamplesQueue.TryDequeue(out var floats);
                var sample = floats is { Length: > 0 } ? floats[0] : null;
                if (sample != null)
                {
                    samples = samples.Concat(sample).ToArray();
                }
            }
        }

        if (raw.Length > 0)
        {
            if (samples.Length > raw.Length)
            {
                Console.WriteLine("NextSample: raw target array too small");
            }
            Array.Copy(samples, raw, raw.Length);
        }

        processed = Preprocess(samples);

        return samples.Length;
    }

    private void OnNewSamples(object? sender, double[][] samples)
    {
        SamplesQueue.Enqueue(samples);
    }
}