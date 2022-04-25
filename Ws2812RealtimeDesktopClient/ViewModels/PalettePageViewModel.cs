using Avalonia.Collections;
using Ws2812RealtimeDesktopClient.Models;

namespace Ws2812RealtimeDesktopClient.ViewModels
{
    public partial class PalettePageViewModel : ViewModelBase
    {
        public PalettePageViewModel()
        {
            Segments = new AvaloniaList<SegmentEntry>();
            SegmentChanged += OnSegmentChanged;
        }
        
        ~PalettePageViewModel()
        {
            SegmentChanged -= OnSegmentChanged;
        }

        public string PageHeader => "Color palettes";
        public string PageSubtitle => "Create and edit color palettes used for some effects";

        private void OnSegmentChanged()
        {
            /* Reorder list. AvaloniaList doesn't really support sorting */
            Segments = new AvaloniaList<SegmentEntry>(Segments.OrderBy(x => x.Start).AsEnumerable());
            RaisePropertyChanged(nameof(Segments));
        }

        public event Action? AddEvent;
        public event Action<SegmentEntry>? EditEvent;
        
        public AvaloniaList<SegmentEntry> Segments { get; set; }
        public event Action? SegmentChanged;

        public async Task AddItem(SegmentEntry segment)
        {
            if (Segments.All(x => x.Name != segment.Name))
            {
                Segments.Add(segment);
                segment.UpdateFromViewModel();
                RemoteStripManager.Instance.AddSegment(segment);
            }
            else
            {
                await UpdateItem(segment, segment.Name);
            }
            
            SegmentChanged?.Invoke();
        }  
        
        public async Task UpdateItem(SegmentEntry segment, string oldName)
        {
            for (var i = 0; i < Segments.Count; i++)
            {
                if (Segments[i].Name == oldName)
                {
                    Segments[i] = segment;
                    Segments[i].UpdateFromViewModel();
                }
            }
            
            await RemoteStripManager.Instance.UpdateSegmentAsync(segment, oldName);
            SegmentChanged?.Invoke();
        }
        
        public async Task DeleteItem(object? param)
        {
            if (param is SegmentEntry segment)
            {
                Segments.Remove(segment);
                await RemoteStripManager.Instance.DeleteSegmentAsync(segment.Name);
            }
            
            SegmentChanged?.Invoke();
        }

        public void DoAddCommand()
        {
            AddEvent?.Invoke();
        }

        public void DoEditCommand(object? param)
        { 
            if (param is SegmentEntry segment)
            {
                EditEvent?.Invoke(segment);
            }
        }
    }
}
