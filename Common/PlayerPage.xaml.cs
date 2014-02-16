using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using MyToolkit.Multimedia;
using Microsoft.SilverlightMediaFramework.Core.Media;
using Microsoft.SilverlightMediaFramework.Plugins.Primitives;
using System.IO.IsolatedStorage;
using System.IO;
using Centapp.CartoonCommon.Helpers;

namespace Centapp.CartoonCommon
{
    public partial class PlayerPage : PhoneApplicationPage
    {
        // http://blogs.microsoft.nl/blogs/ux/archive/2011/05/02/building-a-custom-video-player-with-the-player-framework-for-the-web-desktop-and-the-phone.aspx

        public PlayerPage()
        {
            InitializeComponent();

            adControl1.Visibility = System.Windows.Visibility.Visible;
            adControl1.AdRefreshed -= adControl1_AdRefreshed;
            adControl1.AdRefreshed += adControl1_AdRefreshed;
            adControl1.ErrorOccurred -= adControl1_ErrorOccurred;
            adControl1.ErrorOccurred += adControl1_ErrorOccurred;
            adControl1.AdUnitId = App.ViewModel.AdUnitId;
            adControl1.ApplicationId = App.ViewModel.ApplicationId;
            adControl1.IsHitTestVisible = false;
            adControl1.Width = 80;
            adControl1.Height = 480;

            SMFPlayerControl.IsControlStripVisible = false;
            SMFPlayerControl.VolumeLevel = 0.8;
            SMFPlayerControl.DataReceived += Player_DataReceived;
            SMFPlayerControl.MediaEnded += Player_MediaEnded;
            SMFPlayerControl.MediaOpened += Player_MediaOpened;
            SMFPlayerControl.MediaFailed += Player_MediaFailed;

            MediaElementPlayer.AutoPlay = true;
            MediaElementPlayer.MediaEnded += MediaElementPlayer_MediaEnded;
            MediaElementPlayer.MediaOpened += MediaElementPlayer_MediaOpened;
            MediaElementPlayer.MediaFailed += MediaElementPlayer_MediaFailed;
        }

        void MediaElementPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
        }

        void MediaElementPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
        }

        void MediaElementPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
        }

        void Player_MediaFailed(object sender, Microsoft.SilverlightMediaFramework.Core.CustomEventArgs<Exception> e)
        {
        }

        void Player_MediaOpened(object sender, EventArgs e)
        {
        }

        void Player_MediaEnded(object sender, EventArgs e)
        {
        }

        void Player_DataReceived(object sender, DataReceivedInfo e)
        {
        }

        void adControl1_ErrorOccurred(object sender, Microsoft.Advertising.AdErrorEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                adControl1.Width = 0;
            });
        }

        void adControl1_AdRefreshed(object sender, EventArgs e)
        {
        }

        //void Player_VolumeLevelChanged(object sender, Microsoft.SilverlightMediaFramework.Core.CustomEventArgs<double> e)
        //{
        //}

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            App.ViewModel.IsDataLoading = false;

            if (SMFPlayerControl.Visibility == Visibility.Visible)
            {
                //SMF PLAYER
                SMFPlayerControl.Playlist.Clear();
                //if (GenericHelper.AppIsOfflineSettingValue)
                //{
                //    SMFPlayerControl.Playlist.Add(new PlaylistItem());
                //    using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                //    {
                //        using (var stream = IsolatedStorageFile.GetUserStoreForApplication().OpenFile(App.ViewModel.CurrentYoutubeMP4FileName, System.IO.FileMode.Open))
                //        {
                //            SMFPlayerControl.Playlist.ElementAt(0).StreamSource = stream;
                //            SMFPlayerControl.Playlist.ElementAt(0).DeliveryMethod = DeliveryMethods.NotSpecified;
                //            SMFPlayerControl.Playlist.ElementAt(0).VideoStretchMode = System.Windows.Media.Stretch.UniformToFill;
                //        }
                //    }
                //}
                //else
                //{
                SMFPlayerControl.Playlist.Add(new PlaylistItem() { MediaSource = App.ViewModel.CurrentYoutubeMP4Uri });
                //}
                SMFPlayerControl.Play();

                
            }
            else
            {
                //MEDIAPLAYER
                //per customizzarlo: http://msdn.microsoft.com/en-us/library/ms748248(v=vs.110).aspx
                if (GenericHelper.AppIsOfflineSettingValue)
                {
                    using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        using (var stream = IsolatedStorageFile.GetUserStoreForApplication().OpenFile(App.ViewModel.CurrentYoutubeMP4FileName, System.IO.FileMode.Open))
                        {
                            MediaElementPlayer.SetSource(stream);
                        }
                    }
                }
                else
                {
                    MediaElementPlayer.Source = App.ViewModel.CurrentYoutubeMP4Uri;
                }
            }
        }

    }
}