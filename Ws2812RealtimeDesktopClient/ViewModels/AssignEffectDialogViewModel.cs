using FluentAvalonia.UI.Controls;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;
using Ws2812RealtimeDesktopClient.Models;

namespace Ws2812RealtimeDesktopClient.ViewModels
{
    public class AssignEffectDialogViewModel : ViewModelBase
    {
        private readonly ContentDialog _dialog;
        private string _segment;
        private EffectDescriptor? _effect;

        public AssignEffectDialogViewModel(ContentDialog dialog, EffectAssignment? assignment)
        {
            _dialog = dialog;
            assignment ??= new EffectAssignment();
            Segment = assignment.SegmentName;
            Effect = AvailableEffects.FirstOrDefault(x => x.Name == assignment.EffectName);
        }

        public string[] AvailableSegments => RemoteStripManager.Instance.SegmentEntries.Select(x => x.Name).ToArray();
        public EffectDescriptor[] AvailableEffects => ReactiveEffectDescriptorList.Instance.Descriptors.ToArray();

        public string Segment
        {
            set => RaiseAndSetIfChanged(ref _segment, value);
            get => _segment;
        }

        public EffectDescriptor? Effect
        {
            set => RaiseAndSetIfChanged(ref _effect, value);
            get => _effect;
        }

        public EffectAssignment? ApplyTo(EffectAssignment? oldEntry)
        {
            if (Effect == null)
            {
                return null;
            }
            
            var entry = oldEntry ?? new EffectAssignment();
            entry.EffectName = Effect.Name;
            entry.SegmentName = Segment;
            return entry;
        }
    }
}
