using System.Drawing;
using FluentAvalonia.UI.Controls;
using Ws2812RealtimeDesktopClient.Models;

namespace Ws2812RealtimeDesktopClient.ViewModels
{
    public class EditPresetDialogViewModel : ViewModelBase
    {
        private readonly ContentDialog _dialog;
        private string _name = string.Empty;

        public EditPresetDialogViewModel(ContentDialog dialog, PresetEntry? entry)
        {
            _dialog = dialog;

            Name = entry?.Name ?? string.Empty;
        }

        public string Name
        {
            set => RaiseAndSetIfChanged(ref _name, value);
            get => _name;
        }

        public PresetEntry ApplyTo(PresetEntry? oldEntry)
        {
            var entry = oldEntry ?? new PresetEntry(Name);
            entry.Name = Name;
            return entry;
        }
    }
}
