using Centapp.CartoonCommon;
using Centapp.CartoonCommon.ViewModels;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;


namespace Centapp.CartoonCommon.Converters
{
    public class ItemToDescrConverter : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ItemViewModel item = value as ItemViewModel;
            if (item != null)
            {
                string localizedDescr = AppResources.ResourceManager.GetString(string.Format("ep_{0}",
                                                                               (value as ItemViewModel).Id.ToString().PadLeft(2, '0')));
                return localizedDescr.Trim();
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
        #endregion
    }
}