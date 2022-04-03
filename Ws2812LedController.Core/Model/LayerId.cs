namespace Ws2812LedController.Core.Model;

/*
 * Current layer map
 * [3] Always-on notification layer; automated & temporary animations
 * [2] Power switch layer; power on/off animations
 * [1] Exclusive ENET layer; reserved for realtime effects via network
 * [0] Base layer; user-controlled space
 */
public enum LayerId : int
{
    BaseLayer = 0,
    ExclusiveEnetLayer = 1,
    PowerSwitchLayer = 2,
    NotificationLayer = 3,
}