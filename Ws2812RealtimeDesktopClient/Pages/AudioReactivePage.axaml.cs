using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Ws2812RealtimeDesktopClient.ViewModels;

namespace Ws2812RealtimeDesktopClient.Pages
{
    public partial class AudioReactivePage : UserControl
    {
        public AudioReactivePage()
        {
            InitializeComponent();

            DataContext = new AudioReactivePageViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
