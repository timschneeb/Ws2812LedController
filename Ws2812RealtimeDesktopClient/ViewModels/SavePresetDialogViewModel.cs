using FluentAvalonia.UI.Controls;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;
using Ws2812RealtimeDesktopClient.Models;

namespace Ws2812RealtimeDesktopClient.ViewModels
{
    public class SavePresetDialogViewModel : ViewModelBase
    {
        private readonly ContentDialog _dialog;
        private string _presetName;

        public SavePresetDialogViewModel(ContentDialog dialog, string presetName = "")
        {
            _dialog = dialog;
            _presetName = presetName;
        }

        public IEnumerable<string> AvailablePresetNames => 
            PresetManager.Instance.PresetEntries.Select(x => x.Name).ToArray();

        public string PresetName
        {
            set => RaiseAndSetIfChanged(ref _presetName, value);
            get => _presetName;
        }
    }
}
