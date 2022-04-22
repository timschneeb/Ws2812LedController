using System.Text.Json;
using FluentAvalonia.UI.Navigation;
using Ws2812RealtimeDesktopClient.Models;
using Ws2812RealtimeDesktopClient.Pages;
using Ws2812RealtimeDesktopClient.Services;

namespace Ws2812RealtimeDesktopClient.ViewModels
{
    public class HomePageViewModel : ViewModelBase
    {
        private string _serverConnectionState = "Not connected";

        public HomePageViewModel()
        {
            RemoteStripManager.Instance.ConnectionStateChanged += OnConnectionStateChanged;
            NavigationService.Instance.Frame.Navigated +=
                (_, _) => RaisePropertyChanged(nameof(ShowNoSegmentsWarning));
        }
        
        
        private void OnConnectionStateChanged(ProtocolType obj)
        {
            ServerConnectionState = RemoteStripManager.Instance.IsUdpConnected ||
                                    RemoteStripManager.Instance.IsRestConnected ? "Connected" : "Not connected";
        }

        public string PageHeader => "Home";

        public bool ShowNoSegmentsWarning => RemoteStripManager.Instance.SegmentEntries.Count < 1;

        public string ServerConnectionState
        {
            set => RaiseAndSetIfChanged(ref _serverConnectionState, value);
            get => _serverConnectionState;
        }
    }
}
