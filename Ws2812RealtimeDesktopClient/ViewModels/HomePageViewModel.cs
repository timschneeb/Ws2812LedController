using System.Text.Json;
using Ws2812RealtimeDesktopClient.Models;

namespace Ws2812RealtimeDesktopClient.ViewModels
{
    public class HomePageViewModel : ViewModelBase
    {
        private string _serverConnectionState = "Not connected";

        public HomePageViewModel()
        {
            RemoteStripManager.Instance.ConnectionStateChanged += OnConnectionStateChanged;
        }

        private void OnConnectionStateChanged(ProtocolType obj)
        {
            ServerConnectionState = RemoteStripManager.Instance.IsUdpConnected ||
                                    RemoteStripManager.Instance.IsRestConnected ? "Connected" : "Not connected";
        }

        public string PageHeader => "Home";

        public string ServerConnectionState
        {
            set => RaiseAndSetIfChanged(ref _serverConnectionState, value);
            get => _serverConnectionState;
        }
    }
}
