using EvDevSharp.Enums;

namespace EvDevSharp.EventArgs;

public class OnKeyEventArgs : System.EventArgs
{
    public OnKeyEventArgs(EvDevKeyCode key, EvDevKeyValue value) =>
        (Key, Value) = (key, value);

    public EvDevKeyCode Key { get; set; }
    public EvDevKeyValue Value { get; set; }
}