using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Threading;

using GameMover.Code;
using GameMover.Model;

using JetBrains.Annotations;

using MaterialDesignThemes.Wpf;

using Microsoft.VisualStudio.Threading;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

using Newtonsoft.Json;

using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;

using Utilities.Strings;

using static GameMover.Code.StaticMethods;

namespace GameMover.ViewModels
{
    public class MainWindowViewModel : BindableBase
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
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");

            (SourceCollection, DestinationCollection) = FolderCollection.Factory.CreatePair(() => SynchronizationContext.Current is DispatcherSynchronizationContext);

            SourceCollection.FolderBrowserDefaultLocation =
                regKey == null ? @"C:" : regKey.GetValue("SteamPath").ToString().Replace(@"/", @"\") + @"\steamapps\common";
            DestinationCollection.FolderBrowserDefaultLocation = @"E:\Steam\SteamApps\common";

            MergedCollection = new MergedItemEnumerable(SourceCollection, DestinationCollection);

            BindingOperations.EnableCollectionSynchronization(DisplayedMappings, new object());

            SourceCollection.PropertyChanged += OnFolderCollectionPropertyChange;
            DestinationCollection.PropertyChanged += OnFolderCollectionPropertyChange;

            var appDataDirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                nameof(GameMover));
            SavedMappingsFilePath = Path.Combine(Directory.CreateDirectory(appDataDirectoryPath).FullName, "JunctionDirectories.json");

            var deserializedMappings =
                JsonConvert.DeserializeObject<List<DirectoryMapping>>(File.ReadAllText(SavedMappingsFilePath, Encoding.UTF8));
            deserializedMappings.ForEach(mapping => {
                mapping.IsSavedMapping = true;
                DisplayedMappings.Add(mapping);
            });

            DisplayedMappings.CollectionChanged += (sender, args) => WriteSavedMappings();
        }

        /// <summary>Fody PropertyChanged handles creating change notification whenever something this function depends on changes.</summary>
        [NotNull]
        [AutoLazy.Lazy]
        public Func<GameFolder, bool> PassesFilter => folder =>
            folder.Name.Contains(FilterNameText, StringComparison.OrdinalIgnoreCase)
            && FilterLowerSizeLimit <= folder.Size && folder.Size <= FilterUpperSizeLimit;

        public IObservable<EventPattern<PropertyChangedEventArgs>> PassesFilterChangedObservable { get; }

        public string FilterNameText { get; set; } = string.Empty;

        public double FilterLowerSizeLimit { get; set; } = double.NegativeInfinity;
        public double FilterUpperSizeLimit { get; set; } = double.PositiveInfinity;

        public FindJunctionsViewModel FindJunctionsViewModel { get; } = new FindJunctionsViewModel();
        public InteractionRequest<INotification> DisplayFindJunctionsDialogRequest { get; } = new InteractionRequest<INotification>();
        public InteractionRequest<INotification> CloseDialogRequest { get; } = new InteractionRequest<INotification>();

        [AutoLazy.Lazy]
        public DelegateCommand<DialogClosingEventArgs> DialogClosedCommand
            => new DelegateCommand<DialogClosingEventArgs>(args => OnDialogClosed?.Invoke());
        private event Action OnDialogClosed;

        public FolderCollection SourceCollection { get; private set; }
        public FolderCollection DestinationCollection { get; private set; }

        public MergedItemEnumerable MergedCollection { get; private set; }

        public ObservableCollection<DirectoryMapping> DisplayedMappings { get; } = new ObservableCollection<DirectoryMapping>();

        private string SavedMappingsFilePath { get; set; }
        private bool IsSelectedMappingModificationAllowed { get; set; } = true;

        private DirectoryMapping _selectedMapping;
        public DirectoryMapping SelectedMapping
        {
            get { return _selectedMapping; }
            set {
                var previousValue = _selectedMapping;
                _selectedMapping = value;
                if (!Directory.Exists(_selectedMapping.Source))
                {
                    DisplayedMappings.Remove(_selectedMapping);
                    _selectedMapping = new DirectoryMapping(null, _selectedMapping.Destination);
                }
                if (!Directory.Exists(_selectedMapping.Destination))
                {
                    DisplayedMappings.Remove(_selectedMapping);
                    _selectedMapping = new DirectoryMapping(_selectedMapping.Source, null);
                }

                if (!DisplayedMappings.Contains(_selectedMapping)) DisplayedMappings.Add(_selectedMapping);
                if (previousValue?.IsSavedMapping == false) DisplayedMappings.Remove(previousValue);

                if (previousValue != null) previousValue.PropertyChanged -= SelectedMappingPropertyChanged;
                _selectedMapping.PropertyChanged += SelectedMappingPropertyChanged;

                IsSelectedMappingModificationAllowed = false;
                SourceCollection.Location = _selectedMapping.Source;
                DestinationCollection.Location = _selectedMapping.Destination;
                IsSelectedMappingModificationAllowed = true;
            }
        }

        private void WriteSavedMappings()
        {
            string json = JsonConvert.SerializeObject(DisplayedMappings.Where(mapping => mapping.IsSavedMapping), Formatting.Indented);
            File.WriteAllText(SavedMappingsFilePath, json, Encoding.UTF8);
        }

        private void SelectedMappingPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(DirectoryMapping.IsSavedMapping)))
            {
                WriteSavedMappings();
                SaveCurrentLocationCommand.RaiseCanExecuteChanged();
                DeleteCurrentLocationCommand.RaiseCanExecuteChanged();
            }
        }

        private void OnFolderCollectionPropertyChange(object sender, PropertyChangedEventArgs args)
        {
            // When a new folder location is chosen, check if it is already saved and if so select it so that it can be displayed in the combo box
            if (args.PropertyName.Equals(nameof(FolderCollection.Location)) && IsSelectedMappingModificationAllowed)
            {
                SelectedMapping = new DirectoryMapping(SourceCollection.Location, DestinationCollection.Location);
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
            var folderDialog = NewFolderDialog("Select Root Directory");
            if (folderDialog.ShowDialog() != CommonFileDialogResult.Ok) return;

            var selectedPath = folderDialog.FileName;

            DisplayFindJunctionsDialogRequest.Raise(null);
            OnDialogClosed = () => FindJunctionsViewModel.Cancel();

            var junctions = await FindJunctionsViewModel.GetJunctions(selectedPath);

            foreach (var directoryInfo in junctions)
            {
                Debug.Assert(directoryInfo.Parent != null, "directoryInfo.Parent != null");
                var folderMapping = new DirectoryMapping(directoryInfo.Parent.FullName,
                    Directory.GetParent(JunctionPoint.GetTarget(directoryInfo)).FullName, isSavedMapping: true);
                if (!DisplayedMappings.Contains(folderMapping)) DisplayedMappings.Add(folderMapping);
            }
        }

        [AutoLazy.Lazy]
        public DelegateCommand RefreshFoldersCommand => new DelegateCommand(() => {
            SourceCollection.Refresh();
            DestinationCollection.Refresh();
        });
    }
}
