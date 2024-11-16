namespace Ws2812LedController.Core.Model;

public class FriendlyNameAttribute : Attribute
{
    public string FriendlyName { get; }
    
    public FriendlyNameAttribute(string friendlyName)
    {
        FriendlyName = friendlyName;
    }
}