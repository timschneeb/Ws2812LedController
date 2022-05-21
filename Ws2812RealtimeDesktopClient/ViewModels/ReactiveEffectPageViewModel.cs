using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using Force.DeepCloner;
using Ws2812LedController.Core.Model;
using Ws2812RealtimeDesktopClient.Dialogs;
using Ws2812RealtimeDesktopClient.Models;
using Ws2812RealtimeDesktopClient.Pages;
using Ws2812RealtimeDesktopClient.Services;
using Ws2812RealtimeDesktopClient.Utilities;

namespace Ws2812RealtimeDesktopClient.ViewModels
{
    public class ReactiveEffectPageViewModel : ViewModelBase
    {
        private EffectAssignment? _selectedAssignment;
        private EffectDescriptor? _selectedEffect;

        public ReactiveEffectPageViewModel()
        {
            PropertyChanged += OnPropertyChanged;
        }

        public DataGridCollectionView PropertyGridItems
        {
            set => RaiseAndSetIfChanged(ref _propertyGridItems, value);
            get => _propertyGridItems;
        }

        public void UpdatePropertyGrid()
        {
            if (_selectedAssignment == null)
            {
                PropertyGridItems = new DataGridCollectionView(Array.Empty<PropertyRow>());
                return;
            }

            PropertyGridItems = new DataGridCollectionView(_selectedAssignment.Properties)
            {
                GroupDescriptions =
                {
                    new DataGridPathGroupDescription("Group")
                }
            };

            PropertyGridItems.SortDescriptions.Add(new DataGridComparerSortDesctiption(new PropertyGroupSorter()
            {

            }, ListSortDirection.Ascending));
        }

        private async void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Assignments):
                    RaisePropertyChanged(nameof(ShowSelectors));
                    UpdatePropertyGrid();
                    break;
                case nameof(SelectedAssignment):
                {
                    if (_selectedAssignment != null)
                    {
                        _ignoreEffectChange = true;
                        SelectedEffect = AvailableEffects.FirstOrDefault(x => x.Name == _selectedAssignment.EffectName);
                        UpdatePropertyGrid();
                        _ignoreEffectChange = false;
                    }

                    break;
                }
                case nameof(SelectedEffect):
                    if (_selectedAssignment != null && _selectedEffect != null && !_ignoreEffectChange)
                    {
                        // Different effect; don't carry over properties
                        _selectedAssignment.Properties = new AvaloniaList<PropertyRow>();
                        _selectedAssignment.EffectName = _selectedEffect.Name;

                        var copy = _selectedAssignment;
                        await RemoteStripManager.Instance.DeleteEffectAssignmentAsync(_selectedAssignment.SegmentName,
                            true);
                        await RemoteStripManager.Instance.AddOrUpdateEffectAssignmentAsync(copy);
                        SelectedAssignment = copy;
                        RaisePropertyChanged(nameof(Assignments));
                    }

