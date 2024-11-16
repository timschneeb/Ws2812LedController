namespace Ws2812LedController.Core.Model;

public enum CancelMode
{
    /**
     * <summary>High priority: Cancel previous effect immediately and clear all queued effects.</summary> 
     */
    Now,
    /**
     * <summary>Semi-high priority: Cancel previous effect on its next cycle and clear all queued effects.</summary> 
     */
    NextCycle,
    /**
     * <summary>Low priority: Do not cancel previous effect. Wait until the previous effect has finished instead.</summary> 
     */
    Enqueue
}