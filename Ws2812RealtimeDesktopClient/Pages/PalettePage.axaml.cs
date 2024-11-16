using System.Drawing;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using Ws2812LedController.Core.FastLedCompatibility;
using Ws2812RealtimeDesktopClient.Dialogs;
using Ws2812RealtimeDesktopClient.Models;
using Ws2812RealtimeDesktopClient.Utilities;
using Ws2812RealtimeDesktopClient.ViewModels;

namespace Ws2812RealtimeDesktopClient.Pages
{
    public class PalettePage : UserControl
    {
        private ListBox _listView;

        public PalettePage()
        {
            InitializeComponent();
            
            var context = new PalettePageViewModel();
            context.AddEvent += OnAddEvent;
            context.EditEvent += OnEditEvent;
            context.Palettes = new AvaloniaList<PaletteEntry>(SettingsProvider.Instance.Palettes ?? Array.Empty<PaletteEntry>());

            _listView = this.FindControl<ListBox>("ListBox1");
            var entries = PaletteManager.Instance.PaletteEntries;
            _listView.Items = entries;
            
            DataContext = context;
        }

        private async Task<PaletteEntry?> OpenEditDialog(PaletteEntry? entry)
        {
            var dialog = new ContentDialog()
            {
                Title = entry == null ? "Create new color palette" : "Edit color palette",
                PrimaryButtonText = entry == null ? "Create" : "Save",
                CloseButtonText = "Cancel"
            };

            var viewModel = new EditPaletteDialogViewModel(dialog, entry);
            var content = new EditPaletteContentDialog
            {
                DataContext = viewModel,
                GradientCheckBox =
                {
                    IsChecked = entry == null
                }
            };
            dialog.Content = content;
            
            
            void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
            {
                if (DataContext is PalettePageViewModel vm)
                {
                    var isNameExisting = entry == null /* create mode */ ? vm.Palettes.Any(x => x.Name == viewModel.Name) :
                        vm.Palettes.Count(x => x.Name == viewModel.Name) > 1;
                    var isNameEmpty = viewModel.Name.Trim().Length < 1;
                    var hasNoColors = true;
                    viewModel.PaletteColors.ToList().ForEach(x =>
                    {
                        if (hasNoColors)
                        {
                            hasNoColors = x == Color.FromArgb(0, 0, 0, 0);
                        }
                    });
                    args.Cancel = isNameExisting || hasNoColors || isNameEmpty;

                    if (args.Cancel)
                    {
                        var msg = "Unknown validation error";
                        if (isNameExisting)
                        {
                            msg = "Name is already taken by another palette";
                        } 
                        else if (isNameEmpty)
                        {
                            msg = "Name must not be empty";
                        }
                        else if (hasNoColors)
                        {
                            msg = "At least one color must be set";
                        }
                        
                        var resultHint = new ContentDialog()
                        {
                            Content = msg,
                            Title = "Error",
                            PrimaryButtonText = "Close"
                        };

                        _ = resultHint.ShowAsync();
                    }
                    else
                    {
                        // Gradient generations
                        if (content.GradientCheckBox.IsChecked ?? false)
                        {
                            CRGBPalette16 expanded;
                            if (viewModel.PaletteColors[1].IsTransparent() && viewModel.PaletteColors[2].IsTransparent() && viewModel.PaletteColors[3].IsTransparent())
                            {
                                expanded = new CRGBPalette16(viewModel.PaletteColors[0]);
                            }
                            else if (viewModel.PaletteColors[2].IsTransparent() && viewModel.PaletteColors[3].IsTransparent())
                            {
                                expanded = new CRGBPalette16(viewModel.PaletteColors[0],viewModel.PaletteColors[1]);
                            }
                            else if (viewModel.PaletteColors[3].IsTransparent())
                            {
                                expanded = new CRGBPalette16(viewModel.PaletteColors[0],viewModel.PaletteColors[1],
                                    viewModel.PaletteColors[2]);
                            }
                            else
                            {
                                expanded = new CRGBPalette16(viewModel.PaletteColors[0],viewModel.PaletteColors[1],
                                    viewModel.PaletteColors[2],viewModel.PaletteColors[3]);
                            }
                            viewModel.PaletteColors = expanded.Entries;
                        }
                    }
                }
                
            }

            dialog.PrimaryButtonClick += OnPrimaryButtonClick;
            var result = await dialog.ShowAsync();
            dialog.PrimaryButtonClick -= OnPrimaryButtonClick;
            
            return result == ContentDialogResult.None ? null : viewModel.ApplyTo(entry);
        }

        private async void OnEditEvent(PaletteEntry obj)
        {
            var item = await OpenEditDialog(obj);
            if (item != null)
            {
                (DataContext as PalettePageViewModel)?.UpdateItem(item, obj.Name);
            }
        }

        private async void OnAddEvent()
        {
            var item = await OpenEditDialog(null);
            if (item != null)
            {
                (DataContext as PalettePageViewModel)?.AddItem(item);
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
