using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

using JetBrains.Annotations;

using Prism.Mvvm;

namespace Junctionizer.Model
{
    public class GameFolderPair : BindableBase, IEquatable<GameFolderPair>, IComparable<GameFolderPair>, IMonitorsAccess
    {
        private GameFolder _sourceEntry;
        [CanBeNull]
        public GameFolder SourceEntry
        {
            get { return _sourceEntry; }
            set {
                if (_sourceEntry != value)
                {
                    if (_sourceEntry != null)
                    {
                        PropertyChangedEventManager.RemoveHandler(_sourceEntry, OnSubPropertyChanged, string.Empty);
                    }

                    _sourceEntry = value;

                    if (_sourceEntry != null)
                    {
                        PropertyChangedEventManager.AddHandler(_sourceEntry, OnSubPropertyChanged, string.Empty);
                    }

                    Debug.Assert(SourceEntry == null || DestinationEntry == null || SourceEntry.Name == DestinationEntry.Name);
                }
            }
        }

        private GameFolder _destinationEntry;
        [CanBeNull]
        public GameFolder DestinationEntry
        {
            get { return _destinationEntry; }
            set {
                if (_destinationEntry != value)
                {
                    if (_destinationEntry != null)
                    {
                        PropertyChangedEventManager.RemoveHandler(_destinationEntry, OnSubPropertyChanged, string.Empty);
                    }

                    _destinationEntry = value;

                    if (_destinationEntry != null)
                    {
                        PropertyChangedEventManager.AddHandler(_destinationEntry, OnSubPropertyChanged, string.Empty);
                    }

                    Debug.Assert(SourceEntry == null || DestinationEntry == null || SourceEntry.Name == DestinationEntry.Name);
                }
            }
        }

        /// <summary>Raises changes for properties in <see cref="IMonitorsAccess"/>.</summary>
        private void OnSubPropertyChanged(object sender, PropertyChangedEventArgs e) => OnPropertyChanged(e);

        /// <inheritdoc/>
        public GameFolderPair([CanBeNull] GameFolder sourceEntry = null, [CanBeNull] GameFolder destinationEntry = null)
        {
            Debug.Assert(sourceEntry != null || destinationEntry != null);

            SourceEntry = sourceEntry;
            DestinationEntry = destinationEntry;

            Name = SourceEntry?.Name ?? DestinationEntry.Name;
            HashCode = Name.GetHashCode();
        }

        public bool IsBeingAccessed => SourceEntry?.IsBeingAccessed == true || DestinationEntry?.IsBeingAccessed == true;
        public bool IsBeingDeleted => SourceEntry?.IsBeingDeleted == true || DestinationEntry?.IsBeingDeleted == true;

        private int HashCode { get; }

        [NotNull]
        public string Name { get; }

        /// <inheritdoc/>
        public bool Equals(GameFolderPair other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return string.Equals(Name, other.Name, StringComparison.Ordinal);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            return obj is GameFolderPair pair && Equals(pair);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode;

        /// <inheritdoc/>
        public int CompareTo(GameFolderPair other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;

            return Comparer<GameFolder>.Default.Compare(SourceEntry ?? DestinationEntry, other.SourceEntry ?? other.DestinationEntry);
        }

        /// <inheritdoc/>
        public override string ToString() => $"{nameof(SourceEntry)}: {SourceEntry}, {nameof(DestinationEntry)}: {DestinationEntry}";
    }
}
