using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Centapp.CartoonCommon.ViewModels
{
    public class ItemViewModel : INotifyPropertyChanged
    {

        public override string ToString()
        {
            return string.Format("id={0}, title={1}", Id, Title);
        }

        private int _id;
        public int Id
        {
            get
            {
                return _id;
            }
            set
            {
                if (value != _id)
                {
                    _id = value;
                    NotifyPropertyChanged("Id");
                }
            }
        }

        //public int Progressive { get; set; }
        
        public string OrigId { get; set; }

        public string Title { get; set; }

        private bool _isAvailableInTrial = false;
        public bool IsAvailableInTrial
        {
            get
            {
                return _isAvailableInTrial;
            }
            set
            {
                _isAvailableInTrial = value;
                NotifyPropertyChanged("IsAvailableInTrial");
            }
        }

        private int _dwnRetries = 0;
        public int DwnRetries
        {
            get
            {
                return _dwnRetries;
            }
            set
            {
                _dwnRetries = value;
                NotifyPropertyChanged("DwnRetries");
            }
        }

        private bool _isAvailableOffline = false;
        public bool IsAvailableOffline
        {
            get
            {
                return _isAvailableOffline;
            }
            set
            {
                _isAvailableOffline = value;
                NotifyPropertyChanged("IsAvailableOffline");
            }
        }

        private string _url;
        public string Url
        {
            get
            {
                return _url;
            }
            set
            {
                if (value != _url)
                {
                    _url = value;
                    NotifyPropertyChanged("Url");
                }
            }
        }

        private string _defaultDescr;
        public string DefaultDescr
        {
            get
            {
                return _defaultDescr;
            }
            set
            {
                if (value != _defaultDescr)
                {
                    _defaultDescr = value;
                    NotifyPropertyChanged("DefaultDescr");
                }
            }
        }

        public MyToolkit.Multimedia.YouTubeUri ActualMP4Uri { get; set; }

        private string _OfflineFileName = null;
        public string OfflineFileName
        {
            get
            {
                return _OfflineFileName;
            }
            set
            {
                if (value != _OfflineFileName)
                {
                    _OfflineFileName = value;
                    NotifyPropertyChanged("OfflineFileName");
                }
            }
        }

        private bool _isFavorite = false;
        public bool IsFavorite
        {
            get
            {
                return _isFavorite;
            }
            set
            {
                _isFavorite = value;
                NotifyPropertyChanged("IsFavorite");
            }
        }

        

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }




        public int SeasonId { get; set; }
    }
}