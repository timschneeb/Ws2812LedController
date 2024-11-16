namespace EvDevSharp.EventArgs;

public class EvDevEventArgs : System.EventArgs
{
    public EvDevEventArgs(int code, int value) =>
        (Code, Value) = (code, value);

    public int Code { get; set; }
    public int Value { get; set; }
}