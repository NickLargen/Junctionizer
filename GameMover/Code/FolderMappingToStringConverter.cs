using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using GameMover.Model;

namespace GameMover.Code
{
    class FolderMappingToStringConverter :IValueConverter
    {

        private const string SEPARATOR = " => ";

        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            FolderMapping obj = (FolderMapping) value;
            return obj.Source + SEPARATOR + obj.Destination;
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();

            var _selectedPath = (string) value;
            string[] paths = _selectedPath?.Split(new[] { SEPARATOR }, StringSplitOptions.RemoveEmptyEntries);

            if (paths.Length != 2) throw new Exception($"Unable to parse selected path \"{_selectedPath}\".");

            return new FolderMapping(paths[0], paths[1]);
        }

    }
}
