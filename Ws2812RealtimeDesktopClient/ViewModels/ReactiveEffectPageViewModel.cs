using System.ComponentModel;
using System.Text.Json;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using Ws2812RealtimeDesktopClient.Dialogs;
using Ws2812RealtimeDesktopClient.Models;
using Ws2812RealtimeDesktopClient.Pages;
using Ws2812RealtimeDesktopClient.Services;

namespace Ws2812RealtimeDesktopClient.ViewModels
{
    public class ReactiveEffectPageViewModel : ViewModelBase
    {
        private EffectAssignment? _selectedAssignment;
        private string? _selectedEffect;
        private List<EffectAssignment> _assignments = new();

        public ReactiveEffectPageViewModel()
        {
            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Assignments):
                    RaisePropertyChanged(nameof(ShowSelectors));
                    break;
                case nameof(SelectedAssignment):
                {
                    if (_selectedAssignment != null)
                    {
                        SelectedEffect = _selectedAssignment.EffectName;
                    }
                    break;
                }
                case nameof(SelectedEffect):
                    if (_selectedAssignment != null && _selectedEffect != null)
                    {
                        _selectedAssignment.EffectName = _selectedEffect;
                    }
                    break;
            }
        }

        public async Task AssignEffect()
        {
             var result = await OpenAssignDialog(null);
             if (result != null)
             {
                 Assignments.RemoveAll(x => x.SegmentName == result.SegmentName);
                 Assignments.Add(result);
                 
                 RaisePropertyChanged(nameof(Assignments)); 
                 
                 SelectedAssignment = result;
                 SelectedEffect = result.EffectName;
             }
        } 
        
        public void RemoveEffect(object? param)
        {
            if (param is EffectAssignment ass)
            {
                Assignments.RemoveAll(x => x.SegmentName == ass.SegmentName);
                SelectedAssignment = Assignments.FirstOrDefault();
                RaisePropertyChanged(nameof(Assignments));
            }
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

        public List<EffectAssignment> Assignments
        {
            set => RaiseAndSetIfChanged(ref _assignments, value);
            get => _assignments;
        }

        public EffectAssignment? SelectedAssignment
        {
            set => RaiseAndSetIfChanged(ref _selectedAssignment, value);
            get => _selectedAssignment;
        }
        
        public string? SelectedEffect
        {
            set => RaiseAndSetIfChanged(ref _selectedEffect, value);
            get => _selectedEffect;
        }

        public bool ShowSelectors => Assignments.Count > 0;
        public string[] AvailableEffects => ReactiveEffectDescriptorList.Instance.Descriptors.Select(x => x.Name).ToArray();
        
        public string PageHeader => "Reactive effects";
        public string PageSubtitle => "Set-up audio/video reactive real-time effects";
        
        private async Task<EffectAssignment?> OpenAssignDialog(EffectAssignment? entry)
        {
            if (RemoteStripManager.Instance.SegmentEntries.Count < 1)
            {
                var noSegmentsDialog = new ContentDialog()
                {
                    Content = "Please create a new segment before assigning an effect. Press 'Go to...' to navigate to the segment management page.",
                    Title = "No segments defined",
                    PrimaryButtonText = "Go to...",
                    CloseButtonText = "Cancel"
                };
                noSegmentsDialog.PrimaryButtonClick += (_, _) => NavigationService.Instance.Navigate(typeof(SegmentPage));
                await noSegmentsDialog.ShowAsync();
                return null;
            }

            var dialog = new ContentDialog()
            {
                Title = "Assign effect to segment",
                PrimaryButtonText = "Assign",
                CloseButtonText = "Cancel"
            };

            var viewModel = new AssignEffectDialogViewModel(dialog, entry);
            dialog.Content = new AssignEffectContentDialog()
            {
                DataContext = viewModel
            };
            
            void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
            {
                var defer = args.GetDeferral();

                var isEffectEmpty = viewModel.Effect.Trim().Length < 1;
                var isNameEmpty = viewModel.Segment.Trim().Length < 1;
                    
                args.Cancel = isEffectEmpty || isNameEmpty;

                if (args.Cancel)
                {
                    var msg = "Unknown validation error";
                    if (isNameEmpty)
                    {
                        msg = "Segment name must not be empty";
                    }
                    else if (isEffectEmpty)
                    {
                        msg = "Effect must be selected";
                    }
                        
                    var resultHint = new ContentDialog()
                    {
                        Content = msg,
                        Title = "Error",
                        PrimaryButtonText = "Close"
                    };

                    _ = resultHint.ShowAsync().ContinueWith(_ => defer.Complete());
                }
                else
                {
                    defer.Complete();
                }
            }

            dialog.PrimaryButtonClick += OnPrimaryButtonClick;
            var result = await dialog.ShowAsync();
            dialog.PrimaryButtonClick -= OnPrimaryButtonClick;
            
            return result == ContentDialogResult.None ? null : viewModel.ApplyTo(entry);
        }
    }
}