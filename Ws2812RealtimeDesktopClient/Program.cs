using System.Runtime.ExceptionServices;
using Avalonia;

namespace Ws2812RealtimeDesktopClient
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.FirstChanceException += AppDomainOnFirstChanceException;

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        private static void AppDomainOnFirstChanceException(object? sender, FirstChanceExceptionEventArgs e)
        {
            if (e.Exception is TaskCanceledException or OperationCanceledException)
            {
                return;
            }
            
            Console.WriteLine($"Exception: {e.Exception}");
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .With(new Win32PlatformOptions()
                {
                    UseWindowsUIComposition = true,
                    EnableMultitouch = true, 
                    CompositionBackdropCornerRadius = 8f
                });
    }
}
