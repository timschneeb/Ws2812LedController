using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using Ws2812LedController.Core;
using Ws2812LedController.Core.Effects;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;
using Ws2812LedController.UdpServer;
using Ws2812LedController.UdpServer.Model;
using Ws2812LedController.UdpServer.Packets;
using Ws2812LedController.WebApi;
using Color = System.Drawing.Color;

namespace Ws2812LedController.Simulator
{
    public partial class MainWindow : Window
    {
        private readonly LedStrip _strip = new(124, true);
        private readonly LedSegment _segmentA;
        private readonly LedSegment _segmentB;
        private readonly LedManager _mgr = new();

        private const int PixelSize = 16;

        private readonly LedStripControl _control;
        private readonly LedStripControl _controlSegmentA;
        private readonly LedStripControl _controlSegmentB;

        private readonly WebApiManager _webApiManager;
        private readonly EnetServer _enetServer;
        
        public MainWindow()
        {
            var segmentFull = _strip.FullSegment;
            _segmentA = _strip.CreateSegment(0, 40);
            _segmentB = _strip.CreateSegment(40, 40);
            
            InitializeComponent();
            DataContext = this;
            SizeToContent = SizeToContent.WidthAndHeight;
            
            _control = this.FindControl<LedStripControl>("VirtualStrip");
            _controlSegmentA = this.FindControl<LedStripControl>("VirtualSegmentA");
            _controlSegmentB = this.FindControl<LedStripControl>("VirtualSegmentB");
            
            _strip.ActiveCanvasChanged += OnActiveCanvasChanged;
            _strip.Render();

            _control.PixelSize = PixelSize;
            _control.Colors = segmentFull.State;
            _control.InvalidateVisual(); 
            
            _controlSegmentA.PixelSize = PixelSize;
            _controlSegmentB.PixelSize = PixelSize;

            _webApiManager = new WebApiManager(new Ref<LedManager>(() => _mgr));
            _enetServer = new EnetServer();
            _enetServer.Start();
            
            _enetServer.PacketReceived += EnetServerOnPacketReceived;
            
            Init();
        }

        private IPacket? EnetServerOnPacketReceived(IPacket arg)
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
                        var _ = Task.Run((() =>
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
                        }));
                    }
                    break;
            }

            return new ResultPacket()
            {
                SourcePacketId = arg.TypeId,
                Result = 0
            };
        }

        protected override async void OnClosed(EventArgs e)
        {
            await _webApiManager.Terminate();
            base.OnClosed(e);
        }

        private async void Init()
        {
            _mgr.RegisterSegment("full", _strip.FullSegment);
            //_mgr.RegisterSegment("a", _segmentA);
            //_mgr.RegisterSegment("b", _segmentB);

            /*_mgr.Get("b")!.SourceSegment.InvertX = true;
            _mgr.MirrorTo("a", "b");
            _mgr.Get("b");*/

            var ctrl = _mgr.Get("full")!;
            //ctrl.SourceSegment.Mask = new LedMask((color, i, width) => (i % 3 != 0) ? Color.DarkSlateGray : color);

            await ctrl.SetEffectAsync(new RainbowCycle());
            
            /*await ctrl.SetEffectAsync(new Twinkle()
            {
                Speed = 1000,
                CancellationMethod = new CancellationCycleMethod(3),
                BackgroundColor = Color.FromArgb(0,0,0,0)
            }, CancelMode.Now, false, LayerId.NotificationLayer);
            await ctrl.SetEffectAsync(new FadeTo()
            {
                Color = Color.FromArgb(0,0,0,0)
            }, CancelMode.Enqueue, false, LayerId.NotificationLayer);*/

            //await Task.Delay(1000);
            //await ctrl.SetEffectAsync(new RainbowCycle(), CancelMode.NextCycle);
            //await Task.Delay(1000);
            //ctrl.SegmentGroup.RemoveMirror(ctrl.SegmentGroup.Segments[1]);
        }

        private void OnActiveCanvasChanged(object? sender, Color[] e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _control.Colors = e;
                _control.InvalidateVisual();
                _controlSegmentA.Colors = _segmentA.State;
                _controlSegmentA.InvalidateVisual();
                _controlSegmentB.Colors = _segmentB.State;
                _controlSegmentB.InvalidateVisual();
            }, DispatcherPriority.Render);
        }
    }
}