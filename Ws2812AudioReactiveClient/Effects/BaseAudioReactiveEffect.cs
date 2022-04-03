using System.Collections.Concurrent;
using System.Diagnostics;
using Aubio;
using Aubio.Spectral;
using NWaves.FeatureExtractors;
using NWaves.FeatureExtractors.Base;
using NWaves.FeatureExtractors.Multi;
using NWaves.FeatureExtractors.Options;
using NWaves.Filters.Fda;
using NWaves.Signals.Builders;
using Ws2812AudioReactiveClient.Dsp;
using Ws2812LedController.Core.Effects.Base;

namespace Ws2812AudioReactiveClient.Effects;

public abstract class BaseAudioReactiveEffect : IEffect
{
    protected readonly ConcurrentQueue<float[][]> SamplesQueue = new();
    public override int Speed { set; get; } = 1000 / 60;
    public virtual int MinVolume { set; get; } = -60;
    public virtual double Multiplier { set; get; } = 1;

    private float[] _nullBuffer = Array.Empty<float>();
    
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
        _sampleAvg = 0;
        _sample = 0;
        _micLev = 0;
        _peakTime = 0;
        _timeSinceStart.Reset();
        base.Reset();
    }

    /* Smoothed peak detection */
    protected readonly Stopwatch _timeSinceStart = new();
    private int _sample; // Current sample.
    private float _sampleAvg = 0F; // Smoothed Average.
    private float _micLev = 0F; // Used to convert returned value to have '0' as minimum.
    private long _peakTime = 0;

    protected bool DoSmoothedPeakCheck(float[] samples, int triggerVolume = 11, int squelch = 7)
    {
        return DoSmoothedPeakCheck(samples, out _, triggerVolume, squelch);
    }

    protected bool DoSmoothedPeakCheck(float[] samples, out float sampleAvg, int triggerVolume = 11, int squelch = 7)
    {
        var micIn = (short)(samples[0] * 1024);
        _micLev = ((_micLev * 31) + micIn) / 32; // Smooth it out over the last 32 samples for automatic centering.
        micIn = (short)(micIn - _micLev); // Let's center it to 0 now.
        micIn = Math.Abs(micIn); // And get the absolute value of each sample.
        _sample = (micIn <= squelch) ? 0 : _sample + micIn / 2; // smooth out with the last sample.
        sampleAvg = _sampleAvg = ((_sampleAvg * 31) + _sample) / 32; // Smooth it out over the last 32 samples.

        if (_sample > (_sampleAvg + triggerVolume) && _timeSinceStart.ElapsedMilliseconds > (_peakTime + 50))
        {
            _peakTime = _timeSinceStart.ElapsedMilliseconds;
            return true;
        }

        return false;
    }
    
    private void Preprocess(ref float[] buffer)
    {
        // Calculate the current volume for silence detection
        var volume = Volume.DbSpl(buffer);
        if (double.IsInfinity(volume))
        {
            volume = 0.0f;
        }
        
        if (volume < MinVolume)
        {
            Array.Fill(buffer, 0);
        }

        // Apply multiplier
        for (var index = 0; index < buffer.Length; index++)
        {
            buffer[index] = (float)(Multiplier * buffer[index]);
        }
    }

    protected double FindMaxSample(float[] buffer)
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
    
    protected int NextSample(ref float[] processed, bool oneFrame = true)
    {
        return NextSample(ref processed, ref _nullBuffer!, oneFrame);
    }

    protected int NextSample(ref float[] processed, ref float[] raw, bool oneFrame = true)
    {
        var samples = Array.Empty<float>();
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

        Preprocess(ref samples);
        processed = samples;

        return samples.Length;
    }
    
    private void OnNewSamples(object? sender, float[][] samples)
    {
        SamplesQueue.Enqueue(samples);
    }
}