using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using Ws2812RealtimeDesktopClient.Dialogs;
using Ws2812RealtimeDesktopClient.Models;
using Ws2812RealtimeDesktopClient.Services;
using Ws2812RealtimeDesktopClient.ViewModels;

namespace Ws2812RealtimeDesktopClient.Pages
{
    public class ReactiveEffectPage : UserControl
    {
        public ReactiveEffectPage()
        {
            InitializeComponent();

            var context = new ReactiveEffectPageViewModel();
            context.AssignEvent += OnAssignEvent;
            DataContext = context;
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        private async void OnAssignEvent()
        {
            await OpenAssignDialog(null);
        }
        
        private async Task<EffectAssignment?> OpenAssignDialog(EffectAssignment? entry)
        {
            if (RemoteStripManager.Instance.SegmentEntries.Count < 1)
            {
                var noSegmentsDialog = new ContentDialog()
                {
                    Content = "Please create a new segment before assigning an effect. Press 'Go to...' to navigate to the segment management page.",
                    Title = "No segments defined",
                    PrimaryButtonText = "Go to...",
                    CloseButtonText = "Cancel"
                };
                noSegmentsDialog.PrimaryButtonClick += (_, _) => NavigationService.Instance.Navigate(typeof(SegmentPage));
                await noSegmentsDialog.ShowAsync();
                return null;
            }

            var dialog = new ContentDialog()
            {
                Title = "Assign effect to segment",
                PrimaryButtonText = "Assign",
                CloseButtonText = "Cancel"
            };

            var viewModel = new AssignEffectDialogViewModel(dialog, entry);
            dialog.Content = new AssignEffectContentDialog()
            {
                DataContext = viewModel
            };
            
            void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
            {
                if (DataContext is AssignEffectDialogViewModel vm)
                {
                    var defer = args.GetDeferral();

                    var isEffectEmpty = viewModel.Effect.Trim().Length < 1;
                    var isNameEmpty = viewModel.Segment.Trim().Length < 1;
                    
                    args.Cancel = isEffectEmpty || isNameEmpty;

                    if (args.Cancel)
                    {
                        var msg = "Unknown validation error";
                        if (isNameEmpty)
                        {
                            msg = "Segment name must not be empty";
                        }
                        else if (isEffectEmpty)
                        {
                            msg = "Effect must be selected";
                        }
                        
                        var resultHint = new ContentDialog()
                        {
                            Content = msg,
                            Title = "Error",
                            PrimaryButtonText = "Close"
                        };

                        _ = resultHint.ShowAsync().ContinueWith(_ => defer.Complete());
                    }
                    else
                    {
                        defer.Complete();
                    }
                }
                
            }

            dialog.PrimaryButtonClick += OnPrimaryButtonClick;
            var result = await dialog.ShowAsync();
            dialog.PrimaryButtonClick -= OnPrimaryButtonClick;
            
            if (result == ContentDialogResult.None)
            {
                return null;
            }
            
            return viewModel.ApplyTo(entry);
        }

    }
}
