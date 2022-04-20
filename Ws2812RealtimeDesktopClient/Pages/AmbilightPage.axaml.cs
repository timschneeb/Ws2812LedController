using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Ws2812RealtimeDesktopClient.ViewModels;

namespace Ws2812RealtimeDesktopClient.Pages
{
    public partial class AmbilightPage : UserControl
    {
        public AmbilightPage()
        {
            InitializeComponent();

            DataContext = new AmbilightPageViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
