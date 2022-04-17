using System.Collections.Concurrent;
using System.Diagnostics;
using FftSharp;
using Ws2812AudioReactiveClient.Dsp;
using Ws2812AudioReactiveClient.Model;
using Ws2812LedController.Core.Effects.Base;

namespace Ws2812AudioReactiveClient.Effects.Base;

public abstract class BaseAudioReactiveEffect : IEffect
{
    protected readonly ConcurrentQueue<double[][]> SamplesQueue = new();
    public override int Speed { set; get; } = 1000 / 60;
    public virtual int MinVolume { set; get; } = -70;
    public virtual double Multiplier { set; get; } = 1;
    public virtual double AvgSmoothingStrength { set; get; } = 10;

    private double[] _procNullBuffer = Array.Empty<double>();
    private double[] _rawNullBuffer = Array.Empty<double>();

    private readonly ExpFilter _expFilter;
    
    protected BaseAudioReactiveEffect()
    {
        _expFilter = new ExpFilter();
    }
    
    protected override void Begin()
    {
        TimeSinceStart.Restart();
        AudioProviderService.Instance.NewSamples += OnNewSamples;
        base.Begin();
    }

    protected override void End()
    {
        TimeSinceStart.Reset();
        AudioProviderService.Instance.NewSamples -= OnNewSamples;
        base.End();
    } 
    
    public override void Reset()
    {
        SampleAvg = 0;
        SampleAgc = 0;
        Sample = 0;
        _micLev = 0;
        _multiAgc = 0;
        _lastFftBinLength = 0;
        Array.Fill(_peakTime, 0);
        Array.Fill(_fftSampleBuffer, 0);
        Array.Fill(FftMajorPeak, 0);
        Array.Fill(FftCompressedBins, 0);
        Array.Fill(FftAvg, 0);
        Array.Fill(FftBins, 0);
        Array.Fill(FftFreq, 0);
        TimeSinceStart.Reset();
        base.Reset();
    }

    protected AvgSmoothingMode AvgSmoothingMode = AvgSmoothingMode.All;
    protected double Sample = 0;
    protected double SampleAvg = 0;
    protected int SampleAgc;
    protected readonly Stopwatch TimeSinceStart = new();

    private readonly long[] _peakTime = new long[16 + 1];
    private double _micLev = 0;
    private double _multiAgc; // sample * multAgc = sampleAgc. Our multiplier
    private const int TargetAgc = 60; // This is our setPoint at 20% of max for the adjusted output

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
        var minDetect = smootheWithAvg ? (FftAvg[fftBin] + triggerVolume) : triggerVolume;
        if (FftCompressedBins[fftBin] > minDetect && TimeSinceStart.ElapsedMilliseconds > (_peakTime[fftBin] + 50))
        {
            _peakTime[fftBin] = TimeSinceStart.ElapsedMilliseconds;
            return true;
        }

