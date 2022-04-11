using System.Collections.Concurrent;
using System.Diagnostics;
using FftSharp;
using Ws2812AudioReactiveClient.Dsp;
using Ws2812AudioReactiveClient.Model;
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

    protected AvgSmoothingMode AvgSmoothingMode = AvgSmoothingMode.All;

    /* Smoothed peak detection */
    protected readonly Stopwatch _timeSinceStart = new();
    private double _sample = 0; // Current sample.
    private long[] _peakTime = new long[16 + 1];

    /** Volume peak check */
    protected bool IsFftPeak(FftCBinSelector selector, double triggerVolume = 100, bool smootheWithAvg = true)
    {
        for (var i = selector.Start; i <= selector.End; i++)
        {
            if (IsFftPeak(i, triggerVolume, smootheWithAvg))
            {
                return true;
            }
        }

        return false;
    }
    protected bool IsFftPeak(int fftBin, double triggerVolume = 100, bool smootheWithAvg = true)
    {
        Console.WriteLine($"BIN {fftBin}:\t{FftCompressedBins[0]};\tAVG: {FftAvg[0]}");
        
        var minDetect = smootheWithAvg ? (FftAvg[fftBin] + triggerVolume) : triggerVolume;
        if (FftCompressedBins[fftBin] > minDetect && _timeSinceStart.ElapsedMilliseconds > (_peakTime[fftBin] + 50))
        {
            _peakTime[fftBin] = _timeSinceStart.ElapsedMilliseconds;
            return true;
        }
        
        return false;
    }   
    
    protected bool IsPeak(double triggerVolume = 0.05, bool smootheWithAvg = true)
    {
        var minDetect = smootheWithAvg ? (SampleAvg + triggerVolume) : triggerVolume;
        if (_sample > minDetect && _timeSinceStart.ElapsedMilliseconds > (_peakTime[16] + 50))
        {
            _peakTime[16] = _timeSinceStart.ElapsedMilliseconds;
            return true;
        }
        
        return false;
    }

   private double _micLev = 0; // Used to convert returned value to have '0' as minimum.
   //private double _sample = 0; // Used to convert returned value to have '0' as minimum.
    protected double SampleAvg = 0; // Smoothed Average.
    protected double SampleAvg16 => SampleAvg * 65536;

    private void Smooth(ref double[] buffer)
    {
        // TODO refactor this
        switch (AvgSmoothingMode)
        {
            case AvgSmoothingMode.Mean:
                var sample = buffer.Mean();
                _micLev = ((_micLev * 31) + sample) / 32.0;                      // Smooth it out over the last 32 samples for automatic centering.
                sample -= _micLev;                                            // Let's center it to 0 now.
                sample = Math.Abs(sample);                                         // And get the absolute value of each sample.
                _sample =/* sample < 1e-10 ? 0 :*/ (_sample + sample) / 2.0;     // Using a ternary operator, the resultant sample is either 0 or it's a bit smoothed out with the last sample.
        
                SampleAvg = ((SampleAvg * 15) + _sample) / 16.0; // Smooth it out over the last 32 samples.
                
                /* We still need to smooth the buffer itself properly */
                for (var index = 0; index < buffer.Length; index++)
                {
                    _micLev = ((_micLev * 31) + buffer[index]) / 32; 
                    // Smooth it out over the last 32 samples for automatic centering
                    buffer[index] -= _micLev; // Let's center it to 0 now
                    buffer[index] = Math.Abs( buffer[index]); // And get the absolute value of each sample
                }
                break;
            case AvgSmoothingMode.All:
                for (var index = 0; index < buffer.Length; index++)
                {
                    _micLev = ((_micLev * 31) + buffer[index]) / 32; 
                    // Smooth it out over the last 32 samples for automatic centering
                    buffer[index] -= _micLev; // Let's center it to 0 now
                    buffer[index] = Math.Abs( buffer[index]); // And get the absolute value of each sample
                    _sample = buffer[index];
                }
        
                SampleAvg = (SampleAvg * 15 + buffer.Mean()) / 16;
                break;
        }
    }
    
    /* FFT calculation */
    protected FftOptions? FftOptions;

    protected readonly double[] FftMajorPeak = new double[2];
    protected readonly double[] FftCompressedBins = new double[16];
    protected readonly double[] FftAvg = new double[16];
    protected double[] FftBins = new double[256];
    
    private readonly double[][] _fftBinBuffer = new double[16][];
    private readonly double[] _fftPinkAdj =
    {
        1,1.17142591661794,1.47884429079972,1.76196329534098,2.17953539006265,2.62126673958848,3.44609031341581,4.48351183007203,6.2246968291675,8.69568659154196,12.1973195560931,17.4190218987397,25.4925190458713,36.0335128075597,51.8826964891306,113.143509248435
    };
    
    private readonly double[] _fftSampleBuffer = new double[512];
    
    private void DoFFT(double[] buffer)
    {
        if (buffer.Length < 1)
        {
            return;
        }
        
        Array.Copy(buffer, _fftSampleBuffer, _fftSampleBuffer.Length);
        
        if (FftOptions == null)
        {
            // return;
        }

        // Let's work with u16 values in this area, so I don't need to rewrite existing stuff
        for (var i = 0; i < _fftSampleBuffer.Length; i++)
        {
            _fftSampleBuffer[i] *= 65536;
        }
        
        var window = new FftSharp.Windows.Hanning();
        window.ApplyInPlace(_fftSampleBuffer);

        // magnitude (units²) as real numbers
        FftBins = Transform.FFTmagnitude(_fftSampleBuffer);
        var freq = Transform.FFTfreq(48000, FftBins.Length);

        double f = 0, v = 0;
        var found = FftBins.MajorPeak(_fftSampleBuffer.Length - 1, 48000, ref f, ref v);
        FftMajorPeak[0] = found ? f : 0;
        FftMajorPeak[1] = found ? v : 0;
        if (found)
        {
            //Console.WriteLine($"{Math.Round(f,4)}Hz\t\t=\t{Math.Round(v,4)}");
        }
        else
        {
            //Console.WriteLine("No major peak");
        }
        
        _fftBinBuffer[0] = (FftBins.FftMeanWithFreq(1,2,freq));       // 93 - 187 (48000Hz SR only)
        _fftBinBuffer[1] = (FftBins.FftMeanWithFreq(2,3,freq));       // 187 - 280
        _fftBinBuffer[2] = (FftBins.FftMeanWithFreq(3,5,freq));       // 280 - 467
        _fftBinBuffer[3] = (FftBins.FftMeanWithFreq(5,7,freq));       // 467 - 654
        _fftBinBuffer[4] = (FftBins.FftMeanWithFreq(7,10,freq));      // 654 - 934
        _fftBinBuffer[5] = (FftBins.FftMeanWithFreq(10,14,freq));     // 934 - 1307
        _fftBinBuffer[6] = (FftBins.FftMeanWithFreq(14,19,freq));     // 1307 - 1774
        _fftBinBuffer[7] = (FftBins.FftMeanWithFreq(19,26,freq));     // 1774 - 2428
        _fftBinBuffer[8] = (FftBins.FftMeanWithFreq(26,35,freq));     // 2428 - 3268
        _fftBinBuffer[9] = (FftBins.FftMeanWithFreq(35,46,freq));     // 3268 - 4296
        _fftBinBuffer[10] = (FftBins.FftMeanWithFreq(46,62,freq));    // 4296 - 5790
        _fftBinBuffer[11] = (FftBins.FftMeanWithFreq(62,82,freq));    // 5790 - 7658
        _fftBinBuffer[12] = (FftBins.FftMeanWithFreq(82,109,freq));   // 7658 - 10179
        _fftBinBuffer[13] = (FftBins.FftMeanWithFreq(109,145,freq));  // 10179 - 13541
        _fftBinBuffer[14] = (FftBins.FftMeanWithFreq(145,192,freq));  // 13541 - 17930
        _fftBinBuffer[15] = (FftBins.FftMeanWithFreq(192, 255,freq)); // 17930 - 23813
        
        for (var i=0; i<16; i++)
        {
            _fftBinBuffer[i][0] *= _fftPinkAdj[i];
            _fftBinBuffer[i][0] = _fftBinBuffer[i][0] * 1 /* multiplier/gain */ / 40 + _fftBinBuffer[i][0]/16.0;
        }

        for (var i=0; i < 16; i++) 
        {
            FftCompressedBins[i] = _fftBinBuffer[i][0];
            FftAvg[i] = FftCompressedBins[i]*.05 + (1-.05)*FftAvg[i];
        } for (var index = 0; index < _fftBinBuffer.Length; index += 1)
       {
           //Console.WriteLine($"[{index}]\t{Math.Round(_fftBinBuffer[index][1])}..{Math.Round(_fftBinBuffer[index][2])}Hz\t=\t{Math.Round(_fftBinBuffer[index][0], 4)}");
       }
       //Console.WriteLine("-------------------");

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

        _privateBuffer = input;
        
        Array.Copy(input, _privateBuffer, input.Length);
        
        // Using an exponential filter to smooth out the signal
        //_expFilter.Process(ref _privateBuffer);
        
        // Calculate the current volume for silence detection
        var volume = Volume.DbSpl(_privateBuffer);
        if (double.IsInfinity(volume))
        {
            volume = 0.0;
        }
        
        if (volume < MinVolume)
        {
            Array.Fill(_privateBuffer, 0);
        }
        
        RemoveDC(ref _privateBuffer);
        DoFFT(_privateBuffer);
        
        Smooth(ref _privateBuffer);
       
        // SampleAvg = (SampleAvg * 15 + _privateBuffer.Mean()) / 16;

        // Apply multiplier
        for (var index = 0; index < _privateBuffer.Length; index++)
        {
            _privateBuffer[index] *= Multiplier;
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
    
    protected int NextSample(bool oneFrame = true)
    {
        return NextSample(ref _nullBuffer, ref _nullBuffer, oneFrame);
    } 
    
    protected int NextSample(ref double[] processed, bool oneFrame = true)
    {
        return NextSample(ref processed, ref _nullBuffer, oneFrame);
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

        if (processed.Length > 0)
        {
            processed = Preprocess(samples);
        }
        else
        {
            Preprocess(samples);
        }

        return samples.Length;
    }

    private void OnNewSamples(object? sender, double[][] samples)
    {
        SamplesQueue.Enqueue(samples);
    }
}