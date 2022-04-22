using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using Ws2812RealtimeDesktopClient.Dialogs;
using Ws2812RealtimeDesktopClient.Models;
using Ws2812RealtimeDesktopClient.Services;
using Ws2812RealtimeDesktopClient.ViewModels;
using ComboBox = Avalonia.Controls.ComboBox;

namespace Ws2812RealtimeDesktopClient.Pages
{
    public class ReactiveEffectPage : UserControl
    {
        public ReactiveEffectPage()
        {
            InitializeComponent();

            var context = new ReactiveEffectPageViewModel();
            context.PropertyChanged += OnPropertyChanged;
            DataContext = context;
        }

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ReactiveEffectPageViewModel.Assignments))
            {
                // Avalonia bug workaround
                var items = ((ReactiveEffectPageViewModel)DataContext!).Assignments;
                this.FindControl<ComboBox>("AssignmentSelector").Items = items.ToArray();
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

    }
}
