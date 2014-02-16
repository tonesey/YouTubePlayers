using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Resources;


namespace Centapp.CartoonCommon.Converters
{
    public class IdToTitleConverter : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string localizedDescr = string.Empty;

            if (culture == null)
            {
                //utilizza lingua corrente app
                localizedDescr = App.ResManager.GetString(string.Format("ep_{0}", value.ToString().PadLeft(2, '0')));
            }
            else
            {
                //utilizza lingua specifica del parametro (es. per forzare traduzioni inglesi titoli Pingu su broken link)
                localizedDescr = App.ResManager.GetString(string.Format("ep_{0}", value.ToString().PadLeft(2, '0')), culture);
            }

            return localizedDescr.Trim();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
        #endregion

        internal string Convert(int id)
        {
            return Convert(id, null, null, null).ToString();
        }
    }
}