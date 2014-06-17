using System.IO;
using Centapp.CartoonCommon.Helpers;
using Centapp.CartoonCommon.ViewModels;
using MyToolkit.Multimedia;
using System;
using System.Globalization;
using System.IO.IsolatedStorage;
using System.Windows.Data;
using System.Windows.Media.Imaging;


namespace Centapp.CartoonCommon.Converters
{
    public class IdToImageConverter : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

#if DEBUGOFFLINE
            return null;
#endif 
            if (!GenericHelper.AppIsOfflineSettingValue)
            {
                //return new BitmapImage(new Uri(string.Format("/Resources/thumb/{0}.png", (int)value), UriKind.Relative));
                return new BitmapImage(YouTube.GetThumbnailUri(GenericHelper.GetYoutubeID((value as ItemViewModel).Url)));
            }

            BitmapImage image = new BitmapImage();
            using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                var curThumbName = string.Format("thumb_{0}.png", (value as ItemViewModel).Id);
                if (!isoStore.FileExists(curThumbName))
                {
                    //prevedere un img di default? in teoria non dovrebbe MAI passare da qua se offline
                    curThumbName = string.Format("thumb_1.png");
                }
                using (var stream = IsolatedStorageFile.GetUserStoreForApplication().OpenFile(curThumbName, FileMode.Open, FileAccess.Read))
                {
                    image.SetSource(stream);
                }
            }
            return image;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
        #endregion
    }
}