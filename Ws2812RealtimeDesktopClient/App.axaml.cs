using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Ws2812RealtimeDesktopClient.Utilities;
using Ws2812RealtimeDesktopClient.Views;

namespace Ws2812RealtimeDesktopClient
{
    public class App : Application
    {
        public override void Initialize()
        {
            DataContext = this;
            AvaloniaXamlLoader.Load(this);
        }

        public override async void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
				desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }

        public MiniCommand ExitCommand => new(_ => (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown());
        public event Action? TrayIconClicked; 
        
        private void TrayIcon_OnClicked(object? sender, EventArgs e)
        {
            TrayIconClicked?.Invoke();
        }
    }
}
