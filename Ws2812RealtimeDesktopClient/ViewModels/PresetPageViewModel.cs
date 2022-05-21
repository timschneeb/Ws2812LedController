using System.Text.Json;
using Avalonia.Collections;
using FluentAvalonia.UI.Controls;
using Ws2812RealtimeDesktopClient.Dialogs;
using Ws2812RealtimeDesktopClient.Models;
using Ws2812RealtimeDesktopClient.Utilities;

namespace Ws2812RealtimeDesktopClient.ViewModels
{
    public class PresetPageViewModel : ViewModelBase
    {
        public string PageHeader => "Presets";
        public string PageSubtitle => "Manage saved effect presets";
        
        public AvaloniaList<PresetEntry> Presets => PresetManager.Instance.PresetEntries;

        public void DeleteItem(object? param)
        {
            if (param is PresetEntry entry)
            {
                PresetManager.Instance.DeletePreset(entry.Name);
            }
        }

        public async Task RenameItem(object? param)
        { 
            if (param is PresetEntry entry)
            {
                var result = await OpenRenameDialog(entry);
                if (result != null)
                {
                    PresetManager.Instance.AddOrUpdatePreset(result, entry.Name);
                }
                result?.UpdateFromViewModel();
            }
        }
        
        private async Task<PresetEntry?> OpenRenameDialog(PresetEntry? entry)
        {
            if (Presets.Count < 1)
            {
                return null;
            }

            var dialog = new ContentDialog()
            {
                Title = "Rename preset",
                PrimaryButtonText = "Rename",
                CloseButtonText = "Cancel"
            };

            var viewModel = new EditPresetDialogViewModel(dialog, entry);
            dialog.Content = new EditPresetContentDialog()
            {
                DataContext = viewModel
            };
            
            void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
            {
                if (viewModel.Name.Trim().Length < 1)
                {
                    args.Cancel = true;
                    _ = DialogUtils.ShowMessageDialog("Error", "Name must not be empty");
                }
            }

            dialog.PrimaryButtonClick += OnPrimaryButtonClick;
            var result = await dialog.ShowAsync();
            dialog.PrimaryButtonClick -= OnPrimaryButtonClick;
            
            return result == ContentDialogResult.None ? null : viewModel.ApplyTo(entry);
        }    
    }
}
