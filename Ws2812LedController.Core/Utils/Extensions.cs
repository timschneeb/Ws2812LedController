using System.Drawing;

namespace Ws2812LedController.Core.Utils;

public static class Extensions
{
    public static T Map<T>(this T value, dynamic fromSource, dynamic toSource, dynamic fromTarget, dynamic toTarget, bool clamp = false)
    {
        if (fromSource == toSource)
        {
            return (T)fromSource;
        }

        var mapped = (T)((double)(value - fromSource) / (toSource - fromSource) * (toTarget - fromTarget) + fromTarget);
        mapped = clamp switch // TODO clamp doesn't handle reversed from/to values properly
        {
            true when mapped < fromTarget => fromTarget,
            true when mapped > toTarget => toTarget,
            _ => mapped
        };

        return mapped;
    }
    
    public static T Clamp<T>(this T value, dynamic min, dynamic max)
    {
        if (value > max)
        {
            return max;
        }
        if (value < min)
        {
            return min;
        }

        return value;
    }
    
    public static void Populate<T>(this T[] arr, T value ) 
    {
        for (var i = 0; i < arr.Length; i++) 
        {
            arr[i] = value;
        }
    }
    
    public static uint ToUInt32(this Color c)
    {
        return (uint)(((c.A << 24) | (c.R << 16) | (c.G << 8) | c.B) & 0xffffffffL);
    }

    public static T[] ToArray<T>(this Color c) where T : struct, IConvertible
    {
        return new[] { (T)(object)c.R, (T)(object)c.G, (T)(object)c.B };
    }
    
    public static byte ByIndex(this Color c, int index)
    {
        return index switch
        {
            0 => c.R,
            1 => c.G,
            2 => c.B,
            _ => throw new ArgumentOutOfRangeException(nameof(index), index, null)
        };
    }

    public static Color ToColor(this uint value, bool opaque = false)
    {
        return Color.FromArgb(opaque ? 0xFF : (byte)((value >> 24) & 0xFF),
            (byte)((value >> 16) & 0xFF),
            (byte)((value >> 8) & 0xFF),
            (byte) (value & 0xFF));
    }
    
    public static async Task<bool> WaitOneAsync(this WaitHandle handle, int millisecondsTimeout, CancellationToken cancellationToken)
    {
        RegisteredWaitHandle? registeredHandle = null;
        var tokenRegistration = default(CancellationTokenRegistration);
        try
        {
            var tcs = new TaskCompletionSource<bool>();
            registeredHandle = ThreadPool.RegisterWaitForSingleObject(
                handle,
                (state, timedOut) => ((TaskCompletionSource<bool>?)state)?.TrySetResult(!timedOut),
                tcs,
                millisecondsTimeout,
                true);
            tokenRegistration = cancellationToken.Register(
                state => ((TaskCompletionSource<bool>?)state)?.TrySetCanceled(),
                tcs);
            return await tcs.Task;
        }
        finally
        {
            registeredHandle?.Unregister(null);
            await tokenRegistration.DisposeAsync();
        }
    }
    
    public static Task<bool> WaitOneAsync(this WaitHandle handle, TimeSpan timeout, CancellationToken cancellationToken)
    {
        return handle.WaitOneAsync((int)timeout.TotalMilliseconds, cancellationToken);
    }

    public static Task<bool> WaitOneAsync(this WaitHandle handle, CancellationToken cancellationToken)
    {
        return handle.WaitOneAsync(Timeout.Infinite, cancellationToken);
    }

}