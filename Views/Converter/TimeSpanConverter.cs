using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SMBClient.Views
{
    public class TimeSpanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var timeSpan = (TimeSpan)value;

            if (timeSpan.TotalSeconds < 1)
            {
                return "< 1s";
            }

            return string.Join(" ", new string?[]
            {
                timeSpan.Days > 0 ? timeSpan.Days + "d" : null,
                timeSpan.Hours > 0 ? timeSpan.Hours + "h" : null,
                timeSpan.Minutes > 0 ? timeSpan.Minutes + "m" : null,
                timeSpan.Seconds > 0 ? timeSpan.Seconds + "s" : null,
            }.Where(x => x != null));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
