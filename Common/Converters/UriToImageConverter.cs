using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using MyToolkit.Multimedia;
using Centapp.CartoonCommon.Helpers;
using Centapp.CartoonCommon.ViewModels;


namespace Centapp.CartoonCommon.Converters
{
    public class UriToImageConverter : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) {
                return null;
            }
            //return new BitmapImage(new Uri(string.Format("/Resources/thumb/{0}.png", (int)value), UriKind.Relative));
            return YouTube.GetThumbnailUri(GenericHelper.GetYoutubeID((value as ItemViewModel).Url), YouTubeThumbnailSize.Small);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
        #endregion
    }
}