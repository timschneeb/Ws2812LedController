﻿using System.ComponentModel;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using Ws2812RealtimeDesktopClient.Models;
using Ws2812RealtimeDesktopClient.Utilities;

namespace Ws2812RealtimeDesktopClient.ViewModels
{
    public class ConnectionPageViewModel : ViewModelBase
    {
        private readonly InfoBadge _udpBadge;
        private readonly InfoBadge _restBadge;
        public ConnectionPageViewModel(InfoBadge udpBadge, InfoBadge restBadge)
        {
            _udpBadge = udpBadge;
            _restBadge = restBadge;
            
            PropertyChanged += OnPropertyChanged;
            RemoteStripManager.Instance.Connected += OnConnected;
            RemoteStripManager.Instance.Disconnected += OnDisconnected;

            IpAddress = SettingsProvider.Instance.IpAddress;
            StripWidth = SettingsProvider.Instance.StripWidth;
            
            UpdateConnectionStatuses();
        }
        
        ~ConnectionPageViewModel()
        {
            PropertyChanged -= OnPropertyChanged;
            RemoteStripManager.Instance.Connected -= OnConnected;
            RemoteStripManager.Instance.Disconnected -= OnDisconnected;
        }

        private async Task Connect_OnClick()
        {
            await RemoteStripManager.Instance.ConnectAsync();
        }
        
        private async Task Disconnect_OnClick()
        {
            await RemoteStripManager.Instance.DisconnectAsync();
        }

        public void UpdateConnectionStatuses()
        {
            UdpStatus = RemoteStripManager.Instance.IsUdpConnected ? "Connected" : "Not connected";
            RestStatus = RemoteStripManager.Instance.IsRestConnected ? "Connected" : "Not connected";
        }
        
        private void OnDisconnected(ProtocolType protocol, DisconnectReason _)
        {
            UpdateConnectionStatuses();
        }

        private void OnConnected(ProtocolType protocol)
        {
            UpdateConnectionStatuses();
        }

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(IsRestStatusSuccessful) or nameof(IsUdpStatusSuccessful))
            {
                UpdateBadges();
            }
        }

        public int StripWidth
        {
            set
            {
                RaiseAndSetIfChanged(ref _stripWidth, value);
                SettingsProvider.Instance.StripWidth = _stripWidth;
                SettingsProvider.Save();
            }
            get => _stripWidth;
        }

        private string _ip = string.Empty;
        [IpAddress(ErrorMessage = "Invalid IP address")]
        public string IpAddress
        {
            get => _ip;
            set
            {
                RaiseAndSetIfChanged(ref _ip, value);
                SettingsProvider.Instance.IpAddress = _ip;
                SettingsProvider.Save();
            }
        }

        private string _restStatus = string.Empty;

        public bool IsRestStatusSuccessful => _restStatus == "Connected";
        public bool IsUdpStatusSuccessful => _udpStatus == "Connected";
        public string RestStatus
        {
            get => _restStatus;
            set
            {
                RaiseAndSetIfChanged(ref _restStatus, value);
                RaisePropertyChanged(nameof(IsRestStatusSuccessful));
            }
        }

        private string _udpStatus = string.Empty;
        private int _stripWidth;

        public string UdpStatus
        {
            get => _udpStatus;
            set
            {
                RaiseAndSetIfChanged(ref _udpStatus, value);
                RaisePropertyChanged(nameof(IsUdpStatusSuccessful));
            }
        }

        public void UpdateBadges()
        {
            Dispatcher.UIThread.Post(() =>
            {
                _restBadge.Classes.Set("Dot", true);
                _udpBadge.Classes.Set("Dot", true);
                _restBadge.Classes.Set("Critical", !IsRestStatusSuccessful);
                _udpBadge.Classes.Set("Critical", !IsUdpStatusSuccessful);
                _restBadge.Classes.Set("Success", IsRestStatusSuccessful);
                _udpBadge.Classes.Set("Success", IsUdpStatusSuccessful);
            }, DispatcherPriority.Layout);
        }
    }
}
