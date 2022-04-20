using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Ws2812RealtimeDesktopClient.Controls;
using Ws2812RealtimeDesktopClient.Services;
using Ws2812RealtimeDesktopClient.ViewModels;

namespace Ws2812RealtimeDesktopClient.Pages
{
    public class HomePage : UserControl
    {
        public HomePage()
        {
            this.InitializeComponent();
            DataContext = new HomePageViewModel();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            AddHandler(OptionsDisplayItem.NavigationRequestedEvent, OnDisplayItemNavigationRequested);
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            RemoveHandler(OptionsDisplayItem.NavigationRequestedEvent, OnDisplayItemNavigationRequested);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnDisplayItemNavigationRequested(object sender, RoutedEventArgs e)
        {
            if (e.Source is OptionsDisplayItem odi)
            {
                if (odi.Name == "ConnectionItem")
                {
                    NavigationService.Instance.Navigate(typeof(ConnectionPage));
                }
            }
        }
    }
}
