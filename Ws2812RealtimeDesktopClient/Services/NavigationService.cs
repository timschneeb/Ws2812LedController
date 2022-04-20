using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using Ws2812RealtimeDesktopClient.Controls;

namespace Ws2812RealtimeDesktopClient.Services
{
    public class NavigationService
    {
        public static NavigationService Instance { get; } = new NavigationService();

        public void SetFrame(Frame f)
        {
            _frame = f;
        }
        
        public void Navigate(Type t)
        {
            _frame.Navigate(t);
        }
        
        private Frame _frame;
    }
}
