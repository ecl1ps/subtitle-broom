using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SubtitleBroom.Converters
{
    public class CountToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int count;
            if (value is int)
                count = (int) value;
            else if (value is string)
                count = int.Parse(((string)value).Contains("(") ? ((string)value).Substring(0, ((string)value).IndexOf("(", StringComparison.InvariantCultureIgnoreCase)) : (string)value);
            else
                return Brushes.Black;

            return count > 0 ? Brushes.Red : Brushes.Green;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
