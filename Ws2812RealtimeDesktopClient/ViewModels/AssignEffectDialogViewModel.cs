using FluentAvalonia.UI.Controls;
using Ws2812LedController.Core.Utils;
using Ws2812RealtimeDesktopClient.Models;

namespace Ws2812RealtimeDesktopClient.ViewModels
{
    public class AssignEffectDialogViewModel : ViewModelBase
    {
        private readonly ContentDialog _dialog;
        private string _segment;
        private string _effect;

        public AssignEffectDialogViewModel(ContentDialog dialog, EffectAssignment? assignment)
        {
            _dialog = dialog;
            assignment ??= new EffectAssignment();
            Segment = assignment.SegmentName;
            Effect = assignment.EffectName;
        }

        public string[] AvailableSegments => RemoteStripManager.Instance.SegmentEntries.Select(x => x.Name).ToArray();
        public string[] AvailableEffects => ReactiveEffectDescriptorList.Instance.Descriptors.Select(x => x.Name).ToArray();

        public string Segment
        {
            set => RaiseAndSetIfChanged(ref _segment, value);
            get => _segment;
        }

        public string Effect
        {
            set => RaiseAndSetIfChanged(ref _effect, value);
            get => _effect;
        }

        public EffectAssignment ApplyTo(EffectAssignment? oldEntry)
        {
            var entry = oldEntry ?? new EffectAssignment();
            entry.EffectName = Effect;
            entry.SegmentName = Segment;
            return entry;
        }
    }
}
