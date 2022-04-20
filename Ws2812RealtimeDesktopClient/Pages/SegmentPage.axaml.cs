using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using Ws2812RealtimeDesktopClient.Dialogs;
using Ws2812RealtimeDesktopClient.Models;
using Ws2812RealtimeDesktopClient.Utilities;
using Ws2812RealtimeDesktopClient.ViewModels;

namespace Ws2812RealtimeDesktopClient.Pages
{
    public class SegmentPage : UserControl
    {
        public SegmentPage()
        {
            InitializeComponent();
            var context = new SegmentPageViewModel();
            context.AddEvent += OnAddEvent;
            context.EditEvent += OnEditEvent;
            context.SegmentChanged += OnSegmentChanged;
            context.Segments = new AvaloniaList<SegmentEntry>(SettingsProvider.Instance.Segments);

            DataContext = context;
        }

        private void OnSegmentChanged()
        {
            if (DataContext is SegmentPageViewModel vm)
            {
                SettingsProvider.Instance.Segments = vm.Segments.ToArray();
            }
        }

        private async Task<SegmentEntry?> OpenEditDialog(SegmentEntry? entry)
        {
            var dialog = new ContentDialog()
            {
                Title = entry == null ? "Create new segment" : "Edit segment",
                PrimaryButtonText = entry == null ? "Create" : "Save",
                CloseButtonText = "Cancel"
            };

            var viewModel = new EditSegmentDialogViewModel(dialog, entry);
            dialog.Content = new EditSegmentContentDialog()
            {
                DataContext = viewModel
            };
            
            void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
            {
                if (DataContext is SegmentPageViewModel vm)
                {
                    var defer = args.GetDeferral();

                    var isNameExisting = entry == null /* create mode */ ? vm.Segments.Any(x => x.Name == viewModel.Name) :
                        vm.Segments.Any(x => x.Name == viewModel.Name && entry.Start != x.Start && entry.Width != x.Width) ;
                    var isNameEmpty = viewModel.Name.Trim().Length < 1;
                    var hasUnknownMirrorReference = false;
                    viewModel.MirroredTo.ToList().ForEach(x =>
                    {
                        if (hasUnknownMirrorReference == false)
                        {
                             hasUnknownMirrorReference = vm.Segments.All(z => z.Name != x);
                        }
                    });
                    args.Cancel = isNameExisting || hasUnknownMirrorReference || isNameEmpty;

                    if (args.Cancel)
                    {
                        var msg = "Unknown validation error";
                        if (isNameExisting)
                        {
                            msg = "Name is already taken by another segment";
                        } 
                        else if (isNameEmpty)
                        {
                            msg = "Name must not be empty";
                        }
                        else if (hasUnknownMirrorReference)
                        {
                            msg = "Segment is duplicated to non-existent segments. Check your syntax; this app expects a comma-separated list of duplication targets";
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

        private async void OnEditEvent(SegmentEntry obj)
        {
            var item = await OpenEditDialog(obj);
            if (item != null)
            {
                (DataContext as SegmentPageViewModel)?.UpdateItem(item, obj.Name);
            }
        }

        private async void OnAddEvent()
        {
            var item = await OpenEditDialog(null);
            if (item != null)
            {
                (DataContext as SegmentPageViewModel)?.AddItem(item);
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
