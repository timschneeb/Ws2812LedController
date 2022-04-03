#!/bin/sh
dotnet publish -r linux-arm --no-self-contained
pushd ./Ws2812LedController.Console/bin/Debug/net6.0/linux-arm/publish
scp -r ./* pi@192.168.178.57:/home/pi/Ws2812LedController
popd
