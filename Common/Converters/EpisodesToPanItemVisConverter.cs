using System;
using System.Linq;
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
    public class EpisodesToPanItemVisConverter : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int index = int.Parse(parameter.ToString());
            if (AppInfo.Instance.UseJSon)
            {
                //verifico che esista la stagione con l'id passato come parametro
                var vis = (App.ViewModel.Items.FirstOrDefault(it => it.SeasonId == (index + 1)) != null);
                return vis ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                switch (index)
                {
                    //case 0:
                    //    return (value as Collection<ItemViewModel>).Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                    case 1:
                        return (value as Collection<ItemViewModel>).Count > 25 ? Visibility.Visible : Visibility.Collapsed;
                    case 2:
                        return (value as Collection<ItemViewModel>).Count > 50 ? Visibility.Visible : Visibility.Collapsed;
                    case 3:
                        return (value as Collection<ItemViewModel>).Count > 75 ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
