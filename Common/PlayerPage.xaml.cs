﻿using System;
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
using System.Windows.Media;
using Microsoft.Phone.Tasks;

namespace Centapp.CartoonCommon
{
    public partial class PlayerPage : PhoneApplicationPage
    {
        // http://blogs.microsoft.nl/blogs/ux/archive/2011/05/02/building-a-custom-video-player-with-the-player-framework-for-the-web-desktop-and-the-phone.aspx

        public PlayerPage()
        {
            InitializeComponent();

            myAdv.Visibility = System.Windows.Visibility.Collapsed;
            switch (App.ViewModel.AdvProvider)
            {
                case AdvProvider.PubCenter:
                    //MS PubCenter
                    adControlSoma.Visibility = System.Windows.Visibility.Collapsed;
                    adControlPubCenter.Visibility = System.Windows.Visibility.Visible;
                    adControlPubCenter.AdRefreshed -= adControl1_AdRefreshed;
                    adControlPubCenter.AdRefreshed += adControl1_AdRefreshed;
                    adControlPubCenter.ErrorOccurred -= adControl1_ErrorOccurred;
                    adControlPubCenter.ErrorOccurred += adControl1_ErrorOccurred;
                    adControlPubCenter.AdUnitId = App.ViewModel.AdUnitId;
                    adControlPubCenter.ApplicationId = App.ViewModel.ApplicationId;
                    adControlPubCenter.IsHitTestVisible = false;
                    adControlPubCenter.Width = 80;
                    adControlPubCenter.Height = 480;
                    break;
                case AdvProvider.Sooma:
                    //SOOMA
                    adControlPubCenter.Visibility = System.Windows.Visibility.Collapsed;
                    adControlSoma.IsHitTestVisible = false;
                    adControlSoma.PopupAd = true;
                    //adControlSoma.PopupAdDuration = 300;
                    adControlSoma.AdSpaceHeight = 50;
                    adControlSoma.AdSpaceWidth = 320;
                    adControlSoma.Visibility = System.Windows.Visibility.Visible;
                    adControlSoma.Pub = int.Parse(App.ViewModel.AdPublisherId); 
                    adControlSoma.Adspace = int.Parse(App.ViewModel.AdSpaceId);  
                    adControlSoma.Age = 12;
                    adControlSoma.ShowErrors = false;
                    adControlSoma.LocationUseOK = true;
                    adControlSoma.StartAds();
                    adControlSoma.NewAdAvailable -= adControlSoma_NewAdAvailable;
                    adControlSoma.NewAdAvailable += adControlSoma_NewAdAvailable;
                    break;
                default:
                    adControlPubCenter.Visibility = System.Windows.Visibility.Collapsed;
                    adControlSoma.Visibility = System.Windows.Visibility.Collapsed;
                    myAdv.Visibility = System.Windows.Visibility.Visible;
                    break;
            }

            Loaded += PlayerPage_Loaded;
            Unloaded += PlayerPage_Unloaded;

            SMFPlayerControl.IsControlStripVisible = false;
            SMFPlayerControl.VolumeLevel = 0.8;
            SMFPlayerControl.DataReceived += Player_DataReceived;
            SMFPlayerControl.MediaEnded += Player_MediaEnded;
            SMFPlayerControl.MediaOpened += Player_MediaOpened;
            SMFPlayerControl.MediaFailed += Player_MediaFailed;

            //MediaElementPlayer.AutoPlay = true;
            //MediaElementPlayer.MediaEnded += MediaElementPlayer_MediaEnded;
            //MediaElementPlayer.MediaOpened += MediaElementPlayer_MediaOpened;
            //MediaElementPlayer.MediaFailed += MediaElementPlayer_MediaFailed;
        }

        void adControlSoma_NewAdAvailable(object sender, EventArgs e)
        {
        }

        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            switch (App.ViewModel.AdvProvider)
            {
                case AdvProvider.Sooma:
                    adControlSoma.StopAds();
                    break;
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            if (e.NavigationMode == NavigationMode.Back)
            {
                
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.NavigationMode == NavigationMode.Back) {
                StartPlaylist();
            }
        }


        void PlayerPage_Loaded(object sender, RoutedEventArgs e)
        {
            App.ViewModel.IsDataLoading = false;

            if (SMFPlayerControl.Visibility == Visibility.Visible)
            {
                //SMF PLAYER
                StartPlaylist();
                //var videoArea = SMFPlayerControl.VideoArea;
                //var VideoHeight = SMFPlayerControl.VideoHeight;
                //var VideoWidth= SMFPlayerControl.VideoWidth;
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

        private void StartPlaylist()
        {
            SMFPlayerControl.Playlist.Clear();
            SMFPlayerControl.Playlist.Add(new PlaylistItem() { MediaSource = App.ViewModel.CurrentYoutubeMP4Uri });
            SMFPlayerControl.Play();
        }

        void PlayerPage_Unloaded(object sender, RoutedEventArgs e)
        {
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
            NavigationService.GoBack();
        }

        void Player_DataReceived(object sender, DataReceivedInfo e)
        {
        }

        void adControl1_ErrorOccurred(object sender, Microsoft.Advertising.AdErrorEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                adControlPubCenter.Width = 0;
            });
        }

        void adControl1_AdRefreshed(object sender, EventArgs e)
        {
        }

        private void myAdv_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            MarketplaceDetailTask marketplaceDetailTask = new MarketplaceDetailTask();
            marketplaceDetailTask.ContentIdentifier = "c2e057e9-1b3c-4a13-b722-ad744c5d7ddf"; //the color hunter
            marketplaceDetailTask.ContentType = MarketplaceContentType.Applications;
            marketplaceDetailTask.Show();
        }


        //void Player_VolumeLevelChanged(object sender, Microsoft.SilverlightMediaFramework.Core.CustomEventArgs<double> e)
        //{
        //}

       

    }
}