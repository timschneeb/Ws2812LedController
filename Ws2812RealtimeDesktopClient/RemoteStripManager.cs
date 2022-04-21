using System.Diagnostics;
using Ws2812LedController.Core;
using Ws2812LedController.Core.Model;
using Ws2812LedController.UdpServer;
using Ws2812LedController.UdpServer.Packets;
using Ws2812RealtimeDesktopClient.Models;
using Ws2812RealtimeDesktopClient.Utilities;

namespace Ws2812RealtimeDesktopClient;

public class RemoteStripManager
{
    private static readonly Lazy<RemoteStripManager> Lazy =
        new(() => new RemoteStripManager());

    private RemoteLedCanvas? _canvas;
    private LedManager? _mgr;
    private LedStrip? _remote;
    private EnetClient? _client;
    public List<SegmentEntry> SegmentEntries { get; } = SettingsProvider.Instance.Segments.ToList();

    public static RemoteStripManager Instance => Lazy.Value;

    public event Action<ProtocolType>? Connected;
    public event Action<ProtocolType, DisconnectReason>? Disconnected;
    public event Action<ProtocolType>? ConnectionStateChanged;

    public bool IsUdpConnected { private set; get; } = false;
    public bool IsRestConnected { private set; get; } = false;
    private RemoteStripManager()
    {
        Connected += OnConnected;
        Disconnected += OnDisconnected;
    }
    
    #region Connection methods
    public async Task ConnectAsync(string? ip = null, int? maxLength = null)
    {
        await ConnectUdpAsync(ip, maxLength);
    }

    public async Task DisconnectAsync()
    {
        await DisconnectUdpAsync();
    }
    
    public async Task ConnectUdpAsync(string? ip = null, int? maxLength = null)
    {
        ip ??= SettingsProvider.Instance.IpAddress;
        maxLength ??= SettingsProvider.Instance.StripWidth;
        
        await DisconnectUdpAsync(false);
        
        _canvas = new RemoteLedCanvas(LayerId.ExclusiveEnetLayer, 0, maxLength.Value, RenderMode.ManagedTask);
        _mgr = new LedManager();
        _remote = new LedStrip(new RemoteLedStrip(_canvas));
        await SyncSegmentsAsync(SegmentEntries.ToArray());
        
        _client = new EnetClient(ip);
        _client.Connected += OnUdpConnected;
        _client.Timeout += OnUdpTimeout;
        _client.Connect();
    }
    
    public async Task DisconnectUdpAsync(bool clearCanvas = true)
    {
        if (_canvas != null)
        {
            _canvas.NewPacketAvailable -= OnUdpPacketReady;
        }

        if (clearCanvas)
        {
            var clearPkg = new ClearCanvasPacket()
            {
                Color = 0x00000000,
                Layer = LayerId.ExclusiveEnetLayer
            };
            _client?.Enqueue(clearPkg);
            await Task.Delay(50);
        }

        if (_remote != null)
        {
            await _remote.CancelAsync();
            _remote.Dispose();
            _remote = null;
        }

        if (_client != null)
        {
            _client.Connected -= OnUdpConnected;
            _client.Timeout -= OnUdpTimeout;
            await _client.DisconnectAsync();
            _client = null;
        }
        
        Disconnected?.Invoke(ProtocolType.Udp, DisconnectReason.User);
    }
    #endregion

    #region Udp event receivers
    private void OnUdpPacketReady(object? sender, IPacket packet)
    {
        _client?.Enqueue(packet);
    }

    private void OnUdpTimeout(object? sender, EventArgs e)
    {
        Disconnected?.Invoke(ProtocolType.Udp, DisconnectReason.Timeout);
    }

    private void OnUdpConnected(object? sender, EventArgs e)
    {
        if (_canvas != null)
        {
            _canvas.NewPacketAvailable += OnUdpPacketReady;
        }
        Connected?.Invoke(ProtocolType.Udp);
    }
    #endregion
    
