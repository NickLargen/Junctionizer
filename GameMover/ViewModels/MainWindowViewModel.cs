using System;
using System.ComponentModel;
using System.IO;
using System.Linq;

using GameMover.Code;
using GameMover.External_Code;
using GameMover.Model;

using MaterialDesignThemes.Wpf;

using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;

using static GameMover.Code.StaticMethods;

namespace GameMover.ViewModels
{

    public class MainWindowViewModel : BindableBase
    {

        public FindJunctionsViewModel FindJunctionsViewModel { get; } = new FindJunctionsViewModel();

        public InteractionRequest<INotification> CloseDialogRequest { get; } = new InteractionRequest<INotification>();

        [AutoLazy.Lazy]
        public DelegateCommand<DialogClosingEventArgs> DialogClosedCommand => new DelegateCommand<DialogClosingEventArgs>(args => OnDialogClosed?.Invoke());
        private event Action OnDialogClosed;

        /// Allows you to set the selection without trigger selection change (so that when saving a control you don't reload)
        private bool _ignorePathsSelectionChange;

        public FolderCollection InstallCollection { get; private set; }
        public FolderCollection StorageCollection { get; private set; }

        public AsyncObservableCollection<string> SavedPaths { get; } = new AsyncObservableCollection<string>();

        private string _selectedPath;
        public string SelectedPath
        {
            get { return _selectedPath; }
            set {
                if (_selectedPath == value) return;

                _selectedPath = value;

                if (_ignorePathsSelectionChange)
                {
                    _ignorePathsSelectionChange = false;
                    return;
                }

                if (_selectedPath == null) return;

                string[] paths = _selectedPath?.Split(new[] {ARROWED_PATH_SEPARATOR}, StringSplitOptions.RemoveEmptyEntries);

                if (paths.Length != 2) throw new Exception($"Unable to parse selected path \"{_selectedPath}\".");

                InstallCollection.Location = paths[0];
                StorageCollection.Location = paths[1];
            }
        }


        private const string ARROWED_PATH_SEPARATOR = " -> ";

        public void Initialize()
        {
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            var installSteamCommon = regKey == null
                                         ? @"C:"
                                         : regKey.GetValue("SteamPath").ToString().Replace(@"/", @"\") + @"\steamapps\common";

            InstallCollection = new FolderCollection {
                FolderBrowserDefaultLocation = installSteamCommon
            };
            StorageCollection = new FolderCollection {
                FolderBrowserDefaultLocation = @"E:\Steam\SteamApps\common"
            };

            InstallCollection.CorrespondingCollection = StorageCollection;
            StorageCollection.CorrespondingCollection = InstallCollection;

            InstallCollection.PropertyChanged += OnFolderCollectionChange;
            StorageCollection.PropertyChanged += OnFolderCollectionChange;


            SavedPaths.Add(InstallCollection.FolderBrowserDefaultLocation + ARROWED_PATH_SEPARATOR +
                           StorageCollection.FolderBrowserDefaultLocation);
            SavedPaths.Add(@"C:\Users\Nick\Desktop\folder a -> C:\Users\Nick\Desktop\folder b");
        }

        [AutoLazy.Lazy]
        public DelegateCommand SaveCurrentLocationCommand => new DelegateCommand(() => {
            string arrowedPath = GetCurrentLocationsString();

            if (SavedPaths.Contains(arrowedPath) == false)
            {
                SavedPaths.Add(arrowedPath);

                SilentlySetSavedPath(arrowedPath);
            }
        }, () => true);

        [AutoLazy.Lazy]
        public DelegateCommand DeleteCurrentLocationCommand => new DelegateCommand(() => {
            if (SelectedPath != null) SavedPaths.Remove(SelectedPath);
        }, () => true);

        [AutoLazy.Lazy]
        public DelegateCommand FindExistingJunctionsCommand => new DelegateCommand(async () => {
            var folderDialog = NewFolderDialog("Select Root Directory");
            if (folderDialog.ShowDialog() != CommonFileDialogResult.Ok) return;

            var selectedPath = folderDialog.FileName;

            OnDialogClosed = () => FindJunctionsViewModel.Cancel();
            var junctions = await FindJunctionsViewModel.GetJunctions(selectedPath);
            CloseDialogRequest.Raise(null);

            foreach (var directoryInfo in junctions)
            {
                var newPath = directoryInfo.Parent.FullName + ARROWED_PATH_SEPARATOR +
                              Directory.GetParent(JunctionPoint.GetTarget(directoryInfo)).FullName;
                if (!SavedPaths.Contains(newPath)) SavedPaths.Add(newPath);
            }
        }, () => true);

        private void OnFolderCollectionChange(object sender, PropertyChangedEventArgs args)
        {
            // When a new folder location is chosen, check if it is already saved and if so display select it so that it can be displayed in the combo box
            if (args.PropertyName.Equals(nameof(FolderCollection.Location)))
            {
                SilentlySetSavedPath(SavedPaths.FirstOrDefault(
                    s => s.Equals(GetCurrentLocationsString(), StringComparison.OrdinalIgnoreCase)));
            }
        }

        private void SilentlySetSavedPath(string newPath)
        {
            if (SelectedPath == newPath) return;

            _ignorePathsSelectionChange = true;
            SelectedPath = newPath;
        }

        private string GetCurrentLocationsString()
        {
            return $"{InstallCollection.Location}{ARROWED_PATH_SEPARATOR}{StorageCollection.Location}";
        }

    }

}
