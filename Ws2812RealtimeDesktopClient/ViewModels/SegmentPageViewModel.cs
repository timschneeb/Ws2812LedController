using Avalonia.Collections;
using Ws2812RealtimeDesktopClient.Models;

namespace Ws2812RealtimeDesktopClient.ViewModels
{
    public partial class SegmentPageViewModel : ViewModelBase
    {
        public SegmentPageViewModel()
        {
            Segments = new AvaloniaList<SegmentEntry>();
            SegmentChanged += OnSegmentChanged;
        }
        
        ~SegmentPageViewModel()
        {
            SegmentChanged -= OnSegmentChanged;
        }

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
            // Migrate assignments and presets in case of name change
            if (oldName != segment.Name)
            {
                // Modify assignments
                foreach (var assign in RemoteStripManager.Instance.EffectAssignments)
                {
                    if (assign.SegmentName == oldName)
                    {
                        assign.SegmentName = segment.Name;
                        await RemoteStripManager.Instance.AddOrUpdateEffectAssignmentAsync(assign);
                    }
                }
                
                // Modify presets
                foreach (var preset in PresetManager.Instance.PresetEntries)
                {
                    var modified = false;
                    foreach (var assign in preset.Effects ?? Array.Empty<EffectAssignment>())
                    {
                        if (assign.SegmentName == oldName)
                        {
                            modified = true;
                            assign.SegmentName = segment.Name;
                        }
                    }

                    if (modified)
                    {
                        PresetManager.Instance.AddOrUpdatePreset(preset, preset.Name);
                    }
                }
            }
            
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
