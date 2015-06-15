using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using GameMover.Annotations;
using Monitor.Core.Utilities;

namespace GameMover {

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class GameFolder : IComparable<GameFolder>, INotifyPropertyChanged {

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        
        public DirectoryInfo DirectoryInfo { get; private set; }
        public string Name => DirectoryInfo.Name;
        public string JunctionTarget { get; }

        public bool IsJunction { get; set; }

        private long _size;

        public long Size {
            get {
                if (IsJunction) return -1;
                if (_size == 0) {
                    long sizeInBytes =
                        DirectoryInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(fileInfo => fileInfo.Length);
                    _size = sizeInBytes/1024/1024;
                }
                return _size;
            }
            private set {
                _size = value;
                //Allows UI to update with new size
                OnPropertyChanged();
            }
        }

        public GameFolder(DirectoryInfo directory) {
            DirectoryInfo = directory;
            IsJunction = JunctionPoint.Exists(directory);
            if (IsJunction) JunctionTarget = JunctionPoint.GetTarget(directory);
        }

        public GameFolder(string fullPath) : this(new DirectoryInfo(fullPath)) {}

        public void RefreshSize() {
            //Mark size as unknown so that calculation is deferred until next time it is needed
            Size = 0;
        }

        public void Rename(string newName) {
            DirectoryInfo = new DirectoryInfo(DirectoryInfo.Parent?.FullName + @"\" + newName);
            OnPropertyChanged(nameof(Name));
        }

        public bool IsNameEqual(string otherName) {
            return string.Equals(Name, otherName, StringComparison.OrdinalIgnoreCase);
        }

        public int CompareTo(GameFolder other) {
            return string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj) {
            var other = obj as GameFolder;

            return IsNameEqual(other?.Name);
        }

        public override int GetHashCode() {
            return Name.ToLowerInvariant().GetHashCode();
        }


        public static implicit operator DirectoryInfo(GameFolder folder) {
            return folder.DirectoryInfo;
        }

    }

}