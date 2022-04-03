/*using Numpy;

namespace Ws2812LedController.Core.Colors;

public class Gradient
{
    public object _gradient_curve = null;
            
    public virtual double _comb(int N, int k) {
        N = Convert.ToInt32(N);
        k = Convert.ToInt32(k);
        if (k > N || N < 0 || k < 0) {
            return 0;
        }
        var M = N + 1;
        var nterms = Math.Min(k, N - k);
        var numerator = 1;
        var denominator = 1;
        foreach (var j in Enumerable.Range(1, nterms + 1 - 1)) {
            numerator *= M - j;
            denominator *= j;
        }
        return (double)numerator / denominator;
    }
            
    // The Bernstein polynomial of n, i as a function of t
    public virtual double _bernstein_poly(int i, int n, int t) {
        return this._comb(n, i) * Math.Pow(t, n - i) * Math.Pow(1 - t, i);
    }
            
    public virtual object _ease(int chunk_len, double start_val, double end_val, double slope = 1.5) {
        var x = np.linspace(0, 1, chunk_len);
        var diff = end_val - start_val;
        var slopeArray = np.array(slope);
        var pow_x = np.power(x,  slopeArray);
        return diff * pow_x / (pow_x + np.power(1 - x, slopeArray)) + start_val;
    }
            
    // Makes a coloured block easing from start to end colour
    public virtual object _color_ease(int chunk_len, double[] start_color, double[] end_color) {
        return np.array((from i in Enumerable.Range(0, 3)
            select this._ease(chunk_len, start_color[i], end_color[i])).ToArray());
    }

}*/