using System.Collections.Concurrent;

namespace Ws2812AudioReactiveClient;

public class AudioProviderService
{
    private static readonly Lazy<AudioProviderService> Lazy =
        new(() => new AudioProviderService());

    private readonly SoundInputStream _sound;

    public static AudioProviderService Instance => Lazy.Value;
    private AudioProviderService(string inputSink = "jamesdsp_sink.monitor")
    {
        _sound = new SoundInputStream(inputSink);
        _sound.NewSamplesReceived += (_, bytes) => NewSamples?.Invoke(this, bytes);
        Start();
    }

    public event EventHandler<float[][]>? NewSamples;
    
    public void Start()
    {
        _sound.Start();
    }

    public async Task StopAsync()
    {
        await _sound.StopAsync();
    }

    public void InjectSamples(float[][] samples)
    {
        NewSamples?.Invoke(this, samples);
    }
}