using System;
using System.Drawing;
using Ws2812LedController.Core;
using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Effects;
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

            //_mgr.RegisterSegment("b", segmentB);

            //_mgr.Get("b")!.SourceSegment.InvertX = true;
            //_mgr.Get("b")!.SourceSegment.Layers[0].Mask = new LedMask((color, i, _) => (i % 2 == 0) ? Color.Black : color);
            //_mgr.MirrorTo("a", "b");

            var ctrl = _mgr.Get("full")!;
            //ctrl.SourceSegment.Layers[0].Mask = new LedMask((color, i, width) => (i % 2 != 0) ? Color.Black : color);

            ctrl.PowerEffect = new FadePowerEffect();
            //await Task.Delay(2000);
            //await ctrl.SetEffectAsync(new BaseAudioReactiveEffect());
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
                        _mgr.Get("full")!.SegmentGroup.Render();
                    }
                    break;
                case PacketTypeId.PaintInstruction:
                    if (arg is PaintInstructionPacket paint)
                    {
                        if (paint.RenderMode == RenderMode.AnonymousTask)
                        {
                            Task.Run(() =>
                            {
                                switch (paint.PaintInstructionMode)
                                {
                                    case PaintInstructionMode.Full:
                                        for (var i = 0; i < paint.Colors.Length; i++)
                                        {
                                            _mgr.Get("full")!.SegmentGroup.SetPixel(i, paint.Colors[i].ToColor(), paint.Layer);
                                        }
                                        break;
                                    case PaintInstructionMode.Selective:
                                        for (var i = 0; i < paint.Indices.Length; i++)
                                        {
                                            _mgr.Get("full")!.SegmentGroup.SetPixel(paint.Indices[i], paint.Colors[i].ToColor(), paint.Layer);
                                        }
                                        break;
                                }
                                _mgr.Get("full")!.SegmentGroup.Render();
                            });
                            break;
                        }
                        
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
            var segment = _mgr.Get("full");
            if (segment == null)
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
            await segment.SetEffectAsync(new Static()
            {
                Color = color
            });
            return;

            HandleSpecialButtons:
            /* Handle non-color buttons */
            switch (e.Action)
            {
                case KeyAction.PowerOff:
                    await segment.PowerAsync(false);
                    break;
                case KeyAction.PowerOn:
                    await segment.PowerAsync(true);
                    break;
                case KeyAction.BrightnessUp:
                    segment.SegmentGroup.MasterSegment.MaxBrightness =
                        (byte)(segment.SegmentGroup.MasterSegment.MaxBrightness + 30);
                    break;
                case KeyAction.BrightnessDown:
                    segment.SegmentGroup.MasterSegment.MaxBrightness =
                        (byte)(segment.SegmentGroup.MasterSegment.MaxBrightness - 30);
                    break;
                case KeyAction.Flash:
                    await segment.SetEffectAsync(new ColorWipeRandom()
                    {
                        Alternate = true,
                        CancellationMethod = new CancellationCycleMethod(1)
                    }, CancelMode.Now);
                    break;
                case KeyAction.Strobe:
                    await segment.SetEffectAsync(new MultiStrobe());
                    break;
                case KeyAction.Fade:
                    await segment.SetEffectAsync(new Rainbow());
                    break;
                case KeyAction.Smooth:
                    await segment.SetEffectAsync(new RainbowCycle());
                    break;
            }
        }
    }
}