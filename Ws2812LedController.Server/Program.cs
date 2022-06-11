using System;
using System.Drawing;
using System.Runtime.ExceptionServices;
using System.Timers;
using Ws2812LedController.Core;
using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Effects;
using Ws2812LedController.Core.Effects.Firework;
using Ws2812LedController.Core.Effects.PowerEffects;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Strips;
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

        private const int ResetTimerTimeout = 30 * 1000;
        private static System.Timers.Timer _currentSegmentResetTimer = new(ResetTimerTimeout);
        private static int _currentSegment = 0;
        private static CancellationTokenSource _currentSegmentMaskReset = new();
        private static LedSegmentController[] GetCtrls(int idx)
        {
            return idx switch
            {
                1 => new[] { _mgr.Get("bed")! },
                2 => new[] { _mgr.Get("desk_left")!, _mgr.Get("desk_right")! },
                3 => new[] { _mgr.Get("heater")! },
                _ => new[] { _mgr.Get("desk_left")!, _mgr.Get("desk_right")!, _mgr.Get("heater")!, _mgr.Get("bed")!}
            };
        }
        
        private static async Task Main(string[] args)
        {
            AppDomain.CurrentDomain.FirstChanceException += AppDomainOnFirstChanceException;

            var lirc = new IrReceiver();
            lirc.KeyPress += LircOnKeyPress;
            lirc.Start();

            var device = new Ws2812Device( /* bed */ 124 + /* desk_left */ 79 + /* desk_right */ 79 + /* heater */ 81);
            var strip = new LedStrip(device);
            var segmentBed = strip.CreateSegment(0, 124);
            var segmentDeskL = strip.CreateSegment(124, 79);
            var segmentDeskR = strip.CreateSegment(124+79, 79);
            var segmentHeater = strip.CreateSegment(124+79*2, 81);
            
            _mgr = new LedManager(new Ref<LedStrip>(() => strip));
            _mgr.RegisterSegment("bed", segmentBed);
            _mgr.RegisterSegment("desk_left", segmentDeskL);
            _mgr.RegisterSegment("desk_right", segmentDeskR);
            _mgr.RegisterSegment("heater", segmentHeater);
            await _mgr.PowerAllAsync(false);
            
            _webApiManager = new WebApiManager(new Ref<LedManager>(() => _mgr));
            _syncLedReceiver = new SynchronizedLedReceiver(new Ref<LedStrip>(() => strip), new Ref<LedManager>(() => _mgr));

            foreach (var segment in _mgr.Segments)
            {
                segment.PowerEffect = new WipePowerEffect();
            }

            var ctrl = _mgr.GetFull()!;
            await ctrl.SetEffectAsync(new Static
            {
                Color = Color.FromArgb(255, 255, 50, 0)
            }, noPowerOn: true);
            
            //await ctrl.SetEffectAsync(new RainbowCycle());
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
            //var button = new PowerToggleButton(27);
            //button.PowerStateChanged += PowerButton_OnPowerStateChanged;

            _enetServer = new EnetServer();
            _enetServer.Start();
            
            _enetServer.PacketReceived += EnetServerOnPacketReceived;
            
            while(true)
            {
                await Task.Delay(2000);
            }
            
        }

        private static void AppDomainOnFirstChanceException(object? sender, FirstChanceExceptionEventArgs e)
        {
            if (e.Exception is TaskCanceledException or OperationCanceledException)
            {
                return;
            }
            
            System.Console.WriteLine($"Exception: {e.Exception}");
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
                                        Result = _mgr.GetFull()!.SourceSegment.MaxBrightness,
                                        SourcePacketId = sg.TypeId
                                    };
                                }
                                _mgr.GetFull()!.SourceSegment.MaxBrightness = (byte)sg.Value;
                                break;
                            case SetGetRequestId.Power:
                                if (sg.Action == SetGetAction.Get)
                                {
                                    return new ResultPacket()
                                    {
                                        Result = (byte)(_mgr.GetFull()!.CurrentState == PowerState.On ? 1 : 0),
                                        SourcePacketId = sg.TypeId
                                    };
                                }
                                var _ = _mgr.GetFull()!.PowerAsync(sg.Value == 1);
                                break;
                        }
                    }
                    break;
                case PacketTypeId.ClearCanvas:
                    if (arg is ClearCanvasPacket clr)
                    {
                        _mgr.GetFull()!.SegmentGroup.Clear(clr.Color.ToColor(), clr.Layer);
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
            var segment = _mgr.GetFull();
            if (segment == null)
            {
                return;
            }

            System.Console.WriteLine("Power button toggled: " + e);
            await segment.PowerAsync(segment.CurrentState == PowerState.Off);
        }

        private static async void LircOnKeyPress(object? sender, IrKeyPressEventArgs e)
        {
            var fullSeg = _mgr.GetFull();
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
            
            _mgr.GetFull()?.SegmentGroup.Clear(Color.FromArgb(0,0,0,0), LayerId.ExclusiveEnetLayer);
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
                    await _mgr.PowerAllAsync(false, GetCtrls(_currentSegment));
                    break;
                case KeyAction.PowerOn:
                    await _mgr.PowerAllAsync(true, GetCtrls(_currentSegment));
                    break;
                case KeyAction.BrightnessUp:
                    var bu = (byte)(GetCtrls(_currentSegment).First().SourceSegment.MaxBrightness + 17);
                    foreach (var ctrl in GetCtrls(_currentSegment))
                    {
                        ctrl.SourceSegment.MaxBrightness = bu;
                    }
                    
                    //fullSeg.SegmentGroup.MasterSegment.MaxBrightness =
                    //    (byte)(fullSeg.SegmentGroup.MasterSegment.MaxBrightness + 20);
                    break;
                case KeyAction.BrightnessDown:
                    var bd = (byte)(GetCtrls(_currentSegment).First().SourceSegment.MaxBrightness - 17);
                    foreach (var ctrl in GetCtrls(_currentSegment))
                    {
                        ctrl.SourceSegment.MaxBrightness = bd;
                    }
                    
                    //fullSeg.SegmentGroup.MasterSegment.MaxBrightness =
                    //    (byte)(fullSeg.SegmentGroup.MasterSegment.MaxBrightness - 20);
                    break;
                case KeyAction.Flash:
                    _currentSegment++;
                    if (_currentSegment > 3)
                    {
                        _currentSegment = 0;
                    }
                    
                    _currentSegmentResetTimer.Stop();
                    _currentSegmentResetTimer.Close();
                    _currentSegmentResetTimer = new System.Timers.Timer(ResetTimerTimeout);
                    _currentSegmentResetTimer.Elapsed += (_, _) => _currentSegment = 0;
                    _currentSegmentResetTimer.Start();
                    
                    void SetSelected(bool on, int segId)
                    {
                        foreach (var ctrl in GetCtrls(segId))
                        {
                            ctrl.SourceSegment.Layers[(int)LayerId.NotificationLayer].Mask =
                                on ? new LedMask((_, _, _) => Color.FromArgb(40,40,40)) : null;
                        }
                    }

                    var segId = _currentSegment;
                    _currentSegmentMaskReset.Cancel();
                    _currentSegmentMaskReset.Token.WaitHandle.WaitOne(100);
                    for (var i = 0; i <= 3; i++)
                    {
                        SetSelected(false, i);
                    }
                    SetSelected(true, segId);
                    _currentSegmentMaskReset.Dispose();
                    _currentSegmentMaskReset = new CancellationTokenSource();
                    try
                    {
                        var token = _currentSegmentMaskReset.Token;
                        await Task.Delay(1500, token).ContinueWith((_) =>
                        {
                            if (!token.IsCancellationRequested)
                            {
                                SetSelected(false, segId);
                            }
                        }, token);
                    }
                    catch(TaskCanceledException){}
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