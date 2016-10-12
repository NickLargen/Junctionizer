using System;

using Prism.Mvvm;

namespace GameMover.Model
{

    public sealed class FolderMapping : BindableBase, IEquatable<FolderMapping>
    {

        public string Source { get; }
        public string Destination { get; }
        public bool SaveMapping { get; set; }

        public FolderMapping(string source, string destination, bool saveMapping = false)
        {
            Source = source;
            Destination = destination;
            SaveMapping = saveMapping;
        }

        private bool EqualsInternal(FolderMapping other)
        {
            return string.Equals(Destination, other.Destination, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(Source, other.Source, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public bool Equals(FolderMapping other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return EqualsInternal(other);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;

            return EqualsInternal((FolderMapping) obj);
        }

        public static bool operator ==(FolderMapping left, FolderMapping right) => Equals(left, right);
        public static bool operator !=(FolderMapping left, FolderMapping right) => !Equals(left, right);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Destination != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(Destination) : 0) * 397) ^
                       (Source != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(Source) : 0);
            }
        }

    }

}
