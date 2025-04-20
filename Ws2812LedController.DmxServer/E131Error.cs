namespace Ws2812LedController.DmxServer;

public enum E131Error
{
    None,
    NullPtr,
    PreambleSize,
    PostambleSize,
    AcnPid,
    VectorRoot,
    VectorFrame,
    VectorDmp,
    TypeDmp,
    FirstAddrDmp,
    AddrIncDmp
}

public static class E131ErrorExtensions
{
    public static string ToErrorString(this E131Error e) => e switch
        {
            E131Error.None => "Success",
            E131Error.PreambleSize => "Invalid Preamble Size",
            E131Error.PostambleSize => "Invalid Post-amble Size",
            E131Error.AcnPid => "Invalid ACN Packet Identifier",
            E131Error.VectorRoot => "Invalid Root Layer Vector",
            E131Error.VectorFrame => "Invalid Framing Layer Vector",
            E131Error.VectorDmp => "Invalid Device Management Protocol (DMP) Layer Vector",
            E131Error.TypeDmp => "Invalid DMP Address & Data Type",
            E131Error.FirstAddrDmp => "Invalid DMP First Address",
            E131Error.AddrIncDmp => "Invalid DMP Address Increment",
            _ => e.ToString(),
        };
}
