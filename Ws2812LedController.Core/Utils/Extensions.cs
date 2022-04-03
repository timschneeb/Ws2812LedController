using System.Drawing;

namespace Ws2812LedController.Core.Utils;

public static class Extensions
{
    public static T Map<T>(this T value, dynamic fromSource, dynamic toSource, dynamic fromTarget, dynamic toTarget)
    {
        if (fromSource == toSource)
        {
            return (T)fromSource;
        }
        
        return (T)((value - fromSource) / (toSource - fromSource) * (toTarget - fromTarget) + fromTarget);
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