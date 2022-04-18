using System;
using System.Drawing;
using Ws2812LedController.Core;
using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Effects;
using Ws2812LedController.Core.Effects.Firework;
using Ws2812LedController.Core.Effects.PowerEffects;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;
using Ws2812LedController.PowerButton;
using Ws2812LedController.Lirc;
using Ws2812LedController.UdpServer;
using Ws2812LedController.UdpServer.Model;
using Ws2812LedController.UdpServer.Packets;
using Ws2812LedController.WebApi;

namespace Ws2812LedController.Console
{
    internal static class Program
    {
        private static LedManager _mgr = null!;
        private static WebApiManager _webApiManager = null!;
        private static EnetServer _enetServer = null!;
        private static SynchronizedLedReceiver _syncLedReceiver = null!;
        
        private static async Task Main(string[] args)
        {
            var lirc = new IrReceiver();
            lirc.KeyPress += LircOnKeyPress;
            lirc.Start();
            
            var strip = new LedStrip(/* bed */ 124 + /* desk_left */ 79 + /* desk_right */ 79 + /* heater */ 81);
            var segmentBed = strip.CreateSegment(0, 124);
            var segmentDeskL = strip.CreateSegment(124, 79);
            var segmentDeskR = strip.CreateSegment(124+79, 79);
            var segmentHeater = strip.CreateSegment(124+79*2, 81);
            
            _mgr = new LedManager();
            _mgr.RegisterSegment("full", strip.FullSegment);
            _mgr.RegisterSegment("bed", segmentBed);
            _mgr.RegisterSegment("desk_left", segmentDeskL);
            _mgr.RegisterSegment("desk_right", segmentDeskR);
            _mgr.RegisterSegment("heater", segmentHeater);
            
            _webApiManager = new WebApiManager(new Ref<LedManager>(() => _mgr));
            _syncLedReceiver = new SynchronizedLedReceiver(new Ref<LedStrip>(() => strip), new Ref<LedManager>(() => _mgr));

            foreach (var segment in _mgr.Segments)
            {
                segment.PowerEffect = new WipePowerEffect();
            }
            
            var ctrl = _mgr.Get("full")!;
            await ctrl.SetEffectAsync(new RainbowCycle());
           /* await ctrl.SetEffectAsync(new LarsonScanner()
            {
                Speed = 1000,
                CancellationMethod = new CancellationCycleMethod(3),
                BackgroundColor = Color.FromArgb(0,0,0,0)
            }, CancelMode.Now, false, LayerId.NotificationLayer);
            await ctrl.SetEffectAsync(new FadeTo()
            {
                Color = Color.FromArgb(0,0,0,0)
            }, CancelMode.Enqueue,  false, LayerId.NotificationLayer);
*/
            var button = new PowerToggleButton(27);
            button.PowerStateChanged += PowerButton_OnPowerStateChanged;

            _enetServer = new EnetServer();
            _enetServer.Start();
            
            _enetServer.PacketReceived += EnetServerOnPacketReceived;
            
            while(true)
            {
                await Task.Delay(2000);
            }
            
        }

        private static IPacket? EnetServerOnPacketReceived(IPacket arg)
        {
            switch (arg.TypeId)
            {
                case PacketTypeId.Result:
                    break;
                case PacketTypeId.SetGetRequest:
                    if (arg is SetGetRequestPacket sg)
                    {
                        switch (sg.Key)
                        {
                            case SetGetRequestId.Brightness:
                                if (sg.Action == SetGetAction.Get)
                                {
                                    return new ResultPacket()
                                    {
                                        Result = _mgr.Get("full")!.SourceSegment.MaxBrightness,
                                        SourcePacketId = sg.TypeId
                                    };
                                }
                                _mgr.Get("full")!.SourceSegment.MaxBrightness = (byte)sg.Value;
                                break;
                            case SetGetRequestId.Power:
                                if (sg.Action == SetGetAction.Get)
                                {
                                    return new ResultPacket()
                                    {
                                        Result = (byte)(_mgr.Get("full")!.CurrentState == PowerState.On ? 1 : 0),
                                        SourcePacketId = sg.TypeId
                                    };
                                }
                                var _ = _mgr.Get("full")!.PowerAsync(sg.Value == 1);
                                break;
                        }
                    }
                    break;
                case PacketTypeId.ClearCanvas:
                    if (arg is ClearCanvasPacket clr)
                    {
                        _mgr.Get("full")!.SegmentGroup.Clear(clr.Color.ToColor(), clr.Layer);
                    }
                    break;
                case PacketTypeId.PaintInstruction:
                    if (arg is PaintInstructionPacket paint)
                    {
                        _syncLedReceiver.EnqueuePacket(paint);
                    }
                    break;
            }

            return null;
        }

