using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;

using JetBrains.Annotations;

using Junctionizer.Model;

using MaterialDesignThemes.Wpf;

using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

using Prism.Commands;
using Prism.Interactivity.InteractionRequest;

using Utilities;

using static Junctionizer.StaticMethods;

namespace Junctionizer.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel()
        {
            PassesFilterChangedObservable = Observable.FromEventPattern<PropertyChangedEventArgs>(this, nameof(PropertyChanged))
                                                      .Where(pattern => pattern.EventArgs.PropertyName == nameof(PassesFilter))
                                                      .Sample(TimeSpan.FromMilliseconds(200))
                                                      .ObserveOn(SynchronizationContext.Current);
        }

        public void Initialize()
        {
            (SourceCollection, DestinationCollection) = FolderCollection.Factory.CreatePair(() => SynchronizationContext.Current is DispatcherSynchronizationContext);

            FolderPairCollection = new GameFolderPairEnumerable(SourceCollection, DestinationCollection);

            BindingOperations.EnableCollectionSynchronization(DisplayedMappings, new object());

            SourceCollection.PropertyChanged += OnFolderCollectionPropertyChanged;
            DestinationCollection.PropertyChanged += OnFolderCollectionPropertyChanged;

            Task.Run(() => {
                // Run inside of a task so that the UI isn't blocked from appearing. 

                Settings.StateTracker.Configure(this).AddProperties(nameof(DisplayedMappings), nameof(SelectedMapping)).Apply();

                var folderCollectionPersistedProperties = new[] {nameof(FolderCollection.FolderBrowserInitialLocation)};
                Settings.StateTracker.Configure(SourceCollection).IdentifyAs("Source").AddProperties(folderCollectionPersistedProperties).Apply();
                Settings.StateTracker.Configure(DestinationCollection).IdentifyAs("Destination").AddProperties(folderCollectionPersistedProperties).Apply();

                if (!Directory.Exists(Settings.AppDataDirectoryPath)) NewUserSetup();
            });
        }

        private void NewUserSetup()
        {
            MessageBox.Show("To get started select a source directory (top left) that contains the directories you wish to move. Then select a destination directory on another drive.");

            RegistryKey steamRegKey = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            SourceCollection.FolderBrowserInitialLocation =
                steamRegKey == null ? @"C:" : steamRegKey.GetValue("SteamPath").ToString().Replace(@"/", @"\") + @"\steamapps\common";

            var suggestedBackupDrive =
                DriveInfo.GetDrives()
                         .Where(driveInfo => driveInfo.IsReady && driveInfo.Name != @"C:\" && driveInfo.DriveType == DriveType.Fixed)
                         .OrderByDescending(driveInfo => driveInfo.AvailableFreeSpace)
                         .Select(driveInfo => driveInfo.Name)
                         .FirstOrDefault() ?? "E:";
            DestinationCollection.FolderBrowserInitialLocation = suggestedBackupDrive;
        }

        /// <summary>Fody PropertyChanged handles creating change notification whenever something this function depends on changes.</summary>
        [NotNull]
        [AutoLazy.Lazy]
        public Func<GameFolder, bool> PassesFilter => folder =>
            folder.Name.Contains(FilterNameText, StringComparison.OrdinalIgnoreCase)
            && FilterLowerSizeLimit <= folder.Size && folder.Size <= FilterUpperSizeLimit;

        public IObservable<EventPattern<PropertyChangedEventArgs>> PassesFilterChangedObservable { get; }

        public ObservableCollection<string> LiveFilteringProperties { get; } = new ObservableCollection<string>();

        public string FilterNameText { get; set; } = string.Empty;
        [UsedImplicitly]
        private void OnFilterNameTextChanged()
        {
            const string propertyName = nameof(GameFolder.Name);
            if (string.IsNullOrEmpty(FilterNameText)) LiveFilteringProperties.Remove(propertyName);
            else if (!LiveFilteringProperties.Contains(propertyName)) LiveFilteringProperties.Add(propertyName);
        }

        public double FilterLowerSizeLimit { get; set; } = double.NegativeInfinity;
        [UsedImplicitly]
        private void OnFilterLowerSizeLimitChanged() => OnSizeFilterChanged();

        public double FilterUpperSizeLimit { get; set; } = double.PositiveInfinity;
        [UsedImplicitly]
        private void OnFilterUpperSizeLimitChanged() => OnSizeFilterChanged();

        private void OnSizeFilterChanged()
        {
            const string propertyName = nameof(GameFolder.Size);
            var isNotFilteringBySize = double.IsNegativeInfinity(FilterLowerSizeLimit) && double.IsPositiveInfinity(FilterUpperSizeLimit);
            if (isNotFilteringBySize) LiveFilteringProperties.Remove(propertyName);
            else if (!LiveFilteringProperties.Contains(propertyName)) LiveFilteringProperties.Add(propertyName);
        }

        public FindJunctionsViewModel FindJunctionsViewModel { get; } = new FindJunctionsViewModel();
        public InteractionRequest<INotification> ShowErrorDialogRequest { get; } = new InteractionRequest<INotification>();
        public InteractionRequest<INotification> DisplayFindJunctionsDialogRequest { get; } = new InteractionRequest<INotification>();
        public InteractionRequest<INotification> CloseDialogRequest { get; } = new InteractionRequest<INotification>();

        [AutoLazy.Lazy]
        public DelegateCommand<DialogClosingEventArgs> DialogClosedCommand
            => new DelegateCommand<DialogClosingEventArgs>(args => OnDialogClosed?.Invoke());
        private event Action OnDialogClosed;

        public FolderCollection SourceCollection { get; private set; }
        public FolderCollection DestinationCollection { get; private set; }

        public GameFolderPairEnumerable FolderPairCollection { get; private set; }

        public ObservableCollection<DirectoryMapping> DisplayedMappings { get; [UsedImplicitly] private set; } = new ObservableCollection<DirectoryMapping>();

        /// <summary>Changes to folder locations and DisplayedMappings can result in the SelectedMapping setter being called. This tracks whether the origination of those changes is the SelectedMapping setter itself in order to prevent (indirect) recursive calls.</summary>
        private bool IsSettingSelectedMapping { get; set; }
        private DirectoryMapping _selectedMapping;
        public DirectoryMapping SelectedMapping
        {
            get => _selectedMapping;
            set {
                if (IsSettingSelectedMapping) return;

                IsSettingSelectedMapping = true;
                var previousValue = _selectedMapping;
                _selectedMapping = value;

                if (_selectedMapping != null)
                {
                    if (_selectedMapping.Source != null && !Directory.Exists(_selectedMapping.Source))
                    {
                        DisplayedMappings.Remove(_selectedMapping);
                        _selectedMapping = new DirectoryMapping(null, _selectedMapping.Destination);
                    }
                    if (_selectedMapping.Destination != null && !Directory.Exists(_selectedMapping.Destination))
                    {
                        DisplayedMappings.Remove(_selectedMapping);
                        _selectedMapping = new DirectoryMapping(_selectedMapping.Source, null);
                    }

                    if (_selectedMapping.Source == null && _selectedMapping.Destination == null)
                    {
                        _selectedMapping = null;
                    }
                    else
                    {
                        if (!DisplayedMappings.Contains(_selectedMapping)) DisplayedMappings.Add(_selectedMapping);
                        _selectedMapping.PropertyChanged += SelectedMappingPropertyChanged;
                    }
                }

                if (previousValue?.IsSavedMapping == false) DisplayedMappings.Remove(previousValue);
                if (previousValue != null) previousValue.PropertyChanged -= SelectedMappingPropertyChanged;

                SourceCollection.Location = _selectedMapping?.Source;
                DestinationCollection.Location = _selectedMapping?.Destination;
                IsSettingSelectedMapping = false;
            }
        }

        private void SelectedMappingPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(DirectoryMapping.IsSavedMapping)))
            {
                SaveCurrentLocationCommand.RaiseCanExecuteChanged();
                DeleteCurrentLocationCommand.RaiseCanExecuteChanged();
            }
        }

        private void OnFolderCollectionPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            // When a new folder location is chosen, check if it is already saved and if so select it so that it can be displayed in the combo box
            if (args.PropertyName.Equals(nameof(FolderCollection.Location)))
            {
                SelectedMapping = DisplayedMappings.FirstOrDefault(mapping =>
                                      string.Equals(mapping.Source, SourceCollection.Location, StringComparison.OrdinalIgnoreCase) &&
                                      string.Equals(mapping.Destination, DestinationCollection.Location, StringComparison.OrdinalIgnoreCase))
                                  ?? new DirectoryMapping(SourceCollection.Location, DestinationCollection.Location);
            }
        }

        [AutoLazy.Lazy]
        public DelegateCommand SaveCurrentLocationCommand => new DelegateCommand(() => {
            if (SelectedMapping != null) SelectedMapping.IsSavedMapping = true;
        }, () => SelectedMapping?.IsSavedMapping == false).ObservesProperty(() => SelectedMapping);

        [AutoLazy.Lazy]
        public DelegateCommand DeleteCurrentLocationCommand => new DelegateCommand(() => {
            if (SelectedMapping != null) SelectedMapping.IsSavedMapping = false;
        }, () => SelectedMapping?.IsSavedMapping == true).ObservesProperty(() => SelectedMapping);

        [AutoLazy.Lazy]
        public DelegateCommand FindExistingJunctionsCommand => new DelegateCommand(() => FindExistingJunctions().Forget());

        private async Task FindExistingJunctions()
        {
            var selectedDirectory = PromptForDirectory("Select Root Directory");
            if (selectedDirectory == null) return;

            DisplayFindJunctionsDialogRequest.Raise(null);
            OnDialogClosed = () => FindJunctionsViewModel.Cancel();

            var junctions = await FindJunctionsViewModel.GetJunctions(selectedDirectory);

            foreach (var directoryInfo in junctions)
            {
                Debug.Assert(directoryInfo.Parent != null, "directoryInfo.Parent != null");
                var folderMapping = new DirectoryMapping(directoryInfo.Parent.FullName,
                    Directory.GetParent(JunctionPoint.GetTarget(directoryInfo)).FullName, isSavedMapping: true);
                if (!DisplayedMappings.Contains(folderMapping)) DisplayedMappings.Add(folderMapping);
            }
        }

        /// <summary>Shows an error message and then reprompts if the user selects an invalid entry. Returns null iff the dialog is cancelled.</summary>
        private DirectoryInfo PromptForDirectory(string dialogTitle)
        {
            var folderDialog = NewFolderDialog(dialogTitle);

            while (folderDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                try
                {
                    return new DirectoryInfo(folderDialog.FileName);
                }
                catch (Exception)
                {
                    ShowErrorDialogRequest.Raise(
                        new Prism.Interactivity.InteractionRequest.Notification {
                            Title = "Invalid Selection",
                            Content =
                                $"Unable to search the selected location - did you select a valid directory path?" +
                                $"\nTry choosing a more specific location."
                        });
                }
            }

            return null;
        }

        [AutoLazy.Lazy]
        public DelegateCommand RefreshFolderSizesCommand => new DelegateCommand(() => {
            SourceCollection.RefreshSizes();
            DestinationCollection.RefreshSizes();
        });
    }
}