        return false;
    }
    protected bool IsPeak(double triggerVolume = 100, bool smootheWithAvg = true)
    {
        var minDetect = smootheWithAvg ? (SampleAvg + triggerVolume) : triggerVolume;
        if (Sample > minDetect && TimeSinceStart.ElapsedMilliseconds > (_peakTime[16] + 50))
        {
            _peakTime[16] = TimeSinceStart.ElapsedMilliseconds;
            return true;
        }
        
        return false;
    }
    
    private void Smooth(ref double[] buffer)
    {
        // TODO refactor this
        switch (AvgSmoothingMode)
        {
            case AvgSmoothingMode.Mean:
                var sample = buffer.Rms();
                _micLev = ((_micLev * 31) + sample) / 32.0;                      // Smooth it out over the last 32 samples for automatic centering.
                sample -= _micLev;                                            // Let's center it to 0 now.
                sample = Math.Abs(sample);                                         // And get the absolute value of each sample.
                Sample =/* sample < 1e-10 ? 0 :*/ (Sample + sample) / 2.0;     // Using a ternary operator, the resultant sample is either 0 or it's a bit smoothed out with the last sample.
        
                SampleAvg = ((SampleAvg * AvgSmoothingStrength) + Sample) / AvgSmoothingStrength + 1; // Smooth it out over the last 32 samples.
                
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
                    buffer[index] = Math.Abs(buffer[index]); // And get the absolute value of each sample
                    Sample = buffer[index];
                }
                
                SampleAvg = (SampleAvg * AvgSmoothingStrength + buffer.Rms()) / (AvgSmoothingStrength + 1);
                break;
        }
    }
    private void CalculateAgcAverage()
    {
        _multiAgc = (SampleAvg < 1) ? TargetAgc : TargetAgc / SampleAvg;  // Make the multiplier so that sampleAvg * multiplier = setpoint
        var tmpAgc = (int)(Sample * _multiAgc);
        SampleAgc = tmpAgc;  
    }
    
    /* FFT calculation */
    protected readonly double[] FftMajorPeak = new double[2];
    protected readonly Range[] FftCompressedFreqRanges = new Range[16];
    protected readonly double[] FftCompressedBins = new double[16];
    protected readonly double[] FftAvg = new double[16];
    protected double[] FftBins = new double[256];
    protected double[] FftFreq = new double[256];
    
    private int _lastFftBinLength = 0;
    private readonly double[] _fftSampleBuffer = new double[512];
    private readonly double[][] _fftBinBuffer = new double[16][];
    private readonly double[] _fftCompPinkAdj =
    {
        1,1.17142591661794,1.47884429079972,1.76196329534098,2.17953539006265,2.62126673958848,3.44609031341581,4.48351183007203,6.2246968291675,8.69568659154196,12.1973195560931,17.4190218987397,25.4925190458713,36.0335128075597,51.8826964891306,113.143509248435
    };
    private readonly double[] _fftPinkAdj =
    {
        1.1581494320320136,0.17954052992643268,0.11411783222952508,0.20926814707618135,0.37213848274527855,0.4643181404444211,0.1916398765570712,0.26210892164898353,0.7168136842389251,0.516509164253814,0.6870328616935559,0.6679204271387482,0.6574741051014277,0.9362197109571655,1.248645946992092,1.0660006319130704,1.2139778333675577,0.8174254981646795,0.4781453767390736,0.4626804540392181,0.7715990060618365,2.253259961280299,2.622921837713043,0.9403090536017659,0.7314719182237536,0.9153222794529826,0.7840131556679397,1.417230986003442,1.6497674855198248,5.434296398071467,1.1382129414513786,0.7314415570395271,0.7446841573370444,1.1523555630013247,2.4060714784422372,1.4655540990310674,3.148371564892358,1.6280795695492047,2.9763456866888265,2.1010899287886033,1.2624638257139833,3.493996624411244,2.0968035760682855,1.2167851692045208,1.182832818680565,1.0594417270215786,2.3420833746433978,5.389663414391671,3.1435003507184334,1.8918696451707653,1,1.1612878423572237,2.7137492255703237,0.9966791657465396,1.3631051501215545,2.9769555006187263,1.2320997307782582,1.1198072776134518,1.644264809755738,1.7304582447365258,1.375367530528066,5.194940486813968,1.302773263402874,7.658810117033094,1.112933472698576,2.0242845359661588,5.097340011701368,1.5006140960295244,0.620917164572307,0.6953279668043848,1.2132748303731524,1.2871867008706075,0.9095785155915695,0.5090232918175216,0.6500227651928222,1.7008854525300892,2.876993775480498,2.0291297404194815,2.324700899972868,1.830018088370599,1.3581865628507404,1.1060333833843814,1.6132467727143194,2.5256865440774536,1.7347666301519946,1.595563710227952,1.1499654972254554,1.5551896214220229,2.5195940356046087,1.0730323313038004,0.8136167754000457,1.3238950695686587,1.9085195239442243,3.0382567972355874,1.8957569735740856,1.2587029422291294,2.2128147423402513,2.4964588259261955,2.426405496670787,3.30464290804675,2.2569397806193296,5.357106856337299,3.427428166009944,36.71520083771222,5.884998370208554,7.886424310987245,3.046262033336842,1.2786465752269054,1.247757334359136,1.6101888773265347,4.0375983400449265,1.6298784355366152,1.368205571074396,5.134551460125144,3.1081488030092506,1.514923617228621,2.214858263310564,2.82926788575316,2.121052751157657,3.3465424765280543,2.9219720684425226,2.4465883063871106,1.2315246057718108,1.022325876032168,2.116197792948019,3.396625141683459,2.404382352553807,1.0804436487784188,1.070916487552353,2.1525521890253847,2.939047757133706,4.321210420519421,7.457594218502982,6.096009981453517,3.5219845642284477,2.7345383461817043,1.5823484224326607,1.4006274390504538,2.738206821204548,2.026358517210773,1.1240130112315265,2.894563642378296,3.8553973720018737,2.3917573601122952,3.651671436779412,21.394328828147593,2.684445546767358,1.925968557819417,2.4008289610326004,6.302119974616959,2.9440515920009434,3.702538247146957,2.7191452780052754,1.1493836193172606,1.296092250109726,4.280241673830898,8.309769754710677,2.975296230531208,2.116486220890192,4.715895206505838,2.568106446357662,2.7709955138530495,3.5404787295080884,2.4336903152281635,2.2185620813784634,6.551282372115161,2.3464305531897582,1.4191258203313633,1.1337491710553549,0.8378673219089272,0.9054998253340769,3.259854484917041,14.667521597456927,3.6637008759702567,1.9267317755541253,2.2075951386801456,3.290365925495642,2.142356794494174,2.1602615928626827,4.623584059758533,2.3868350904431472,2.5761402119606305,5.285235960107152,3.9075172428289426,5.103899064062245,2.1426852955572295,2.6303737985044666,4.009799139974323,3.2265285415429674,6.681694147228474,2.2676350519357116,3.340197739090201,7.066105466589689,3.240718390706586,6.066253321621093,2.7301016959613484,3.0654602617946787,4.515084581516429,7.055916552880353,1.596410837795214,1.2901378369431562,2.267608995342418,6.04665060800628,2.403389072445319,1.7134381016907676,2.656730214846545,2.3059526166067172,2.452372950540476,1.4625618938211398,2.543820731722099,3.2356750218949166,2.4813680957224595,4.789969385712427,4.903150267390659,3.2519248245158283,2.8054503691240775,3.979128459969361,13.562758781468055,43.39883893968304,13.365691307880525,7.698873113243683,9.860450047019057,6.620516805478772,4.690928696803322,8.517174737658681,26.344725017791202,13.577292520516147,31.652923354885058,60.23425195346719,49.703630453870616,41.646105303242294,25.21525579391836,63.15714070404175,79.38012542539227,500.0456415581142,817.588845865582,1160.2598788278012,1806.1826207803347,785.9466693146092,1329.953980329877,2347.170432239264,5931.3540120576345,7499.63678997414,51701.520906432226,4685.550047168866,5542.013686335011,30740.51344622,11321.926220953044,19988.027957276914,79996.9971059273,47844.36582078675,37759.16780453839,77649.5662458079,113312.10430083403,62632.64254611067,57582.4971406396,125947.53141706402
    };
    
    private void DoFFT(double[] buffer)
    {
        if (buffer.Length < 1)
        {
            return;
        }
        
        Array.Copy(buffer, _fftSampleBuffer, Math.Min(_fftSampleBuffer.Length, buffer.Length));
        
        var window = new FftSharp.Windows.Hanning();
        window.ApplyInPlace(_fftSampleBuffer);

        // magnitude (units²) as real numbers
        FftBins = Transform.FFTmagnitude(_fftSampleBuffer);
        if (_lastFftBinLength != FftBins.Length)
        {
            FftFreq = Transform.FFTfreq(48000, FftBins.Length);
            _lastFftBinLength = FftBins.Length;
        }
        
        for (var i = 0; i < Math.Min(FftBins.Length, _fftPinkAdj.Length); i++)
        {
            FftBins[i] *= _fftPinkAdj[i];
        }
        
        double f = 0, v = 0;
        var found = FftBins.MajorPeak(_fftSampleBuffer.Length - 1, 48000, ref f, ref v);
        FftMajorPeak[0] = found ? f : 0;
        FftMajorPeak[1] = found ? v : 0;
        
        _fftBinBuffer[0] = (FftBins.FftMeanWithFreq(1,2,FftFreq));       // 93 - 187 (48000Hz SR only)
        _fftBinBuffer[1] = (FftBins.FftMeanWithFreq(2,3,FftFreq));       // 187 - 280
        _fftBinBuffer[2] = (FftBins.FftMeanWithFreq(3,5,FftFreq));       // 280 - 467
        _fftBinBuffer[3] = (FftBins.FftMeanWithFreq(5,7,FftFreq));       // 467 - 654
        _fftBinBuffer[4] = (FftBins.FftMeanWithFreq(7,10,FftFreq));      // 654 - 934
        _fftBinBuffer[5] = (FftBins.FftMeanWithFreq(10,14,FftFreq));     // 934 - 1307
        _fftBinBuffer[6] = (FftBins.FftMeanWithFreq(14,19,FftFreq));     // 1307 - 1774
        _fftBinBuffer[7] = (FftBins.FftMeanWithFreq(19,26,FftFreq));     // 1774 - 2428
        _fftBinBuffer[8] = (FftBins.FftMeanWithFreq(26,35,FftFreq));     // 2428 - 3268
        _fftBinBuffer[9] = (FftBins.FftMeanWithFreq(35,46,FftFreq));     // 3268 - 4296
        _fftBinBuffer[10] = (FftBins.FftMeanWithFreq(46,62,FftFreq));    // 4296 - 5790
        _fftBinBuffer[11] = (FftBins.FftMeanWithFreq(62,82,FftFreq));    // 5790 - 7658
        _fftBinBuffer[12] = (FftBins.FftMeanWithFreq(82,109,FftFreq));   // 7658 - 10179
        _fftBinBuffer[13] = (FftBins.FftMeanWithFreq(109,145,FftFreq));  // 10179 - 13541
        _fftBinBuffer[14] = (FftBins.FftMeanWithFreq(145,192,FftFreq));  // 13541 - 17930
        _fftBinBuffer[15] = (FftBins.FftMeanWithFreq(192, 255,FftFreq)); // 17930 - 23813
        
        for (var i=0; i<16; i++)
        {
            //_fftBinBuffer[i][0] *= _fftCompPinkAdj[i];
            _fftBinBuffer[i][0] = _fftBinBuffer[i][0] * 1 /* multiplier/gain */ / 40 + _fftBinBuffer[i][0]/16.0;
        }

        for (var i=0; i < 16; i++)
        {
            FftCompressedFreqRanges[i] = new Range((Index)_fftBinBuffer[i][1], (Index)_fftBinBuffer[i][2]);
            FftCompressedBins[i] = _fftBinBuffer[i][0];
            FftAvg[i] = FftCompressedBins[i]*.05 + (1-.05)*FftAvg[i];
        }
        
        /*for (var index = 0; index < _fftBinBuffer.Length; index += 1)
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
        
        Array.Fill(_privateBuffer, 0);
        Array.Copy(input, _privateBuffer, Math.Min(input.Length, _privateBuffer.Length));
        
        // Using an exponential filter to smooth out the signal
        //_expFilter.Process(ref _privateBuffer);
        
        // Calculate the current volume for silence detection
        var volume = VolumeLevel.DbSpl(_privateBuffer);
        if (double.IsInfinity(volume))
        {
            volume = 0.0;
        }
        
        if (volume < MinVolume)
        {
            Array.Fill(_privateBuffer, 0);
        }
        
        // Let's work with u16 values in this area, so I don't need to rewrite existing stuff
        for (var i = 0; i < _privateBuffer.Length; i++)
        {
            _privateBuffer[i] *= 65536;
        }
        
        RemoveDC(ref _privateBuffer);
        DoFFT(_privateBuffer);
        
        Smooth(ref _privateBuffer);
        CalculateAgcAverage();
        
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
        return NextSample(ref _procNullBuffer, ref _rawNullBuffer, oneFrame);
    } 
    
    protected int NextSample(ref double[] processed, bool oneFrame = true)
    {
        return NextSample(ref processed, ref _rawNullBuffer, oneFrame);
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
            Array.Copy(samples, raw, Math.Min(raw.Length, samples.Length));
        }
        
        processed = Preprocess(samples);
        return samples.Length;
    }

    private void OnNewSamples(object? sender, double[][] samples)
    {
        SamplesQueue.Enqueue(samples);
    }
}