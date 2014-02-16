using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Data;
using System.Globalization;
using System.Collections.Generic;
using Centapp.CartoonCommon.ViewModels;
using System.Collections.ObjectModel;

namespace Centapp.CartoonCommon.Converters
{
    public class EpisodesToPanItemContentConverter : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int index = int.Parse(parameter.ToString());
            switch (index)
            {
                //case 0:
                //    return string.Format("1-{0}", (value as Collection<ItemViewModel>).Count);
                case 1:
                    return string.Format("26-{0}", 26 + (value as Collection<ItemViewModel>).Count - 1);
                case 2:
                    return string.Format("51-{0}", 51 + (value as Collection<ItemViewModel>).Count - 1);
                case 3:
                    return string.Format("76-{0}", 76 + (value as Collection<ItemViewModel>).Count - 1);
            }
            return "???";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
