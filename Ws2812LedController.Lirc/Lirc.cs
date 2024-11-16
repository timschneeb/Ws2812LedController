using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using EvDevSharp;
using EvDevSharp.EventArgs;

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
    public int KeyPressDebounce { set; get; } = 200;
    public int KeyHoldTimeout { set; get; } = 650;

    private int _previousKeyCode = 0;
    private readonly EvDevDevice? _device;
    private readonly Stopwatch _stopwatch = new();

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
        {0x5c, KeyAction.BrightnessUp},
        {0x5d, KeyAction.BrightnessDown},
        {0x41, KeyAction.Next}, // TODO
        {0x40, KeyAction.PowerToggle},
        
        {0x58, KeyAction.Red},
        {0x59, KeyAction.Green},
        {0x45, KeyAction.Blue},
        {0x44, KeyAction.White},
        
        {0x54, KeyAction.DarkOrange}, // TODO
        {0x55, KeyAction.MossGreen},
        {0x49, KeyAction.AzureBlue},
        {0x48, KeyAction.TempWarmHigh}, // TODO temperatures
        
        {0x50, KeyAction.Orange},
        {0x51, KeyAction.LightGreen},
        {0x4d, KeyAction.NavyBlue},
        {0x4c, KeyAction.TempWarmLow},
        
        {0x1c, KeyAction.LightOrange}, // TODO
        {0x1d, KeyAction.Turquoise},
        {0x1e, KeyAction.DarkPurple},
        {0x1f, KeyAction.TempCoolLow},

        {0x18, KeyAction.Yellow},
        {0x19, KeyAction.LightBlue},
        {0x1a, KeyAction.Rose},
        {0x1b, KeyAction.TempCoolHigh},
        
        {0x14, KeyAction.RedUp},
        {0x10, KeyAction.RedDown},
        {0x15, KeyAction.GreenUp},
        {0x11, KeyAction.GreenDown},
        {0x16, KeyAction.BlueUp},
        {0x12, KeyAction.BlueDown},
        {0x0c, KeyAction.Diy1}, // TODO
        {0x0d, KeyAction.Diy2},
        {0x0e, KeyAction.Diy3},
        {0x08, KeyAction.Diy4},
        {0x09, KeyAction.Diy5},
        {0x0a, KeyAction.Diy6},
        
        {0x17, KeyAction.SpeedUp},
        {0x13, KeyAction.SpeedDown},
        
        
                
        //{0x0b, KeyAction.Strobe},
        
        {0x0f, KeyAction.Auto},
        {0x0b, KeyAction.Flash},
        {0x06, KeyAction.Fade3},
        {0x07, KeyAction.Fade7},
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
    
    PowerToggle,
    PowerOff,
    PowerOn,
    BrightnessUp,
    BrightnessDown,
    Next,
    Red,
    DarkOrange,
    LightOrange,
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
    Fade3,
    Fade7,
    TempWarmHigh,
    TempWarmLow,
    TempCoolLow,
    TempCoolHigh,
    
    RedUp,
    RedDown,
    GreenUp,
    GreenDown,
    BlueUp,
    BlueDown,
    Diy1,
    Diy2,
    Diy3,
    Diy4,
    Diy5,
    Diy6,
    
    SpeedUp,
    SpeedDown,
    
    Auto,
}