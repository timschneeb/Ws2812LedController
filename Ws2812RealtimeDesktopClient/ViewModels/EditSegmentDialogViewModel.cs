using FluentAvalonia.UI.Controls;
using Ws2812RealtimeDesktopClient.Models;

namespace Ws2812RealtimeDesktopClient.ViewModels
{
    public class EditSegmentDialogViewModel : ViewModelBase
    {
        private readonly ContentDialog _dialog;
        public EditSegmentDialogViewModel(ContentDialog dialog, SegmentEntry? segment)
        {
            _dialog = dialog;

            segment ??= new SegmentEntry("", 0, 1);

            Name = segment.Name;
            MirroredTo = segment.MirroredTo;
            Start = segment.Start;
            Width = segment.Width;
            InvertX = segment.InvertX;
        }

        public string Name { set; get; }
        public string[] MirroredTo { set; get; }
        public int Start { set; get; }
        public int Width { set; get; }
        public bool InvertX { set; get; }
        public string MirroredToCsv
        {
            set => MirroredTo = value.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            get => string.Join(",", MirroredTo);
        }

        public SegmentEntry ApplyTo(SegmentEntry? oldEntry)
        {
            var entry = oldEntry ?? new SegmentEntry(Name, Start, Width);
            entry.Name = Name;
            entry.MirroredTo = MirroredTo;
            entry.Start = Start;
            entry.Width = Width;
            entry.InvertX = InvertX;
            return entry;
        }
    }
}
