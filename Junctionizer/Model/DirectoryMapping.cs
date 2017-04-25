using System;

using JetBrains.Annotations;

using Prism.Mvvm;

namespace Junctionizer.Model
{
    /// <summary>Container for two directory location.</summary>
    public sealed class DirectoryMapping : BindableBase, IEquatable<DirectoryMapping>
    {
        public DirectoryMapping([CanBeNull] string source, [CanBeNull] string destination, bool isSavedMapping = false)
        {
            Source = source;
            Destination = destination;
            IsSavedMapping = isSavedMapping;
        }

        [CanBeNull]
        public string Source { get; }

        [CanBeNull]
        public string Destination { get; }

        public bool IsSavedMapping { get; set; }

        /// <inheritdoc/>
        public override string ToString() => $"{Source} → {Destination}";

        /// <inheritdoc/>
        public bool Equals(DirectoryMapping other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return EqualsInternal(other);
        }

        private bool EqualsInternal(DirectoryMapping other)
        {
            return string.Equals(Destination, other.Destination, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(Source, other.Source, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;

            return EqualsInternal((DirectoryMapping) obj);
        }

        public static bool operator ==(DirectoryMapping left, DirectoryMapping right) => Equals(left, right);
        public static bool operator !=(DirectoryMapping left, DirectoryMapping right) => !Equals(left, right);

        /// <inheritdoc/>
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
