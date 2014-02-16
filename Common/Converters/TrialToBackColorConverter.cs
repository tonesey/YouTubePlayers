using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;

namespace Centapp.CartoonCommon.Converters
{
    public class TrialToBackColorConverter : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (!((App)Application.Current).IsTrial)
                {
                    return Colors.White;
                }
            }
            catch (Exception)
            {
                return Colors.White;
            }
            
            bool val = bool.Parse(value.ToString());
           return val ?  Colors.White : Colors.Red;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
        #endregion

        
    }
}