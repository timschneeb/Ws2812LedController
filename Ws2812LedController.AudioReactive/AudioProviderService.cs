namespace Ws2812LedController.AudioReactive;

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

    public event EventHandler<double[][]>? NewSamples;
    
    public void Start()
    {
        _sound.Start();
    }

    public async Task StopAsync()
    {
        await _sound.StopAsync();
    }

    public void InjectSamples(double[][] samples)
    {
        NewSamples?.Invoke(this, samples);
    }
}