using System.Runtime.InteropServices.ComTypes;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using Ws2812RealtimeDesktopClient.ViewModels;

namespace Ws2812RealtimeDesktopClient.Pages
{
    public partial class ConnectionPage : UserControl
    {
        private InfoBadge _udpBadge;
        private InfoBadge _restBadge;
        
        public ConnectionPage()
        {
            InitializeComponent();
            _udpBadge = this.FindControl<InfoBadge>("UdpBadge");
            _restBadge = this.FindControl<InfoBadge>("RestBadge");
            
            DataContext = new ConnectionPageViewModel(_udpBadge, _restBadge);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
