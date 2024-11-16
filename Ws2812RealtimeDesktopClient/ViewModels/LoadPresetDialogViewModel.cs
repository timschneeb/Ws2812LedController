using FluentAvalonia.UI.Controls;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;
using Ws2812RealtimeDesktopClient.Models;

namespace Ws2812RealtimeDesktopClient.ViewModels
{
    public class LoadPresetDialogViewModel : ViewModelBase
    {
        private readonly ContentDialog _dialog;
        private PresetEntry? _preset;

        public LoadPresetDialogViewModel(ContentDialog dialog)
        {
            _dialog = dialog;
            Preset = AvailablePresets.FirstOrDefault();
        }

        public IEnumerable<PresetEntry> AvailablePresets => PresetManager.Instance.PresetEntries.ToArray();

        public PresetEntry? Preset
        {
            set => RaiseAndSetIfChanged(ref _preset, value);
            get => _preset;
        }
    }
}
