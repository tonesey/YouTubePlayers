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
            BitmapImage image = new BitmapImage();
            bool useOfflineThumb = false;
            string curThumbName = string.Empty;

            if (GenericHelper.AppIsOfflineSettingValue)
            {
                curThumbName = string.Format("thumb_{0}.png", (value as ItemViewModel).Id);
                try
                {
                    using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (isoStore.FileExists(curThumbName))
                        {
                            useOfflineThumb = true;
                        }
                    }
                }
                catch (Exception)
                {
                }
            }

            lock (this)
            {
                if (useOfflineThumb)
                {
                    using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        using (var stream = isoStore.OpenFile(curThumbName, FileMode.Open, FileAccess.Read))
                        {
                            image.SetSource(stream);
                        }
                    }
                }
                else
                {
                    image = new BitmapImage(YouTube.GetThumbnailUri(GenericHelper.GetYoutubeID((value as ItemViewModel).Url)));
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