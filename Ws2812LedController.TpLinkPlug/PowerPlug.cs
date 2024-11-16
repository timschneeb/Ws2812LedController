using TPLinkSmartDevices.Devices;
using Ws2812LedController.Core;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;

namespace Ws2812LedController.TpLinkPlug;

public class PowerPlug
{
    private readonly Ref<LedManager> _mgr;
    private readonly string _ip;

    public PowerPlug(Ref<LedManager> mgr, string ip)
    {
        _mgr = mgr;
        _ip = ip;

        _mgr.Value.SegmentPowerStateChanged += OnLedSegmentPowerStateChanged;
        SetPlugPower(_mgr.Value.IsPowered());
    }

    private void OnLedSegmentPowerStateChanged(object? sender, LedManager.PowerStateChangedEventArgs e)
    {
        if (e.State is PowerState.PoweringOn or PowerState.PoweringOff)
            return;

        // Use LedManager.IsPowered since other strips may still need power
        SetPlugPower(_mgr.Value.IsPowered());
    }

    public void SetPlugPower(bool powered)
    {
        try
        {
            Console.WriteLine($"Setting plug power to {powered}");
            // Library does not support TP-Link's new Klap protoco
            _ = new TPLinkSmartPlug(_ip)
            {
                OutletPowered = powered
            };
            Console.WriteLine($" - DONE");
        }
        catch (Exception e)
        {
            Console.WriteLine($" - FAILED: {e}\n");
            
        }
    }
}