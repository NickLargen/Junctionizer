using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

using AutoLazy;

using MaterialDesignColors;

using Prism.Commands;

namespace Junctionizer.ViewModels
{
    public class PaletteSelectorViewModel : ViewModelBase
    {
        private static Comparer<Hue> HueComparer { get; } = Comparer<Hue>.Create((x, y) => GetComparableHue(x.Color).CompareTo(GetComparableHue(y.Color)));

        private static float GetComparableHue(Color c)
        {
            var color = System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B);
            return (color.GetHue() + 100) % 360;
        }

        public static IEnumerable<Swatch> Swatches { get; } =
            new SwatchesProvider().Swatches
                                  .OrderByDescending(swatch => swatch.IsAccented)
                                  .ThenByDescending(swatch => swatch.ExemplarHue, HueComparer)
                                  .ToList();

        [Lazy]
        public DelegateCommand<Swatch> ApplyPrimaryCommand => new DelegateCommand<Swatch>(swatch => {
            UISettings.PrimarySwatch = swatch;
        });

        [Lazy]
        public DelegateCommand<Swatch> ApplyAccentCommand => new DelegateCommand<Swatch>(swatch => {
            UISettings.AccentSwatch = swatch;
        });
    }
}
