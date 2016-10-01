using System;
using System.IO;
using System.Linq;
using GameMover.External_Code;
using Prism.Mvvm;

namespace GameMover.Model
{
    [PropertyChanged.DoNotNotify]
    public class GameFolder : BindableBase, IComparable<GameFolder>, IEquatable<GameFolder>
    {
        public DirectoryInfo DirectoryInfo { get; private set; }
        public string Name => DirectoryInfo.Name;
        public string JunctionTarget { get; }

        public bool IsJunction { get; set; }

        private long _size;

        public long Size
        {
            get {
                if (IsJunction) return -1;
                if (_size == 0)
                {
                    var sizeInBytes =
                        DirectoryInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(fileInfo => fileInfo.Length);
                    _size = sizeInBytes / 1024 / 1024;
                }
                return _size;
            }
            private set {
                if (value != _size)
                {
                    _size = value;
                    OnPropertyChanged();
                }
            }
        }

        public GameFolder(DirectoryInfo directory)
        {
            DirectoryInfo = directory;
            try
            {
                IsJunction = JunctionPoint.Exists(directory);
            }
            catch (IOException)
            {
                // Hack to get around file in use by another process error
                IsJunction = JunctionPoint.Exists(directory);
            }

            if (IsJunction) JunctionTarget = JunctionPoint.GetTarget(directory);
        }

        public GameFolder(string fullPath) : this(new DirectoryInfo(fullPath)) {}

        public void RecalculateSize()
        {
            // Mark size as unknown so that calculation is deferred until next time it is needed
            Size = 0;
        }

        public void Rename(string newName)
        {
            DirectoryInfo = new DirectoryInfo(DirectoryInfo.Parent?.FullName + @"\" + newName);
            OnPropertyChanged(nameof(Name));
        }

        public int CompareTo(GameFolder other) => other == null ? 1 : string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object obj) => CompareTo(obj as GameFolder) == 0;

        public bool Equals(GameFolder other) => CompareTo(other) == 0;

        public override int GetHashCode() => Name.ToLowerInvariant().GetHashCode();

        public static implicit operator DirectoryInfo(GameFolder folder) => folder.DirectoryInfo;

    }

}
