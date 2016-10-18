using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

using GameMover.Code;
using GameMover.External_Code;

using Prism.Mvvm;

namespace GameMover.Model
{

    [DebuggerDisplay(nameof(GameFolder) + " {" + nameof(DirectoryInfo) + "}")]
    public class GameFolder : BindableBase, IComparable<GameFolder>, IEquatable<GameFolder>
    {

        public DirectoryInfo DirectoryInfo { get; private set; }
        public string Name => DirectoryInfo.Name;
        public string JunctionTarget { get; }

        private DateTime? _lastWriteTime;
        public DateTime? LastWriteTime
        {
            get {
                return _lastWriteTime ?? (_lastWriteTime = DirectoryInfo.EnumerateAllAccessibleDirectories()
                                                                        .DefaultIfEmpty(DirectoryInfo)
                                                                        .Max(info => info.LastWriteTime));
            }
            private set { _lastWriteTime = value; }
        }

        public bool IsJunction { get; }

        public bool IsSizeUnknown { get; set; } = true;

        private long _size;
        public long Size
        {
            get {
                if (IsJunction)
                {
                    IsSizeUnknown = false;
                    return -1;
                }

                if (IsSizeUnknown)
                {
                    try
                    {
                        _size = DirectoryInfo.EnumerateAllAccessibleDirectories()
                                             .SelectMany(info => info.EnumerateFiles())
                                             .Sum(fileInfo => fileInfo.Length);
                        IsSizeUnknown = false;
                    }
                    catch (IOException e)
                    {
                        var message = e.Message;
                        var maybeFullPath = e.GetType()
                                             .GetField("_maybeFullPath",
                                                 BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
                        if (maybeFullPath != null)
                        {
                            message += $" \"{maybeFullPath.GetValue(e)}\"";
                        }
                        StaticMethods.HandleError(message, e);
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
        public static bool operator ==(GameFolder left, GameFolder right) => Equals(left, right);
        public static bool operator !=(GameFolder left, GameFolder right) => !Equals(left, right);

        public override int GetHashCode() => Name.ToLowerInvariant().GetHashCode();

        //        public static implicit operator DirectoryInfo(GameFolder folder) => folder.DirectoryInfo;
    }

}
