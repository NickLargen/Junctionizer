using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using GameMover.Model;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;

namespace GameMover.ViewModels
{

    public class ViewModel : BindableBase
    {

        /// Allows you to set the selection without trigger selection change (so that when saving a control you don't reload)
        private bool _ignorePathsSelectionChange = false;

        private string _selectedBoxPath;

        public FolderCollection InstallPane { get; } = new FolderCollection();

        public FolderCollection StoragePane { get; } = new FolderCollection();

        public ObservableCollection<string> BoxPathsObservableCollection { get; } = new ObservableCollection<string>();

        public string SelectedBoxPath
        {
            get { return _selectedBoxPath; }
            set {
                if (_selectedBoxPath == value) return;

                _selectedBoxPath = value;

                if (_ignorePathsSelectionChange)
                {
                    _ignorePathsSelectionChange = false;
                    return;
                }

                string[] paths = SelectedBoxPath?.Split(new[] {ARROWED_PATH_SEPARATOR}, StringSplitOptions.RemoveEmptyEntries);

                //TODO fail horribly?
                if (paths?.Length != 2) return;

                InstallPane.SetLocation(paths[0]);
                StoragePane.SetLocation(paths[1]);
            }
        }

        private const string ARROWED_PATH_SEPARATOR = " -> ";

        public ViewModel()
        {
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");

            var installSteamCommon = regKey == null ? @"C:" : regKey.GetValue("SteamPath").ToString().Replace(@"/", @"\") + @"\steamapps\common";



            InstallPane.PropertyChanged += OnFolderCollectionChange;
            StoragePane.PropertyChanged += OnFolderCollectionChange;

            InstallPane.SteamCommonFolderGuess = installSteamCommon;
            InstallPane.OtherPane = StoragePane;

            StoragePane.SteamCommonFolderGuess = @"E:\Steam\SteamApps\common";
            StoragePane.OtherPane = InstallPane;

            BoxPathsObservableCollection.Add(InstallPane.SteamCommonFolderGuess + ARROWED_PATH_SEPARATOR + StoragePane.SteamCommonFolderGuess);
        }

        private void OnFolderCollectionChange(object sender, PropertyChangedEventArgs args)
        {
            // When a new folder location is chosen, check if it is already saved and if so display select it so that it can be displayed in the combo box
            if (args.PropertyName.Equals(nameof(FolderCollection.Location)))
            {
                ChangeBoxPathWithoutTriggerSelectionChange(BoxPathsObservableCollection.FirstOrDefault(s => s.Equals(GetCurrentLocationsString(), StringComparison.OrdinalIgnoreCase)));
            }
        }



        [AutoLazy.Lazy]
        public DelegateCommand SaveCurrentLocationCommand => new DelegateCommand(() => {
            string arrowedPath = GetCurrentLocationsString();

            if (BoxPathsObservableCollection.Contains(arrowedPath) == false)
            {
                BoxPathsObservableCollection.Add(arrowedPath);

                ChangeBoxPathWithoutTriggerSelectionChange(arrowedPath);
            }
        }, () => true);

        private void ChangeBoxPathWithoutTriggerSelectionChange(string newPath)
        {
            if (SelectedBoxPath == newPath) return;

            _ignorePathsSelectionChange = true;
            SelectedBoxPath = newPath;
        }

        private string GetCurrentLocationsString() => $"{InstallPane.Location}{ARROWED_PATH_SEPARATOR}{StoragePane.Location}";

        [AutoLazy.Lazy]
        public DelegateCommand DeleteCurrentLocationCommand => new DelegateCommand(() => {
            if (SelectedBoxPath != null) BoxPathsObservableCollection.Remove(SelectedBoxPath);
        }, () => true);

    }

}
