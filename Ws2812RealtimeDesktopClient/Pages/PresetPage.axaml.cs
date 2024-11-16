using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Ws2812RealtimeDesktopClient.ViewModels;

namespace Ws2812RealtimeDesktopClient.Pages
{
    public partial class PresetPage : UserControl
    {
        public PresetPage()
        {
            InitializeComponent();

            DataContext = new PresetPageViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
