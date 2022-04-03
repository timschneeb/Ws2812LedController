using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using EvDevSharp;

namespace Ws2812LedController.Lirc;

using System;
using System.Text;
using System.Net.Sockets;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class IrReceiver
{
    public static bool IsAvailable => UnixEnvironment.GetEffectiveUserId() == UnixEnvironment.RootUserId &&
                                      RuntimeInformation.IsOSPlatform(OSPlatform.Linux); 
    public static EvDevDevice? CreateDevice(string name)
    {
        return !IsAvailable ? null : EvDevDevice.GetDevices().FirstOrDefault(x => x.Name == name);
    }
    
    public event EventHandler<IrKeyPressEventArgs>? KeyPress;
    public int KeyPressDebounce { set; get; } = 500;
    public int KeyHoldTimeout { set; get; } = 650;

    private int _previousKeyCode = 0;
    private EvDevDevice? _device;
    private Stopwatch _stopwatch = new();

    public IrReceiver() : this(CreateDevice("gpio_ir_recv")) {}
    public IrReceiver(EvDevDevice? dev)
    {
        if (!IsAvailable)
        {
            Console.WriteLine("Lirc.IrReceiver: No root access. IR receiver disabled");
            return;
        }
        
        if (dev == null)
        {
            Console.WriteLine("Lirc.IrReceiver: Device is null. IR receiver disabled");
            return;
        }
        
        _device = dev;
        _device.OnMiscellaneousEvent += OnMiscellaneousEvent;
    }

    private void OnMiscellaneousEvent(object sender, EvDevEventArgs e)
    {
        var elapsed = _stopwatch.Elapsed;
        if (elapsed.TotalMilliseconds < KeyPressDebounce)
        {
            return;
        }
        
        KeyPress?.Invoke(this, new IrKeyPressEventArgs()
        {
            KeyCode = e.Value,
            Repeated = elapsed.TotalMilliseconds < KeyHoldTimeout && _previousKeyCode == e.Value,
            Action = KeyMap.ContainsKey(e.Value) ? KeyMap[e.Value] : KeyAction.Unknown,
            TimeSinceLastEvent = elapsed
        });

        _previousKeyCode = e.Value;
        _stopwatch.Restart();
    }

    public void Start()
    {
        _stopwatch.Start();
        _device?.StartMonitoring();
    }

    public void Stop()
    {
        _device?.StopMonitoring();
        _stopwatch.Stop();
    }

    public Dictionary<int, KeyAction> KeyMap => new()
    {
        {128, KeyAction.BrightnessUp},
        {129, KeyAction.BrightnessDown},
        {130, KeyAction.PowerOff},
        {131, KeyAction.PowerOn},
        
        {132, KeyAction.Red},
        {133, KeyAction.Green},
        {134, KeyAction.Blue},
        {135, KeyAction.White},
        
        {136, KeyAction.Orange},
        {137, KeyAction.Turquoise},
        {138, KeyAction.DarkPurple},
        {139, KeyAction.Flash},

        {140, KeyAction.Yellow},
        {141, KeyAction.LightBlue},
        {142, KeyAction.Purple},
        {143, KeyAction.Strobe},

        {144, KeyAction.LightGreen},
        {145, KeyAction.AzureBlue},
        {146, KeyAction.Pink},
        {147, KeyAction.Fade},

        {148, KeyAction.MossGreen},
        {149, KeyAction.NavyBlue},
        {150, KeyAction.Rose},
        {151, KeyAction.Smooth},
    };
}

public class IrKeyPressEventArgs : EventArgs
{
    public int KeyCode { init; get; }
    public KeyAction Action { init; get; }
    public bool Repeated { init; get; }
    public TimeSpan TimeSinceLastEvent { init; get; }

    public override string ToString()
    {
        return $"KeyCode={KeyCode}; Repeated={Repeated}; TimeSinceLastEvent={TimeSinceLastEvent.TotalMilliseconds}ms; Action={Action}";
    }
}

public enum KeyAction
{
    Unknown,
    
    PowerOff,
    PowerOn,
    BrightnessUp,
    BrightnessDown,
    Red,
    Orange,
    Yellow,
    LightGreen,
    MossGreen,
    Green,
    Turquoise,
    LightBlue,
    AzureBlue,
    NavyBlue,
    Blue,
    DarkPurple,
    Purple,
    Pink,
    Rose,
    White,
    Flash,
    Strobe,
    Fade,
    Smooth
}