        private static async void PowerButton_OnPowerStateChanged(object? sender, bool e)
        {
            var segment = _mgr.Get("full");
            if (segment == null)
            {
                return;
            }

            System.Console.WriteLine("Power button toggled: " + e);
            await segment.PowerAsync(segment.CurrentState == PowerState.Off);
        }

        private static async void LircOnKeyPress(object? sender, IrKeyPressEventArgs e)
        {
            var fullSeg = _mgr.Get("full");
            if (fullSeg == null)
            {
                return;
            }

            System.Console.WriteLine(e);
            
            /* Handle colors */
            Color color;
            switch (e.Action)
            {
                case KeyAction.Red:
                    color = Color.Red;
                    break;
                case KeyAction.Orange:
                    color = Color.FromArgb(255, 255, 50, 0);
                    break;
                case KeyAction.Yellow:
                    color = Color.Yellow;
                    break;
                case KeyAction.LightGreen:
                    color = Color.LawnGreen;
                    break;
                case KeyAction.MossGreen:
                    color = Color.ForestGreen;
                    break;
                case KeyAction.Green:
                    color = Color.Green;
                    break;
                case KeyAction.Turquoise:
                    color = Color.Turquoise;
                    break;
                case KeyAction.LightBlue:
                    color = Color.DeepSkyBlue;
                    break;
                case KeyAction.AzureBlue:
                    color = Color.DodgerBlue;
                    break;
                case KeyAction.NavyBlue:
                    color = Color.RoyalBlue;
                    break;
                case KeyAction.Blue:
                    color = Color.Blue;
                    break;
                case KeyAction.DarkPurple:
                    color = Color.MediumPurple;
                    break;
                case KeyAction.Purple:
                    color = Color.Purple;
                    break;
                case KeyAction.Pink:
                    color = Color.DeepPink;
                    break;
                case KeyAction.Rose:
                    color = Color.MediumVioletRed;
                    break;
                case KeyAction.White:
                    color = Color.White;
                    break;
                default:
                    goto HandleSpecialButtons;
            }
            
            _mgr.Get("full")?.SegmentGroup.Clear(Color.FromArgb(0,0,0,0), LayerId.ExclusiveEnetLayer);
            await fullSeg.SetEffectAsync(new Static()
            {
                Color = color
            });
            return;

            HandleSpecialButtons:
            /* Handle non-color buttons */
            switch (e.Action)
            {
                case KeyAction.PowerOff:
                    await _mgr.PowerAllAsync(false, "bed", "desk_left", "desk_right", "heater");
                    break;
                case KeyAction.PowerOn:
                    await _mgr.PowerAllAsync(true, "bed", "desk_left", "desk_right", "heater");
                    break;
                case KeyAction.BrightnessUp:
                    fullSeg.SegmentGroup.MasterSegment.MaxBrightness =
                        (byte)(fullSeg.SegmentGroup.MasterSegment.MaxBrightness + 30);
                    break;
                case KeyAction.BrightnessDown:
                    fullSeg.SegmentGroup.MasterSegment.MaxBrightness =
                        (byte)(fullSeg.SegmentGroup.MasterSegment.MaxBrightness - 30);
                    break;
                case KeyAction.Flash:
                    await fullSeg.SetEffectAsync(new Firework()
                    {
                        Speed = 5000,
                        FadeRate = FadeRate.None
                    });
                    break;
                case KeyAction.Strobe:
                    await fullSeg.SetEffectAsync(new FireFlicker());
                    break;
                case KeyAction.Fade:
                    await fullSeg.SetEffectAsync(new Rainbow()
                    {
                        Speed = 10000
                    });
                    break;
                case KeyAction.Smooth:
                    await fullSeg.SetEffectAsync(new RainbowCycle());
                    break;
            }
        }
    }
}