    public async void DoAction(QuickAction action)
    {
        switch (action)
        {
            case QuickAction.Play:
                await ConnectUdpAsync();
                break;
            case QuickAction.Pause:
                await DisconnectUdpAsync();
                break;
            case QuickAction.Restart:
                break;
        }
    }

    public async void SwitchMode(EffectModes mode)
    {
        switch (mode)
        {
            case EffectModes.Normal:
                await DisconnectUdpAsync();
                break;
            case EffectModes.Reactive:
                await ConnectUdpAsync();
                break;
        }
    }

    #region Segment management
    /** Segments are implicitly reset by this function */
    public async Task SyncSegmentsAsync(SegmentEntry[] entries)
    {
        SegmentEntries.Clear();

        if (_mgr == null)
        {
            Console.WriteLine("SyncSegmentsAsync: LedManager is null");
            return;
        }
        
        await _mgr.UnregisterAllSegmentsAsync(_remote);
        foreach(var entry in entries)
        {
            AddSegment(entry);
        }
    }

    public async Task UpdateSegmentAsync(SegmentEntry entry, string originalName)
    {
        var oldEntryIdx = SegmentEntries.FindIndex(x => x.Name == originalName);
        if (oldEntryIdx == -1)
        {
            AddSegment(entry);
            return;
        }

        if (_mgr == null || _remote == null)
        {
            Console.WriteLine("UpdateSegment: LedManager or LedStrip is null");
            goto SKIP_SEG_UPDATE;
        }

        var seg = _mgr.Get(originalName);
        Debug.Assert(seg != null, "LedSegmentController must not be null at this point");
        
        if(SegmentEntries[oldEntryIdx].Start != entry.Start || SegmentEntries[oldEntryIdx].Width != entry.Width)
        {
            await _mgr.UnregisterSegmentAsync(originalName, _remote);
            _mgr.RegisterSegment(entry.Name, _remote, entry.Start, entry.Width);
        }
        else if (SegmentEntries[oldEntryIdx].Name != entry.Name)
        {
            _mgr.RenameSegment(originalName, entry.Name);
        }

        if(SegmentEntries[oldEntryIdx].InvertX != entry.InvertX)
        {
            seg.SourceSegment.InvertX = entry.InvertX;
        }
        
        ReattachMirrors();
        
        SKIP_SEG_UPDATE:
        SegmentEntries[oldEntryIdx] = entry;
    }

    public void AddSegment(SegmentEntry entry)
    {
        SegmentEntries.Add(entry);
        
        if (_mgr == null || _remote == null)
        {
            Console.WriteLine("AddSegment: LedManager or LedStrip is null");
            return;
        }

        _mgr.RegisterSegment(entry.Name, _remote, entry.Start, entry.Width);
        _mgr.Get(entry.Name)!.SourceSegment.InvertX = entry.InvertX;
        ReattachMirrors();
    }

    public async Task DeleteSegmentAsync(string name)
    {
        SegmentEntries.RemoveAll(x => x.Name == name);
        
        if (_mgr == null)
        {
            Console.WriteLine("DeleteSegmentAsync: LedManager is null");
            return;
        }

        ReattachMirrors();
        await _mgr.UnregisterSegmentAsync(name, _remote);
    }

    public void ReattachMirrors()
    {
        _mgr?.RemoveAllMirrors();
        foreach (var entry in SegmentEntries)
        {
            entry.MirroredTo.ToList().ForEach(x => _mgr?.MirrorTo(entry.Name, x));
        }
    }
    #endregion
    
    #region Connection event receivers
    private void OnConnected(ProtocolType protocol)
    {
        switch (protocol)
        {
            case ProtocolType.Udp:
                IsUdpConnected = true;
                break;
            case ProtocolType.Rest:
                IsRestConnected = true;
                break;
        }
        ConnectionStateChanged?.Invoke(protocol);
    }

    private void OnDisconnected(ProtocolType protocol, DisconnectReason _)
    {
        switch (protocol)
        {
            case ProtocolType.Udp:
                IsUdpConnected = false;
                break;
            case ProtocolType.Rest:
                IsRestConnected = false;
                break;
        }
        ConnectionStateChanged?.Invoke(protocol);
    }
    #endregion
}