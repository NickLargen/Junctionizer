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

        public DirectoryInfo DirectoryInfo { get; }
        public string JunctionTarget { get; }
        public string Name { get; }

        public bool IsJunction { get; set; }

        private long _size;
        public long Size {
            get {
                if (IsJunction) return -1;
                if (_size != 0) return _size;
                return CalculateAndSetCurrentSize();
            }
        }

        public GameFolder(DirectoryInfo directory) {
            DirectoryInfo = directory;
            Name = directory.Name;
            IsJunction = JunctionPoint.Exists(directory);
            if (IsJunction) JunctionTarget = JunctionPoint.GetTarget(directory);
        }


        public void RefreshSize() {
            CalculateAndSetCurrentSize();
            //Allows UI to update with new size
            OnPropertyChanged((nameof(Size)));
        }

        private long CalculateAndSetCurrentSize() {
            long sizeInBytes =
                DirectoryInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(fileInfo => fileInfo.Length);
            _size = sizeInBytes/1024/1024;
            return _size;
        }


        public int CompareTo(GameFolder other) {
            return string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj) {
//            if (obj == null || GetType() != obj.GetType()) {
//                return false;
//            }
            var other = obj as GameFolder;

            return string.Equals(Name, other?.Name, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode() {
            return Name.ToLowerInvariant().GetHashCode();
        }


        public static implicit operator DirectoryInfo(GameFolder folder) {
            return folder.DirectoryInfo;
        }

    }

}