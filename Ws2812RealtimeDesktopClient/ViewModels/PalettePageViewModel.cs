using Avalonia.Collections;
using Ws2812RealtimeDesktopClient.Models;

namespace Ws2812RealtimeDesktopClient.ViewModels
{
    public partial class PalettePageViewModel : ViewModelBase
    {
        public PalettePageViewModel()
        {
            Palettes = new AvaloniaList<PaletteEntry>();
            PaletteChanged += OnPaletteChanged;
        }
        
        ~PalettePageViewModel()
        {
            PaletteChanged -= OnPaletteChanged;
        }

        public string PageHeader => "Color palettes";
        public string PageSubtitle => "Create and edit color palettes used for some effects";

        private void OnPaletteChanged()
        {
            /* Reorder list. AvaloniaList doesn't really support sorting */
            Palettes = new AvaloniaList<PaletteEntry>(Palettes.OrderBy(x => x.Name).AsEnumerable());
            RaisePropertyChanged(nameof(Palettes));
        }

        public event Action? AddEvent;
        public event Action<PaletteEntry>? EditEvent;
        
        public AvaloniaList<PaletteEntry> Palettes { get; set; }
        public event Action? PaletteChanged;

        public void AddItem(PaletteEntry entry)
        {
            if (Palettes.All(x => x.Name != entry.Name))
            {
                Palettes.Add(entry);
                entry.UpdateFromViewModel();
                PaletteManager.Instance.AddOrUpdatePalette(entry, null);
            }
            else
            {
                UpdateItem(entry, entry.Name);
            }
            
            PaletteChanged?.Invoke();
        }  
        
        public void UpdateItem(PaletteEntry entry, string oldName)
        {
            for (var i = 0; i < Palettes.Count; i++)
            {
                if (Palettes[i].Name == oldName)
                {
                    Palettes[i] = entry;
                    Palettes[i].UpdateFromViewModel();
                }
            }
            
            PaletteManager.Instance.AddOrUpdatePalette(entry, oldName);
            PaletteChanged?.Invoke();
        }
        
        public void DeleteItem(object? param)
        {
            if (param is PaletteEntry entry)
            {
                Palettes.Remove(entry);
                PaletteManager.Instance.DeletePalette(entry.Name);
            }
            
            PaletteChanged?.Invoke();
        }

        public void DoAddCommand()
        {
            AddEvent?.Invoke();
        }

        public void DoEditCommand(object? param)
        { 
            if (param is PaletteEntry entry)
            {
                EditEvent?.Invoke(entry);
            }
        }
    }
}
