using System.Diagnostics;
using Avalonia.Collections;
using Avalonia.Media;
using Ws2812LedController.Core;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.Model;
using Ws2812LedController.UdpServer;
using Ws2812LedController.UdpServer.Packets;
using Ws2812RealtimeDesktopClient.Models;
using Ws2812RealtimeDesktopClient.Utilities;
using Color = System.Drawing.Color;

namespace Ws2812RealtimeDesktopClient;

public class RemoteStripManager
{
    private static readonly Lazy<RemoteStripManager> Lazy =
        new(() => new RemoteStripManager());

    private RemoteLedCanvas? _canvas;
    private LedManager? _mgr;
    private LedStrip? _remote;
    private EnetClient? _client;
    public AvaloniaList<SegmentEntry> SegmentEntries { get; } = new(SettingsProvider.Instance.Segments ?? Array.Empty<SegmentEntry>());
    public AvaloniaList<EffectAssignment> EffectAssignments { get; }

    public static RemoteStripManager Instance => Lazy.Value;

    public event Action<ProtocolType>? Connected;
    public event Action<ProtocolType, DisconnectReason>? Disconnected;
    public event Action<ProtocolType>? ConnectionStateChanged;

   
    public bool IsUdpConnected { private set; get; } = false;
    public bool IsRestConnected { private set; get; } = false;
    private RemoteStripManager()
    {
        // Inflate unsaved property information
        var savedAssign = SettingsProvider.Instance.ReactiveEffectAssignments ?? Array.Empty<EffectAssignment>();

        for (var i = 0; i < savedAssign.Length; i++)
        {
            var desc =
                ReactiveEffectDescriptorList.Instance.Descriptors.FirstOrDefault(x => x.Name == savedAssign[i].EffectName);
            if (desc == null) continue;

            savedAssign[i].Properties = new AvaloniaList<PropertyRow>(savedAssign[i].Properties.ToList().Where(x => x != null!));
            foreach (var prop in desc.Properties)
            {
                var propInfo = savedAssign[i].Properties.FirstOrDefault(x => x.Name == prop.Name);
                if (propInfo != null)
                {
                    Console.WriteLine(prop.Name + "=" + prop.Value);
                    propInfo.Update(prop, true);
                }
                else
                {
                    savedAssign[i].Properties.Add(new PropertyRow(prop));
                }
            }
        }

        EffectAssignments = new AvaloniaList<EffectAssignment>(savedAssign);
        
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
        await SyncEffectAssignmentsAsync(EffectAssignments.ToArray());

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

    #region Effect management
    public async Task SyncEffectAssignmentsAsync(EffectAssignment[] entries)
    {
        if (_mgr == null)
        {
            Console.WriteLine("SyncEffectAssignmentsAsync: LedManager is null");
        }
        else
        {
            /*foreach (var assignment in EffectAssignments.ToList())
            {
                await DeleteEffectAssignmentAsync(assignment.SegmentName);
            }*/
        }
        
        foreach(var entry in entries)
        {
            await AddEffectAssignmentAsync(entry);
        }
    }

    public void UpdateEffectProperties(EffectAssignment entry)
    {
        Console.WriteLine("UpdateEffectProperties " + entry.SegmentName);
        var ctrl = _mgr?.Get(entry.SegmentName);
        if (ctrl != null)
        {
            Console.WriteLine("ctrl != null");
            foreach (var property in entry.Properties)
            {
                var effectType = ctrl.CurrentEffects[0]?.GetType();
                var propertyInfo = effectType?.GetProperty(property.Name);
                if (propertyInfo == null)
                {
                    Console.WriteLine("NOT FOUND: "+property.Name);
                    return;
                }
                Console.WriteLine("Set value: "+property.Name + " to " + property.Value);
                propertyInfo.SetValue(ctrl.CurrentEffects[0], property.Value);
            }
        }
    }

    public async Task AddEffectAssignmentAsync(EffectAssignment entry)
    {
        var desc = ReactiveEffectDescriptorList.Instance.Descriptors.FirstOrDefault(x => x.Name == entry.EffectName);
        Debug.Assert(desc != null, "Effect descriptor not found");
        var effect = (BaseEffect?)Activator.CreateInstance(desc.InternalType);
        Debug.Assert(effect != null, "Failed to create effect instance");
        
        var newProperties = new AvaloniaList<PropertyRow>();
        foreach (var property in desc.Properties)
        {
            if (entry.Properties != null && entry.Properties.Any(x => x.Name == property.Name))
            {
                newProperties.Add(entry.Properties.First(x => x.Name == property.Name));
            }
            else
            {
                newProperties.Add(new PropertyRow(property));
            }
        }
        entry.Properties = newProperties;
        
        if (EffectAssignments.Any(x => x.SegmentName == entry.SegmentName))
        {
            /* Reset canvas segment without actually deleting */
            await DeleteEffectAssignmentAsync(entry.SegmentName, true);
            EffectAssignments[EffectAssignments.IndexOf(EffectAssignments.First(x => x.SegmentName == entry.SegmentName))] = entry;
        }
        else
        {
            EffectAssignments.Add(entry);
        }
        
        if (_mgr == null)
        {
            Console.WriteLine("AddSegment: LedManager or LedStrip is null");
        }
        else
        {
            var ctrl = _mgr.Get(entry.SegmentName);
            if (ctrl != null)
            {
                await ctrl.SetEffectAsync(effect, blockUntilConsumed: true);
                UpdateEffectProperties(entry);
            }
        }
    }

    public async Task DeleteEffectAssignmentAsync(string segment, bool keepInList = false)
    {
        if (!keepInList)
        {
            EffectAssignments.Where(x => x.SegmentName == segment).ToList()
                .ForEach(x => EffectAssignments.Remove(x));
        }
        
        if (_mgr == null)
        {
            Console.WriteLine("DeleteSegmentAsync: LedManager is null");
            return;
        }

        var ctrl = _mgr.Get(segment);
        if (ctrl != null)
        {
            await ctrl.CancelLayerAsync(LayerId.BaseLayer);
            ctrl.SegmentGroup.Clear(Color.Black, LayerId.BaseLayer);
        }
    }
    #endregion

    #region Segment management
    /** Segments are implicitly reset by this function */
    public async Task SyncSegmentsAsync(SegmentEntry[] entries)
    {
        if (_mgr == null)
        {
            Console.WriteLine("SyncSegmentsAsync: LedManager is null");
        }
        else
        {
             await _mgr.UnregisterAllSegmentsAsync(_remote);
        }
        
        foreach(var entry in entries)
        {
            AddSegment(entry);
        }
    }

    public async Task UpdateSegmentAsync(SegmentEntry entry, string originalName)
    {
        var oldEntry = SegmentEntries.FirstOrDefault(x => x.Name == originalName);
        var oldEntryIdx = oldEntry == null ? -1 : SegmentEntries.IndexOf(oldEntry);
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

        if(SegmentEntries[oldEntryIdx].Start != entry.Start || SegmentEntries[oldEntryIdx].Width != entry.Width)
        {
            await _mgr.UnregisterSegmentAsync(originalName, _remote);
            _mgr.RegisterSegment(entry.Name, _remote, entry.Start, entry.Width);
        }
        else if (SegmentEntries[oldEntryIdx].Name != entry.Name)
        {
            _mgr.RenameSegment(originalName, entry.Name);
        }

        var seg = _mgr.Get(entry.Name);
        Debug.Assert(seg != null, "LedSegmentController must not be null at this point");
        seg.SourceSegment.InvertX = entry.InvertX;

        ReattachMirrors();
        
        SKIP_SEG_UPDATE:
        SegmentEntries[oldEntryIdx] = entry;
    }

    public void AddSegment(SegmentEntry entry)
    {
        if (SegmentEntries.Any(x => x.Name == entry.Name))
        {
            SegmentEntries[SegmentEntries.IndexOf(SegmentEntries.First(x => x.Name == entry.Name))] = entry;
        }
        else
        {
            SegmentEntries.Add(entry);
        }
        
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
        EffectAssignments.Where(x => x.SegmentName == name).ToList()
            .ForEach(x => EffectAssignments.Remove(x));        
        
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