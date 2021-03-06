﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;

using JetBrains.Annotations;

using MaterialDesignColors;

using MaterialDesignThemes.Wpf;

using Newtonsoft.Json;

using PropertyChanged;

using Utilities;

namespace Junctionizer.ViewModels
{
    [ImplementPropertyChanged]
    public class UISettings
    {
        [JsonIgnore]
        public bool IsModifyingFileSystem { get; set; }

        public bool IsCompactInterface { get; set; } = true;

        public bool AutomaticallySwitchInterfaces { get; set; } = true;

        [UsedImplicitly]
        private void OnAutomaticallySwitchInterfacesChanged() => CheckWindowSize();

        /// <summary>Performs any necessary settings modifications based on the current window size.</summary>
        internal void CheckWindowSize()
        {
            if (AutomaticallySwitchInterfaces)
            {
                IsCompactInterface = Application.Current.MainWindow.ActualWidth < 1200 &&
                                     Application.Current.MainWindow.WindowState != WindowState.Maximized;
            }
        }
        
        [JsonIgnore]
        public bool IsRightDrawerOpen { get; set; } = false;

        private ResourceDictionary ThemedColorsDictionary { get; }
            = Application.Current.Resources.MergedDictionaries
                         .Where(rd => rd.Source != null)
                         .Single(rd => Regex.IsMatch(rd.Source.OriginalString, @".*Colors\.(Light|Dark)"));

        private void ReplaceThemedColorsDictionary()
        {
            var currentUri = ThemedColorsDictionary.Source.AbsoluteUri;
            ThemedColorsDictionary.Source = new Uri(_isDarkTheme
                                                        ? currentUri.Replace("Light.xaml", "Dark.xaml")
                                                        : currentUri.Replace("Dark.xaml", "Light.xaml"));
        }

        private bool _isDarkTheme;
        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set {
                _isDarkTheme = value;
                PaletteHelper.SetLightDark(_isDarkTheme);
                ReplaceThemedColorsDictionary();
            }
        }

        private Swatch _primarySwatch;
        public Swatch PrimarySwatch
        {
            [UsedImplicitly] get => _primarySwatch;
            set {
                _primarySwatch = value;
                PaletteHelper.ReplacePrimaryColor(value);
                ReplaceThemedColorsDictionary();
            }
        }

        private Swatch _accentSwatch;
        public Swatch AccentSwatch
        {
            [UsedImplicitly] get => _accentSwatch;
            set {
                _accentSwatch = value;
                PaletteHelper.ReplaceAccentColor(_accentSwatch);
                ReplaceThemedColorsDictionary();
            }
        }
        
        public IReadOnlyList<int> ColumnOrderingSourceGrid { get; set; }
        public IReadOnlyList<int> ColumnOrderingDestinationGrid { get; set; }
        public IReadOnlyList<int> ColumnOrderingCompactGrid { get; set; }

        private PaletteHelper PaletteHelper { get; } = new PaletteHelper();
        
        internal static PauseTokenSource PauseTokenSource { get; } = new PauseTokenSource();

        private UISettings()
        {
            // Set swatch values to default values (defined in App.xaml) in case there aren't any saved values available to load
            var palette = PaletteHelper.QueryPalette();
            AccentSwatch = palette.AccentSwatch;
            PrimarySwatch = palette.PrimarySwatch;

            PauseTokenSource.PropertyChanged += (sender, args) => {
                IsModifyingFileSystem = PauseTokenSource.IsPaused;
            };

            var propertyNames = GetType()
                .GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)
                .Where(propertyInfo => propertyInfo.GetCustomAttribute<JsonIgnoreAttribute>() == null)
                .Select(propertyInfo => propertyInfo.Name)
                .ToArray();

            Settings.StateTracker
                    .Configure(this)
                    .AddProperties(propertyNames)
                    .Apply();
        }

        public static UISettings Instance { get; } = new UISettings();
    }
}
