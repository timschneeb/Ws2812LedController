using System.Text.Json;

namespace Ws2812RealtimeDesktopClient.ViewModels
{
    public partial class ReactiveEffectPageViewModel : ViewModelBase
    {
        private string _selectedSegment;
        private string[] _assignments;

        public event Action? AssignEvent;

        public ReactiveEffectPageViewModel()
        {
        }

        public void AssignEffect()
        {
            AssignEvent?.Invoke();
        } 
        
        public void RemoveEffect()
        {
            
        }
        
        public void CopyEffect()
        {
            
        }

        public void LoadComposition()
        {
            
        }

        public void SaveComposition()
        {
            
        }

        public string[] Assignments
        {
            set => RaiseAndSetIfChanged(ref _assignments, value);
            get => _assignments;
        }

        public string SelectedSegment
        {
            set => RaiseAndSetIfChanged(ref _selectedSegment, value);
            get => _selectedSegment;
        }

        public string PageHeader => "Reactive effects";
        public string PageSubtitle => "Set-up audio/video reactive real-time effects";
    }
}
