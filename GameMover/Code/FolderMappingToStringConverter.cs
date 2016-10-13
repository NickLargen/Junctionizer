using System;
using System.Globalization;
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
        }

    }
}
