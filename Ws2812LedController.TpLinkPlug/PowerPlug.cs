using TPLinkSmartDevices.Devices;
using Ws2812LedController.Core;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;

namespace Ws2812LedController.TpLinkPlug;

public class PowerPlug
{
    private readonly Ref<LedManager> _mgr;
    private TPLinkSmartPlug? _plug;
    private readonly string _ip;

    /// <summary>
    /// Returns the current power state of the plug. If the plug is unreachable, returns null.
    /// </summary>
    /// <remarks>
    /// The setter and getter are blocking operations. Use the async helper methods instead.
    /// </remarks>
    /// <exception cref="ArgumentNullException">If the setter receives a null value.</exception>
    public bool? IsPowered
    {
        get
        {
            try
            {
                return _plug.OutletPowered;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to update plug power state: {e}\n");
                return null;
            }
        }
        set
        {
            try
            {
                _plug.OutletPowered = value ?? throw new ArgumentNullException(nameof(value));
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to get plug power state: {e}\n");
            }
        }
    }

    public PowerPlug(Ref<LedManager> mgr, string ip)
    {
        _mgr = mgr;
        _ip = ip;
        _plug = CreatePlug(ip);

        _mgr.Value.SegmentPowerStateChanged += OnLedSegmentPowerStateChanged;
        _ = SetPowerStateAsync(_mgr.Value.IsPowered());
    }

    private void OnLedSegmentPowerStateChanged(object? sender, LedManager.PowerStateChangedEventArgs e)
    {
        // When powering off, wait until the power animation is done
        if (e.State is PowerState.PoweringOff)
            return;

        // Use LedManager.IsPowered since other strips may still need power
        _ = SetPowerStateAsync(_mgr.Value.IsPoweredOrPoweringOn());
    }

    private static TPLinkSmartPlug? CreatePlug(string ip)
    {
        try
        {
            return new TPLinkSmartPlug(ip);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to create TPLink plug: {e}\n");
            return null;
        }
    }
    
    public Task ToggleAsync() =>
        Task.Run(() =>
        {
            try
            {
                _plug ??= CreatePlug(_ip);
                if (_plug != null)
                {
                    _plug.OutletPowered = !_plug.OutletPowered;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to toggle plug power state: {e}\n");
            }
        });
    
    public Task SetPowerStateAsync(bool power) =>
        Task.Run(() =>
        {
            try
            {
                _plug ??= CreatePlug(_ip);
                if (_plug != null)
                {
                    _plug.OutletPowered = power;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to set plug power state: {e}\n");
            }
        });
}