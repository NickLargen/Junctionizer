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
using System.Windows.Data;
using System.Windows.Threading;

using JetBrains.Annotations;

using Junctionizer.Model;

using Microsoft.Win32;

using Prism.Commands;

using Utilities;

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

            (SourceCollection, DestinationCollection) = FolderCollection.Factory.CreatePair(() => SynchronizationContext.Current is DispatcherSynchronizationContext, UISettings.PauseTokenSource);

            SourceCollection.JunctionCreated += () => SelectedMapping.IsSavedMapping = true;

            FolderPairCollection = new GameFolderPairEnumerable(SourceCollection, DestinationCollection, UISettings.PauseTokenSource);

            BindingOperations.EnableCollectionSynchronization(DisplayedMappings, new object());

            SourceCollection.PropertyChanged += OnFolderCollectionPropertyChanged;
            DestinationCollection.PropertyChanged += OnFolderCollectionPropertyChanged;
            
            Settings.StateTracker.Configure(this).AddProperties(nameof(DisplayedMappings), nameof(SelectedMapping)).Apply();

            var folderCollectionPersistedProperties = new[] {nameof(FolderCollection.FolderBrowserInitialLocation)};
            Settings.StateTracker.Configure(SourceCollection).IdentifyAs("Source").AddProperties(folderCollectionPersistedProperties).Apply();
            Settings.StateTracker.Configure(DestinationCollection).IdentifyAs("Destination").AddProperties(folderCollectionPersistedProperties).Apply();
        }

        public void NewUserSetup()
        {
            Dialogs.DisplayMessageBox("To get started select a source directory (top left) that contains the directories you wish to move. Then select a destination directory on another drive that files can be copied into.");

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

        #region Filtering Displayed Items
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
        #endregion

        public FolderCollection SourceCollection { get; }
        public FolderCollection DestinationCollection { get; }

        public GameFolderPairEnumerable FolderPairCollection { get; }

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
                    // If a mapping is set that is equivalent to one already in DisplayedMappings, use that one instead
                    _selectedMapping = DisplayedMappings.FirstOrDefault(mapping => mapping == _selectedMapping) ?? _selectedMapping;

                    if (_selectedMapping.Source != null && !Directory.Exists(_selectedMapping.Source))
                    {
                        DisplayedMappings.Remove(_selectedMapping);
                        _selectedMapping = FindOrCreateMapping(null, _selectedMapping.Destination);
                    }
                    if (_selectedMapping.Destination != null && !Directory.Exists(_selectedMapping.Destination))
                    {
                        DisplayedMappings.Remove(_selectedMapping);
                        _selectedMapping = FindOrCreateMapping(_selectedMapping.Source, null);
                    }

                    if (_selectedMapping.Source == null && _selectedMapping.Destination == null)
                    {
                        _selectedMapping = null;
                    }
                    else
                    {
                        if (!DisplayedMappings.Contains(_selectedMapping)) DisplayedMappings.Add(_selectedMapping);
                    }
                }

                if (previousValue?.IsSavedMapping == false) DisplayedMappings.Remove(previousValue);

                SourceCollection.Location = _selectedMapping?.Source;
                DestinationCollection.Location = _selectedMapping?.Destination;
                IsSettingSelectedMapping = false;
            }
        }

        private void OnFolderCollectionPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            // When a new folder location is chosen, check if it is already saved and if so select it so that it can be displayed in the combo box
            if (!IsSettingSelectedMapping && args.PropertyName.Equals(nameof(FolderCollection.Location)))
            {
                SelectedMapping = FindOrCreateMapping(SourceCollection.Location, DestinationCollection.Location);
            }
        }

        [NotNull]
        private DirectoryMapping FindOrCreateMapping(string source, string destination)
        {
             return DisplayedMappings.FirstOrDefault(mapping =>
                        string.Equals(mapping.Source, source, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(mapping.Destination, destination, StringComparison.OrdinalIgnoreCase)) ?? new DirectoryMapping(source, destination);
        }

        [AutoLazy.Lazy]
        public DelegateCommand FindExistingJunctionsCommand => new DelegateCommand(() => FindExistingJunctions().Forget());

        private async Task FindExistingJunctions()
        {
            var selectedDirectory = await Dialogs.PromptForDirectory("Select Root Directory").ConfigureAwait(false);
            if (selectedDirectory == null) return;

            var findJunctionsViewModel  = new FindJunctionsViewModel();
            var dialog = Dialogs.Show(findJunctionsViewModel, closingEventHandler: (sender, args) => findJunctionsViewModel.Cancel());
            
            var junctions = await findJunctionsViewModel.GetJunctions(selectedDirectory);
            
            foreach (var directoryInfo in junctions)
            {
                Debug.Assert(directoryInfo.Parent != null, "directoryInfo.Parent != null");
                var folderMapping = new DirectoryMapping(directoryInfo.Parent.FullName,
                    Directory.GetParent(JunctionPoint.GetTarget(directoryInfo)).FullName, isSavedMapping: true);
                if (!DisplayedMappings.Contains(folderMapping)) DisplayedMappings.Add(folderMapping);
            }

            var ranToCompletion = (bool?) await dialog.ConfigureAwait(false);
        }

        [AutoLazy.Lazy]
        public DelegateCommand RefreshFolderSizesCommand => new DelegateCommand(() => {
            SourceCollection.RefreshSizes();
            DestinationCollection.RefreshSizes();
        });
    }
}
