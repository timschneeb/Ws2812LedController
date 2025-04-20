using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.ExceptionServices;
using System.Timers;
using Ws2812LedController.Core;
using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Devices;
using Ws2812LedController.Core.Effects;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.Effects.Firework;
using Ws2812LedController.Core.Effects.PowerEffects;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;
using Ws2812LedController.HueApi;
using Ws2812LedController.Lirc;
using Ws2812LedController.TpLinkPlug;
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
        private static HueApiManager _hueApiManager = null!;
        private static EnetServer _enetServer = null!;
        private static DmxServer.DmxServer _dmxServer = null!;
        private static SynchronizedLedReceiver _syncLedReceiver = null!;
        private static PowerPlug _powerPlug = null!;

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
        
        [DoesNotReturn]
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
            
            _powerPlug = new PowerPlug(new Ref<LedManager>(() => _mgr), "192.168.178.27");
            
            _webApiManager = new WebApiManager(new Ref<LedManager>(() => _mgr));
            _hueApiManager = new HueApiManager(new Ref<LedManager>(() => _mgr));
            _syncLedReceiver = new SynchronizedLedReceiver(new Ref<LedStrip>(() => strip), new Ref<LedManager>(() => _mgr));
            
            _dmxServer = new DmxServer.DmxServer(new Ref<LedManager>(() => _mgr));

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
            _enetServer.ClientConnected += (_, _) => { _dmxServer.DropPackets = true; };
            _enetServer.ClientDisconnected += (_, _) => { _dmxServer.DropPackets = false; };
            
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
            System.Console.WriteLine(e);

            /* Handle colors */
            Color color;
            switch (e.Action)
            {
                case KeyAction.Red:
                    color = Color.Red;
                    break;
                case KeyAction.DarkOrange:
                    color = Color.FromArgb(255, 255, 50, 0);
                    break;
                case KeyAction.Orange:
                    color = Color.FromArgb(255, 255, 100, 0);
                    break;
                case KeyAction.LightOrange:
                    color = Color.FromArgb(255, 255, 120, 0);
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
            
            _mgr.GetFull().SegmentGroup.Clear(Color.FromArgb(0,0,0,0), LayerId.ExclusiveEnetLayer);
            foreach (var segment in _mgr.Segments)
            {
                segment.SegmentGroup.Clear(Color.FromArgb(0,0,0,0), LayerId.ExclusiveEnetLayer);
                
                Static targetEffect;
                if (segment.CurrentEffects[(int)LayerId.BaseLayer] is Static staticEffect)
                {
                    targetEffect = staticEffect;
                }
                else
                {
                    targetEffect = new Static()
                    {
                        Color = color
                    };

                    await segment.SetEffectAsync(targetEffect, blockUntilConsumed: true);
                }
                
                // calculate the step level of every RGB channel for a smooth transition in requested transition time
                var transitionTime = 4 * (17 - (segment.SegmentGroup.Width / 40)); // every extra led add a small delay that need to be counted for transition time match

                targetEffect.Color = color;
        
                for (byte i = 0; i < 3; i++)
                {
                    if (segment.IsPowered)
                    {
                        targetEffect.StepLevel[i] = ((float)targetEffect.Color.ByIndex(i) - targetEffect.CurrentColor[i]) / transitionTime;
                    }
                    else
                    {
                        targetEffect.StepLevel[i] = (float)targetEffect.CurrentColor[i] / transitionTime;
                    }
                }
            }
            return;
            
            HandleSpecialButtons:
            /* Handle non-color buttons */
            switch (e.Action)
            {
                case KeyAction.Next:
                    await _powerPlug.ToggleAsync();
                    break;
                case KeyAction.PowerToggle:
                    await _mgr.PowerAllAsync(!_mgr.IsPowered(), GetCtrls(_currentSegment));
                    break;
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
                    foreach (var segment in _mgr.Segments)
                    {
                        await segment.SetEffectAsync(new FireFlicker());
                    }
                    break;
                case KeyAction.Fade3:
                    foreach (var segment in _mgr.Segments)
                    {
                        await segment.SetEffectAsync(new Rainbow()
                        {
                            Speed = 10000
                        });
                    }
                    break;
                case KeyAction.Fade7:
                    foreach (var segment in _mgr.Segments)
                    {
                        await segment.SetEffectAsync(new RainbowCycle());
                    }
                    break;
                case KeyAction.RedUp:
                    TransformStaticColor(c => Color.FromArgb(Math.Min(255, c.R + 5), c.G, c.B));
                    break;
                case KeyAction.RedDown:
                    TransformStaticColor(c => Color.FromArgb(Math.Max(0, c.R - 5), c.G, c.B));
                    break;
                case KeyAction.GreenUp:
                    TransformStaticColor(c => Color.FromArgb(c.R, Math.Min(255, c.G + 5), c.B));
                    break;
                case KeyAction.GreenDown:
                    TransformStaticColor(c => Color.FromArgb(c.R, Math.Max(0, c.G - 5), c.B));
                    break;
                case KeyAction.BlueUp:
                    TransformStaticColor(c => Color.FromArgb(c.R, c.G, Math.Min(255, c.B + 5)));
                    break;
                case KeyAction.BlueDown:
                    TransformStaticColor(c => Color.FromArgb(c.R, c.G, Math.Max(0, c.B - 5)));
                    break;
                case KeyAction.SpeedUp:
                    foreach (var segment in _mgr.Segments)
                    {
                        if (segment.CurrentEffects[(int)LayerId.BaseLayer] is {} effect)
                        {
                            effect.Speed = Math.Min(50, effect.Speed + 2);
                        }
                    }
                    break;
                case KeyAction.SpeedDown:
                    foreach (var segment in _mgr.Segments)
                    {
                        if (segment.CurrentEffects[(int)LayerId.BaseLayer] is {} effect)
                        {
                            effect.Speed = Math.Max(1, effect.Speed - 2);
                        }
                    }
                    break;
            }
        }

        private static void TransformStaticColor(Func<Color, Color> transform)
        {
            foreach (var segment in _mgr.Segments)
            {
                if (segment.CurrentEffects[(int)LayerId.BaseLayer] is Static staticEffect)
                {
                    // calculate the step level of every RGB channel for a smooth transition in requested transition time
                    var transitionTime = 4 * (17 - (segment.SegmentGroup.Width / 40)); // every extra led add a small delay that need to be counted for transition time match
                    staticEffect.Color = transform(staticEffect.Color);
        
                    for (byte i = 0; i < 3; i++)
                    {
                        if (segment.IsPowered)
                        {
                            staticEffect.StepLevel[i] = ((float)staticEffect.Color.ByIndex(i) - staticEffect.CurrentColor[i]) / transitionTime;
                        }
                        else
                        {
                            staticEffect.StepLevel[i] = (float)staticEffect.CurrentColor[i] / transitionTime;
                        }
                    }
                }
            }
        }
    }
}