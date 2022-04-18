namespace Ws2812LedController.AudioReactive.Dsp;

/*+
 Dirty utility class to generate adjustment arrays for the FFT bins by playing back pink noise
*/
public class FftCalibration
{
    private readonly double[] _fftMean;

    public FftCalibration(int binLength)
    {
        _fftMean = new double[binLength];
    }

    public void AddDataset(double[] bins)
    {
        if (bins.Length != _fftMean.Length)
        {
            Console.Error.WriteLine($"FftCalibration: Too many/little bins ({bins.Length}); skipped");
            return;
        }

        for (var i = 0; i < bins.Length; i++)
        {
            _fftMean[i] = (bins[i] * (bins.Length - 1) + _fftMean[i]) / bins.Length;
        }
    }

    public double[] ToAdjustmentData(int referenceBin)
    {
        var cal = new double[_fftMean.Length];
        var reference = _fftMean[referenceBin];
        for (var i = 0; i < _fftMean.Length; i++)
        {
            cal[i] = reference / _fftMean[i];
        }

        return cal;
    }

    public string ToCsv(int referenceBin)
    {
        return string.Join(",", ToAdjustmentData(referenceBin));
    }
}