                    break;
            }
        }

        private bool _ignoreEffectChange = false;
        private DataGridCollectionView _propertyGridItems = new(Array.Empty<PropertyRow>());

        public async Task AssignEffect()
        {
            var result = await OpenAssignDialog(null);
            if (result != null)
            {
                await RemoteStripManager.Instance.AddOrUpdateEffectAssignmentAsync(result);
                RaisePropertyChanged(nameof(Assignments));

                SelectedAssignment = result;
                SelectedEffect = AvailableEffects.FirstOrDefault(x => x.Name == result.EffectName);
                UpdatePropertyGrid();
            }
        }

        public async Task RemoveEffect(object? param)
        {
            if (param is EffectAssignment ass)
            {
                await RemoteStripManager.Instance.DeleteEffectAssignmentAsync(ass.SegmentName);
                SelectedAssignment = Assignments.FirstOrDefault();
                RaisePropertyChanged(nameof(Assignments));
                UpdatePropertyGrid();
            }
        }

        public async Task CopyEffect()
        {
            if (_selectedAssignment == null)
            {
                return;
            }

            var result = await OpenCopyDialog(_selectedAssignment.SegmentName);
            if (result != null)
            {
                var source = _selectedAssignment;
                var target = Assignments.FirstOrDefault(x => x.SegmentName == result) ?? new EffectAssignment();
                target.SegmentName = result;
                target.EffectName = source.EffectName;
                target.Properties = new AvaloniaList<PropertyRow>();
                foreach (var prop in source.Properties.ToArray())
                {
                    target.Properties.Add(prop.DeepClone());
                }

                await RemoteStripManager.Instance.AddOrUpdateEffectAssignmentAsync(target);
                RaisePropertyChanged(nameof(Assignments));

                SelectedAssignment = target;
                SelectedEffect = AvailableEffects.FirstOrDefault(x => x.Name == target.EffectName);
                UpdatePropertyGrid();
            }
        }

        public async Task LoadComposition()
        {
            var preset = await OpenLoadDialog();
            if (preset != null && preset.Effects != null)
            {
                await RemoteStripManager.Instance.SyncEffectAssignmentsAsync(preset.Effects, true);
                RaisePropertyChanged(nameof(Assignments));

                SelectedAssignment = Assignments.FirstOrDefault();
                SelectedEffect = AvailableEffects.FirstOrDefault(x => x.Name == SelectedAssignment?.EffectName);
            }
        }

        public async Task SaveComposition()
        {
            var preset = await OpenSaveDialog(RemoteStripManager.Instance.EffectAssignments.ToArray());
            if (preset != null && preset.Effects != null)
            {
                PresetManager.Instance.AddOrUpdatePreset(preset);
            }
        }

        public AvaloniaList<EffectAssignment> Assignments => RemoteStripManager.Instance.EffectAssignments;

        public EffectAssignment? SelectedAssignment
        {
            set => RaiseAndSetIfChanged(ref _selectedAssignment, value);
            get => _selectedAssignment;
        }

        public EffectDescriptor? SelectedEffect
        {
            set => RaiseAndSetIfChanged(ref _selectedEffect, value);
            get => _selectedEffect;
        }

        public bool ShowSelectors => Assignments.Count > 0;
        public EffectDescriptor[] AvailableEffects => ReactiveEffectDescriptorList.Instance.Descriptors;

        public string PageHeader => "Reactive effects";
        public string PageSubtitle => "Set-up audio/video reactive real-time effects";

        private async Task<EffectAssignment?> OpenAssignDialog(EffectAssignment? entry)
        {
            if (RemoteStripManager.Instance.SegmentEntries.Count < 1)
            {
                await DialogUtils.ShowNoSegmentsDialog();
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
                var isEffectEmpty = (viewModel.Effect?.Name.Trim().Length ?? 0) < 1;
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

                    _ = DialogUtils.ShowMessageDialog("Error", msg);
                }
            }

            dialog.PrimaryButtonClick += OnPrimaryButtonClick;
            var result = await dialog.ShowAsync();
            dialog.PrimaryButtonClick -= OnPrimaryButtonClick;

            return result == ContentDialogResult.None ? null : viewModel.ApplyTo(entry);
        }

        private async Task<string?> OpenCopyDialog(string sourceSegment)
        {
            switch (RemoteStripManager.Instance.SegmentEntries.Count)
            {
                case < 1:
                    await DialogUtils.ShowNoSegmentsDialog();
                    return null;
                case < 2:
                    await DialogUtils.ShowNoSegmentsDialog(
                        "No other segments exist. Please create a new segment to copy to.");
                    return null;
            }

            var dialog = new ContentDialog()
            {
                Title = "Copy effect to segment",
                PrimaryButtonText = "Copy",
                CloseButtonText = "Cancel"
            };

            var viewModel = new CopyEffectDialogViewModel(dialog, sourceSegment);
            dialog.Content = new CopyEffectContentDialog()
            {
                DataContext = viewModel
            };

            void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
            {
                if (viewModel.Segment == null)
                {
                    args.Cancel = true;
                    _ = DialogUtils.ShowMessageDialog("Error", "No target segment selected");
                }
            }

            dialog.PrimaryButtonClick += OnPrimaryButtonClick;
            var result = await dialog.ShowAsync();
            dialog.PrimaryButtonClick -= OnPrimaryButtonClick;

            return result == ContentDialogResult.None ? null : viewModel.Segment;
        }
        
        private async Task<PresetEntry?> OpenLoadDialog()
        {
            if (PresetManager.Instance.PresetEntries.Count < 1)
            {
                await DialogUtils.ShowMessageDialog("No presets", "There are no presets saved. Please create one first.");
                return null;
            }

            var dialog = new ContentDialog()
            {
                Title = "Load preset",
                PrimaryButtonText = "Load",
                CloseButtonText = "Cancel"
            };

            var viewModel = new LoadPresetDialogViewModel(dialog);
            dialog.Content = new LoadPresetContentDialog()
            {
                DataContext = viewModel
            };
            
            void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
            {
                if (viewModel.Preset == null)
                {
                    args.Cancel = true;
                    _ = DialogUtils.ShowMessageDialog("Error", "No preset selected");
                }
            }

            dialog.PrimaryButtonClick += OnPrimaryButtonClick;
            var result = await dialog.ShowAsync();
            dialog.PrimaryButtonClick -= OnPrimaryButtonClick;

            foreach (var assign in viewModel.Preset?.Effects ?? Array.Empty<EffectAssignment>())
            {
                assign.InflateProperties();
            }
            
            return result == ContentDialogResult.None ? null : viewModel.Preset;
        }   
        private async Task<PresetEntry?> OpenSaveDialog(EffectAssignment[] assignments)
        {
            if (assignments.Length < 1)
            {
                await DialogUtils.ShowMessageDialog("No effects set", "Please assign at least one effect to a segment to continue.");
                return null;
            }

            var dialog = new ContentDialog()
            {
                Title = "Save preset",
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel"
            };

            var viewModel = new SavePresetDialogViewModel(dialog);
            dialog.Content = new SavePresetContentDialog()
            {
                DataContext = viewModel
            };

            Task<PresetEntry>? task = null;
            void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
            {
                if (viewModel.PresetName.Length < 1)
                {
                    args.Cancel = true;
                    _ = DialogUtils.ShowMessageDialog("Error", "Preset name is empty");
                    return;
                }

                var defer = args.GetDeferral();
                dialog.Title = "Saving preset...";
                dialog.PrimaryButtonText = "Saving...";
                
                task = Task.Run(async () =>
                {
                    var x = new PresetEntry(viewModel.PresetName)
                    {
                        Effects = assignments.DeepClone()
                    };
                    await Dispatcher.UIThread.InvokeAsync(() => defer?.Complete());
                    return x;
                });
            }

            dialog.PrimaryButtonClick += OnPrimaryButtonClick;
            var result = await dialog.ShowAsync();
            dialog.PrimaryButtonClick -= OnPrimaryButtonClick;

            return result == ContentDialogResult.None || task == null ? null : await task;
        }
    }
}