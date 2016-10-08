using System;
using System.IO;
using System.Linq;
using System.Reflection;

using GameMover.Code;
using GameMover.External_Code;

using Prism.Mvvm;

namespace GameMover.Model
{

    public class GameFolder : BindableBase, IComparable<GameFolder>, IEquatable<GameFolder>
    {

        public DirectoryInfo DirectoryInfo { get; private set; }
        public string Name => DirectoryInfo.Name;
        public string JunctionTarget { get; }

        public bool IsJunction { get; }

        private long _size;

        public long Size
        {
            get {
                if (IsJunction) return -1;

                if (_size == 0)
                {
                    try
                    {
                        var sizeInBytes =
                      DirectoryInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(fileInfo => fileInfo.Length);
                        _size = sizeInBytes / 1024 / 1024;
                    }
                    catch (IOException e)
                    {
                        var message = e.Message;
                        var maybeFullPath = e.GetType().GetField("_maybeFullPath", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
                        if (maybeFullPath != null)
                        {
                            message += $" \"{maybeFullPath.GetValue(e)}\"";
                        }
                        StaticMethods.DisplayError(message, e);
                    }
                   
                }

                return _size;
            }
            private set { _size = value; }
        }

        public GameFolder(DirectoryInfo directory)
        {
            DirectoryInfo = directory;
            IsJunction = JunctionPoint.Exists(directory);
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
        }

        public int CompareTo(GameFolder other) => other == null
                                                      ? 1
                                                      : string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object obj) => CompareTo(obj as GameFolder) == 0;

        public bool Equals(GameFolder other) => CompareTo(other) == 0;

        public override int GetHashCode() => Name.ToLowerInvariant().GetHashCode();

//        public static implicit operator DirectoryInfo(GameFolder folder) => folder.DirectoryInfo;
    }

}
