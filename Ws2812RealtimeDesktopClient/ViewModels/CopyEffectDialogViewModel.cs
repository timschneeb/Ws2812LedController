using FluentAvalonia.UI.Controls;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;
using Ws2812RealtimeDesktopClient.Models;

namespace Ws2812RealtimeDesktopClient.ViewModels
{
    public class CopyEffectDialogViewModel : ViewModelBase
    {
        private readonly ContentDialog _dialog;
        private readonly string _sourceSegment;
        private string? _segment;

        public CopyEffectDialogViewModel(ContentDialog dialog, string sourceSegment)
        {
            _dialog = dialog;
            _sourceSegment = sourceSegment;
            Segment = AvailableSegments.FirstOrDefault();
        }

        public IEnumerable<string> AvailableSegments => RemoteStripManager.Instance.SegmentEntries
            .Where(x => x.Name != _sourceSegment)
            .Select(x => x.Name)
            .ToArray();

        public string? Segment
        {
            set => RaiseAndSetIfChanged(ref _segment, value);
            get => _segment;
        }
    }
}
