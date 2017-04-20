using System;
using System.IO;

using Jot;
using Jot.Storage;
using Jot.Triggers;

namespace Junctionizer
{
    public static class Settings
    {
        public static string AppDataDirectoryPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            nameof(Junctionizer) + "Config");

        public static StateTracker StateTracker { get; } = new StateTracker(new JsonFileStoreFactory(AppDataDirectoryPath), new DesktopPersistTrigger());
    }
}
