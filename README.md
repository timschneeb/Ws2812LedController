# Ws2812LedController
Advanced LED controller software for the raspberry pi. Supports WS28XX LED strips out of the box; can support other strips with simple modifications to the code.

**Work-in-progress**

![client](https://user-images.githubusercontent.com/38386967/200141814-6502947b-f38a-4c10-86c1-ed4b6b8910e2.png)

## Features

Server-side software:
* IR remote support
* Many [animations](https://github.com/ThePBone/Ws2812LedController/tree/master/Ws2812LedController.Core/Effects) included
* Remote control via Web API (HTTP)
* Low-latency/real-time LED projection over the network via UDP
* [Samsung SmartThings support](https://github.com/ThePBone/Ws2812LedController/tree/master/SmartThings)
* Philips Hue compatible (via DiyHue)
* Multiple layers
* Split your strip into multiple virtual strips
    * Duplicate or mirror the LED state of one virtual strip to another
    
LED simulator software:
* Supports all features from the actual server-side software
* Renders to a virtual LED strip on your screen
* Also supports HTTP API and UDP remote projection

Client-side software:
* GUI application
* Audio reactive LED effects (projected remotely over UDP)
* Ambilight effects (projected remotely over UDP) (Work in progress)
* Presets
* Color palettes
