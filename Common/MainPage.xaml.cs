using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using MyToolkit.Multimedia;
using Centapp.CartoonCommon;
using Microsoft.Phone.Shell;
using System.ComponentModel;
using System.Windows.Navigation;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Tasks;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Resources;
using System.Globalization;
using System.Threading;
using Centapp.CartoonCommon.Converters;
#if NOINTERNET
#else
using Microsoft.Phone.Net.NetworkInformation;
#endif
using System.Windows.Media.Imaging;
using System.Collections;
using System.Reflection;
using Wp7Shared.Exceptions;
using System.Windows.Threading;
using System.Windows.Controls.Primitives;
using Centapp.CartoonCommon.ViewModels;
using Centapp.CartoonCommon.Helpers;
using Centapp.CartoonCommon;
using Microsoft.Phone.Info;
using Wp7Shared.Helpers;
using GestureEventArgs = System.Windows.Input.GestureEventArgs;


//using Wp7Shared.Helpers;


namespace Centapp.CartoonCommon
{

    public enum AppFunctionToLimit
    {
        EpisodeCount,
        BackupEpisodes
    }

    public enum AdvProvider
    {
        Undefined,
        Sooma,
        PubCenter,
        MyAppPromotion
    }


    public delegate void PopupClosedEventHandler();
    public delegate void PopupCancelPressedActionToExecute();

    public partial class MainPage : PhoneApplicationPage
    {
        string _appVer = string.Empty;
        //bool _useSMF = true;

        IsolatedStorageFile _curIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication();

        //string _usagefileName = string.Format("usages_{0}.txt", DateTime.Now.To().Replace("/", "_"));

        #region settings
        IsolatedStorageSettings _settings = IsolatedStorageSettings.ApplicationSettings;
        string _usages_setting = "usages_count";
        string _curDate_setting = "curDate";
        #endregion

        private object _currentContextItem = null;
        private bool _userMessageShown = false;


        // Constructor
        public MainPage()
        {

#if DEBUG
            //MigrateTranslationToWeb();
#endif
            InitializeComponent();

            //var test = Wp7Shared.Helpers.AppInfosHelper.GetId();
            //CultureInfo cc, cuic;
            //cc = Thread.CurrentThread.CurrentCulture;
            //cuic = Thread.CurrentThread.CurrentUICulture;

            // Set the data context of the listbox control to the sample data
            DataContext = App.ViewModel;

            App.ViewModel.OnError -= ViewModel_OnError;
            App.ViewModel.OnError += ViewModel_OnError;

            App.ViewModel.OnUserMessageRequired -= ViewModel_OnUserMessageRequired;
            App.ViewModel.OnUserMessageRequired += ViewModel_OnUserMessageRequired;

            this.Loaded += new RoutedEventHandler(MainPage_Loaded);

            firstList.Tap += new EventHandler<System.Windows.Input.GestureEventArgs>(itemsList_Tap);
            secondList.Tap += new EventHandler<System.Windows.Input.GestureEventArgs>(itemsList_Tap);
            thirdList.Tap += new EventHandler<System.Windows.Input.GestureEventArgs>(itemsList_Tap);
            fourthList.Tap += new EventHandler<System.Windows.Input.GestureEventArgs>(itemsList_Tap);

            favoritesList.Tap += new EventHandler<System.Windows.Input.GestureEventArgs>(itemsList_Tap);

            _appVer = GenericHelper.GetAppversion();

            SetCaptions();
            AddContextMenus();

            PanoramaMainControl.SelectionChanged += new EventHandler<SelectionChangedEventArgs>(PanoramaMainControl_SelectionChanged);

            CheckOtherAppsPanoramaItem();
            App.ViewModel.Logger.Reset();

            if (!string.IsNullOrEmpty(App.ViewModel.CustomFirstPivotItemName))
            {
                TextBlockFirstPivotItem.Text = App.ViewModel.CustomFirstPivotItemName;
            }

#if DEBUG
            //MessageBox.Show("versione BETA : " + GenericHelper.GetAppversion());
#endif

            //_dt.Interval = TimeSpan.FromSeconds(TimerInterval);
            //_dt.Tick += new EventHandler(dt_Tick);

            //NotificationBox.ShowAgain(
            //    "Show Again",
            //    "Uncheck the show again message and this message won't appear again.",
            //    "Show this message again",
            //    false,
            //    suppressed => { },
            //    GetType().ToString(),
            //    new NotificationBoxCommand("Xxx", () => { }),
            //    new NotificationBoxCommand("Xxx", () => { }),
            //    new NotificationBoxCommand("Zzz", () => { }));
        }

        void ViewModel_OnError(string msg, bool isFatalError)
        {
            Dispatcher.BeginInvoke(() =>
            {
                MessageBox.Show(msg);
                if (isFatalError)
                {
                    throw new ForcedExitException("service error: " + msg);
                }
            });
        }

        public void ShareApp()
        {
            ShareLinkTask shareLinkTask = new ShareLinkTask();
            shareLinkTask.Title = "Check this app!";
            shareLinkTask.LinkUri = new Uri(string.Format("http://www.windowsphone.com/s?appid={0}", Wp7Shared.Helpers.AppInfosHelper.GetId()), UriKind.Absolute);
            shareLinkTask.Show();
        }

        private void MigrateTranslationToWeb()
        {
            List<CultureInfo> supportedUiCultures = new List<CultureInfo>() { 
                new CultureInfo("es-ES"), 
                new CultureInfo("it-IT"), 
                new CultureInfo("en-US"), 
                new CultureInfo("pt-BR") 
            };

            try
            {
                MessageBox.Show("MigrateTranslationToWeb ATTIVO");


                XDocument xDoc = new XDocument();
                Assembly asm = Assembly.GetExecutingAssembly();
                Stream stream = asm.GetManifestResourceStream("Centapp.Cartoon.xml.pingu.xml");
                xDoc = XDocument.Load(stream);

                //<item id="4">
                //  <url>http://www.youtube.com/watch?v=ee2r7PAgNCA</url>
                //  <desc>
                //    <descItem lang="en-US" value="Zipping along"/>
                //  </desc>
                //</item>  

                var items = xDoc.Element("root").Descendants("item");
                foreach (var item in items)
                {

                    int idValue = int.Parse(item.Attribute("id").Value);
                    string dictKey = string.Format("ep_{0}", idValue < 10 ? idValue.ToString().PadLeft(2, '0') : idValue.ToString());

                    if (item.Element("desc") == null)
                    {
                        XElement tmp = new XElement("desc");
                        item.Add(tmp);
                    }

                    XElement descEl = item.Element("desc");

                    foreach (var cultureInfo in supportedUiCultures)
                    {
                        ResourceSet resourceSet = AppResources.ResourceManager.GetResourceSet(cultureInfo, true, true);

                        string translatedValue = string.Empty;
                        foreach (DictionaryEntry entry in resourceSet)
                        {
                            string resourceKey = entry.Key.ToString();
                            if (resourceKey.Equals(dictKey))
                            {
                                translatedValue = entry.Value.ToString().Trim();
                                break;
                            }
                        }
                        string content = string.Format("<descItem lang=\"{0}\" value=\"{1}\"/>", cultureInfo, translatedValue);
                        XElement translatedEl = XDocument.Parse(content).Root;
                        descEl.Add(translatedEl);
                    }

                }
                string str = xDoc.ToString();

                //var c1 = new CultureInfo("it-IT");
                //var c2 = new CultureInfo("en-US");
                //var c3 = new CultureInfo("es-ES");
                //var c4 = new CultureInfo("pt-BR");
                //var c5 = new CultureInfo("fi-FI");

                //MyResourceManager rm = new MyResourceManager(xDoc, c1);
                //string test1 = rm.GetString("ep_13", c1);
                //string test2 = rm.GetString("ep_13", c2);
                //string test3 = rm.GetString("ep_13", c3);
                //string test4 = rm.GetString("ep_13", c4);
                //string test5 = rm.GetString("ep_13", c5);
            }
            catch (Exception)
            {

            }
        }

        void ViewModel_OnUserMessageRequired(string msg, bool mustExitApp)
        {
            Dispatcher.BeginInvoke(() =>
            {
                if (!_userMessageShown && !mustExitApp)
                {
                    MessageBox.Show(msg);
                    _userMessageShown = true;
                }
                if (mustExitApp)
                {
                    throw new ForcedExitException("service error: " + msg);
                }
            });
        }


        void PanoramaMainControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (otherAppsPanoramaItem.Visibility == Visibility.Collapsed)
            {
                return;
            }

            try
            {
                if (PanoramaMainControl.SelectedItem == otherAppsPanoramaItem)
                {
                    otherApps.UnFreezeAll();
                }
                else
                {
                    otherApps.FreezeAll();
                }
            }
            catch (Exception)
            {
            }
        }

        private void SetCaptions()
        {
            //noFavoritestextBlock.Text = "No favorites episodes set.\n\nTap and hold an episode to add it here.\n\nTap and hold an episode in this list (if present) to remove it from favorites";
            noFavoritestextBlock.Text = AppResources.favText;

            favoritesHeader.Text = AppResources.favoritesHeader;
            appHeader.Text = AppResources.appHeader;

            //buyAppButtonText.Text = AppResources.buyAppButton;
            rateAppButtonText.Text = AppResources.rateAppButton;
            writeAnEmailButtonText.Text = AppResources.writeAnEmailButton;
            //otherAppsButtonText.Text = AppResources.otherAppsButton;
            infoButtonText.Text = AppResources.infoButtonText;
            backupEpisodesText.Text = AppResources.backupEpisodesText;
            otherAppsPanoramaHeader.Text = AppResources.otherAppsButton;

            firstListTxtNoInternet.Text = AppResources.noNetworkAvailable;
            secondListTxtNoInternet.Text = AppResources.noNetworkAvailable;
            thirdListTxtNoInternet.Text = AppResources.noNetworkAvailable;
            fourthListTxtNoInternet.Text = AppResources.noNetworkAvailable;
            favoritesListTxtNoInternet.Text = AppResources.noNetworkAvailable;

        }

        // Load data for the ViewModel Items
        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateAppInfos();
            backupEpisodesButton.Visibility = App.ViewModel.DownloadIsAllowed ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateAppInfos()
        {
            //string trialOrReg = ((App)Application.Current).IsTrial? "TRIAL" : "FULL";
            string trialOrReg = ((App)Application.Current).IsTrial ? AppResources.trial : AppResources.full;
            appInfo.Text = string.Format("v.{0} - {1}", new string[] { _appVer, trialOrReg });
            appInfo.Foreground = ((App)Application.Current).IsTrial ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.White);

            if (GenericHelper.AppIsOfflineSettingValue)
            {
                appInfo.Text += " (offline)";
            }
        }


        void itemsList_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if ((sender as ListBox).SelectedItem == null)
            {
                return;
            }

            var selectedItem = (ItemViewModel)((sender as ListBox).SelectedItem);

            //per vedere con webbrowser
            //WebBrowserTask webBrowserTask = new WebBrowserTask();
            //webBrowserTask.URL = "vnd.youtube:" + GenericHelper.GetYoutubeID(selectedItem.Url) + "?vndapp=youtube_mobile";
            //webBrowserTask.Show();

            try
            {

                //GenericHelper.AppIsOfflineSettingValue = false;

                if (GenericHelper.AppIsOfflineSettingValue)
                {
                    #region OFFLINE
                    try
                    {
                        if (App.ViewModel.IsAdvertisingEnabled)
                        {
                            #region advertising ON

                            //KO
                            //C:\Data\Users\DefApps\AppData\{2D034F2D-836B-466C-9CA2-A7BB6B24E3F8}\Local\ep_1.mp4
                            //App.ViewModel.CurrentYoutubeMP4FileName = selectedItem.OfflineFileName;
                            //App.ViewModel.CurrentYoutubeMP4Uri = new Uri("ms-appdata:///local/" + selectedItem.OfflineFileName, UriKind.RelativeOrAbsolute);
                            //App.ViewModel.CurrentYoutubeMP4Uri = new Uri("isostore:/" + selectedItem.OfflineFileName, UriKind.RelativeOrAbsolute);
                            //App.ViewModel.CurrentYoutubeMP4Uri = new Uri(selectedItem.OfflineFileName, UriKind.RelativeOrAbsolute);
                            //App.ViewModel.CurrentYoutubeMP4Uri = new Uri("isostore:/Local/" + selectedItem.OfflineFileName, UriKind.RelativeOrAbsolute);
                            //App.ViewModel.CurrentYoutubeMP4Uri = new Uri("isostore:/local/" + selectedItem.OfflineFileName, UriKind.Absolute);
                            App.ViewModel.IsDataLoading = true;
                            //OK
                            //Uri test1 = new Uri(@"C:\Data\Users\DefApps\AppData\{2D034F2D-836B-466C-9CA2-A7BB6B24E3F8}\Local\ep_1.mp4", UriKind.Absolute);
                            Uri episodeUri = new Uri(@"C:\Data\Users\DefApps\AppData\{" + Wp7Shared.Helpers.AppInfosHelper.GetId() + @"}\Local\" + selectedItem.OfflineFileName, 
                                                    UriKind.Absolute);
                            App.ViewModel.CurrentYoutubeMP4Uri = episodeUri;
                            Wp7Shared.Helpers.NavigationHelper.SafeNavigateTo(NavigationService, Dispatcher, "/PlayerPage.xaml");
                            #endregion
                        }
                        else
                        {
                            #region advertising OFF
                            var episodeUri = new Uri(selectedItem.OfflineFileName, UriKind.RelativeOrAbsolute);
                            var launcher = new MediaPlayerLauncher
                                      {
                                          Controls = MediaPlaybackControls.All,
                                          Location = MediaLocationType.Data,
                                          Media = episodeUri
                                      };
                            launcher.Show();
                            #endregion
                        }
                    }
                    catch (Exception)
                    {
                        MessageBox.Show(AppResources.possibleWrongBackup);
                    }
                    #endregion
                }
                else
                {
                    #region ONLINE

                    if (!NetworkInterface.GetIsNetworkAvailable())
                    {
                        MessageBox.Show(AppResources.noNetworkAvailable);
                        return;
                    }

                    if (((App)Application.Current).IsTrial)
                    {
                        if (!selectedItem.IsAvailableInTrial)
                        {
                            GotoBuyPage(AppFunctionToLimit.EpisodeCount);
                            return;
                        }
                    }

                    if (App.ViewModel.DownloadIsAllowed)
                    {
                        GenericHelper.IncrementOnlineUsagesCount();
                        if (GenericHelper.OnlineUsagesSettingValue % 4 == 0)
                        {
                            switch (MessageBox.Show(AppResources.DownloadEpisodesQuestion, "", MessageBoxButton.OKCancel))
                            {
                                case MessageBoxResult.OK:
                                    GotoDownloaderPage();
                                    return;
                            }
                        }
                    }

                    string id = GenericHelper.GetYoutubeID(selectedItem.Url);
                    App.ViewModel.IsDataLoading = true;

                    if (App.ViewModel.IsAdvertisingEnabled)
                    {
                        #region advertising ON
                        YouTube.GetVideoUri(id,
                                            MyToolkit.Multimedia.YouTubeQuality.Quality480P,
                                            (uri, ex) =>
                                            {
                                               
                                                //SystemTray.ProgressIndicator.IsVisible = true;
                                                if (ex == null && uri != null)
                                                {
                                                    App.ViewModel.CurrentYoutubeMP4Uri = uri.Uri;
                                                    Wp7Shared.Helpers.NavigationHelper.SafeNavigateTo(NavigationService, Dispatcher, "/PlayerPage.xaml");
                                                }
                                                else
                                                {
                                                    switch (MessageBox.Show(AppResources.brokenLinkQuestion,
                                                                            AppResources.ExceptionMessageTitle,
                                                                            MessageBoxButton.OKCancel))
                                                    {

                                                        case MessageBoxResult.OK:
                                                            ReportBrokenLink(ex.Message);
                                                            break;
                                                    }
                                                }
                                            });
                        #endregion
                    }
                    else
                    {
                        #region advertising OFF
                        YouTube.Play(id, true, YouTubeQuality.Quality480P, x =>
                        {
                            if (x != null)
                            {
                                switch (MessageBox.Show(AppResources.brokenLinkQuestion,
                                                        AppResources.ExceptionMessageTitle,
                                                        MessageBoxButton.OKCancel))
                                {

                                    case MessageBoxResult.OK:
                                        ReportBrokenLink(x.Message);
                                        break;
                                }
                            }
                            App.ViewModel.IsDataLoading = false;
                        });
                        #endregion
                    }
                    #endregion
                }
            }
            catch (InvalidOperationException ignored)
            {

            }
        }

        void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
        }

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            if (YouTube.CancelPlay())
            {
                //StopUsageTimer();
                e.Cancel = true;
            }
            else
            {
                // your code here
            }
            base.OnBackKeyPress(e);
        }


        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ToggleNoInternetWarningTxt(false);

            if (!NetworkInterface.GetIsNetworkAvailable() && !GenericHelper.AppIsOfflineSettingValue)
            {
                ToggleNoInternetWarningTxt(true);
                return;
            }

            try
            {
                YouTube.CancelPlay(); // used to reenable page
            }
            catch (Exception)
            {
            }
            // your code here
            // StopUsageTimer();
            base.OnNavigatedTo(e);
        }

        private void CheckOtherAppsPanoramaItem()
        {
            //string currentCulture = Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName;

            if (!App.ViewModel.ShowOtherApps)
            {
                otherAppsPanoramaItem.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                otherAppsPanoramaItem.Visibility = System.Windows.Visibility.Visible;
                otherApps.SetCurrentAppGuid(Wp7Shared.Helpers.AppInfosHelper.GetId());
                otherApps.SetRequiredGenre(Wp7Shared.UserControls.Genre.KidsAndFamily);
            }

            //if (currentCulture.Equals("it") || currentCulture.Equals("en"))
            //{
            //    otherAppsPanoramaItem.Visibility = System.Windows.Visibility.Visible;
            //    otherApps.SetCurrentAppGuid(Wp7Shared.Helpers.AppInfosHelper.GetId());
            //    otherApps.SetRequiredGenre(Wp7Shared.UserControls.Genre.KidsAndFamily);
            //}
            //else
            //{
            //    otherAppsPanoramaItem.Visibility = System.Windows.Visibility.Collapsed;
            //}
        }

        private void ToggleNoInternetWarningTxt(bool visible)
        {
            if (visible)
            {
                firstListTxtNoInternet.Visibility = System.Windows.Visibility.Visible;
                secondListTxtNoInternet.Visibility = System.Windows.Visibility.Visible;
                thirdListTxtNoInternet.Visibility = System.Windows.Visibility.Visible;
                fourthListTxtNoInternet.Visibility = System.Windows.Visibility.Visible;
                favoritesListTxtNoInternet.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                firstListTxtNoInternet.Visibility = System.Windows.Visibility.Collapsed;
                secondListTxtNoInternet.Visibility = System.Windows.Visibility.Collapsed;
                thirdListTxtNoInternet.Visibility = System.Windows.Visibility.Collapsed;
                fourthListTxtNoInternet.Visibility = System.Windows.Visibility.Collapsed;
                favoritesListTxtNoInternet.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            _currentContextItem = ((Border)sender).DataContext;
        }

        #region favorites
        private void AddContextMenus()
        {
            //lista 1
            ContextMenu contextMenu1 = new ContextMenu();

            string addToFavText = AppResources.addToFavText;
            string removeFromFavText = AppResources.removeFromFavText;
            string shareText = AppResources.shareText;
            string reportErrorText = AppResources.reportError;

            //lista1
            MenuItem menuItemAdd1 = new MenuItem { Header = addToFavText };
            contextMenu1.Items.Add(menuItemAdd1);
            menuItemAdd1.Click += new RoutedEventHandler(menuItemAdd_Click);

            if (!GenericHelper.AppIsOfflineSettingValue)
            {
                MenuItem menuItemShare1 = new MenuItem { Header = shareText };
                contextMenu1.Items.Add(menuItemShare1);
                menuItemShare1.Click += new RoutedEventHandler(menuItemShare_Click);
            }

            MenuItem menuItemBrokenlink1 = new MenuItem { Header = reportErrorText };
            contextMenu1.Items.Add(menuItemBrokenlink1);
            menuItemBrokenlink1.Click += new RoutedEventHandler(menuItemBrokenlink_Click);

            ContextMenuService.SetContextMenu(firstList, contextMenu1);

            //lista 2
            ContextMenu contextMenu2 = new ContextMenu();

            MenuItem menuItemAdd2 = new MenuItem { Header = addToFavText };
            contextMenu2.Items.Add(menuItemAdd2);
            menuItemAdd2.Click += new RoutedEventHandler(menuItemAdd_Click);

            MenuItem menuItemShare2 = new MenuItem { Header = shareText };
            contextMenu2.Items.Add(menuItemShare2);
            menuItemShare2.Click += new RoutedEventHandler(menuItemShare_Click);

            MenuItem menuItemBrokenlink2 = new MenuItem { Header = reportErrorText };
            contextMenu2.Items.Add(menuItemBrokenlink2);
            menuItemBrokenlink2.Click += new RoutedEventHandler(menuItemBrokenlink_Click);

            ContextMenuService.SetContextMenu(secondList, contextMenu2);

            //lista 3
            ContextMenu contextMenu3 = new ContextMenu();
            MenuItem menuItemAdd3 = new MenuItem { Header = addToFavText };

            contextMenu3.Items.Add(menuItemAdd3);
            menuItemAdd3.Click += new RoutedEventHandler(menuItemAdd_Click);
            ContextMenuService.SetContextMenu(thirdList, contextMenu3);

            MenuItem menuItemShare3 = new MenuItem { Header = shareText };
            contextMenu3.Items.Add(menuItemShare3);
            menuItemShare3.Click += new RoutedEventHandler(menuItemShare_Click);

            MenuItem menuItemBrokenlink3 = new MenuItem { Header = reportErrorText };
            contextMenu3.Items.Add(menuItemBrokenlink3);
            menuItemBrokenlink3.Click += new RoutedEventHandler(menuItemBrokenlink_Click);

            //lista 4
            ContextMenu contextMenu4 = new ContextMenu();
            MenuItem menuItemAdd4 = new MenuItem { Header = addToFavText };

            contextMenu4.Items.Add(menuItemAdd4);
            menuItemAdd4.Click += new RoutedEventHandler(menuItemAdd_Click);
            ContextMenuService.SetContextMenu(fourthList, contextMenu4);

            MenuItem menuItemShare4 = new MenuItem { Header = shareText };
            contextMenu4.Items.Add(menuItemShare4);
            menuItemShare4.Click += new RoutedEventHandler(menuItemShare_Click);

            MenuItem menuItemBrokenlink4 = new MenuItem { Header = reportErrorText };
            contextMenu4.Items.Add(menuItemBrokenlink4);
            menuItemBrokenlink4.Click += new RoutedEventHandler(menuItemBrokenlink_Click);


            //lista preferite
            ContextMenu contextMenuFavorites = new ContextMenu();

            MenuItem menuItemRemove = new MenuItem { Header = removeFromFavText };
            contextMenuFavorites.Items.Add(menuItemRemove);
            menuItemRemove.Click += new RoutedEventHandler(menuItemRemove_Click);

            MenuItem menuItemShareFav = new MenuItem { Header = shareText };
            contextMenuFavorites.Items.Add(menuItemShareFav);
            menuItemShareFav.Click += new RoutedEventHandler(menuItemShare_Click);

            ContextMenuService.SetContextMenu(favoritesList, contextMenuFavorites);

        }

        void menuItemBrokenlink_Click(object sender, RoutedEventArgs e)
        {
            ReportBrokenLink("");
        }

        private void ReportBrokenLink(string internalError)
        {
            try
            {
                var email = new EmailComposeTask();
                email.To = "centapp@hotmail.com";
                email.Subject = string.Format("{0} - {1} ({2})", AppResources.reportError, App.ViewModel.AppName.ToUpper(), GenericHelper.GetAppversion());
                var episode = (_currentContextItem as ItemViewModel);
                email.Body = string.Format(AppResources.brokenLinkText,
                                          new IdToTitleConverter().Convert(episode.Id, null, null, App.ViewModel.NeutralCulture),
                                          (_currentContextItem as ItemViewModel).Url);

                if (!string.IsNullOrEmpty(episode.OrigId))
                {
                    email.Body += string.Format("\norig id = '{0}'", episode.OrigId);
                }

                if (!string.IsNullOrEmpty(internalError))
                {
                    email.Body += string.Format("\nError code = '{0}'", internalError);
                }

                email.Body += string.Format("\nApp version = '{0}'", _appVer);
                email.Body += string.Format("\nApp language = '{0}'", App.ViewModel.NeutralCulture);

                email.Show();
            }
            catch (InvalidOperationException ignored)
            {
            }
        }

        void menuItemShare_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShareStatusTask shareStatusTask = new ShareStatusTask();
                shareStatusTask.Status = String.Format(AppResources.checkoutVideo,
                                                      new IdToTitleConverter().Convert((_currentContextItem as ItemViewModel).Id, null, null, null),
                                                      (_currentContextItem as ItemViewModel).Url);
                shareStatusTask.Show();
            }
            catch (InvalidOperationException ignored)
            {
            }
        }

        void menuItemAdd_Click(object sender, RoutedEventArgs e)
        {
            if (((App)Application.Current).IsTrial && !(_currentContextItem as ItemViewModel).IsAvailableInTrial)
            {
                GotoBuyPage(AppFunctionToLimit.EpisodeCount);
                return;
            }

            if (GenericHelper.AppIsOfflineSettingValue)
            {
                App.ViewModel.AddToFavorites(_currentContextItem);
            }
            else
            {
                MessageBoxResult messageBoxResult = MessageBox.Show(AppResources.FavoritesOnlyInOfflineMode, AppResources.Warning, MessageBoxButton.OKCancel);
                if (messageBoxResult == MessageBoxResult.OK)
                {
                    ExecBackup();
                }
            }

            //CheckEmptyFavoritesList();
        }

        void menuItemRemove_Click(object sender, RoutedEventArgs e)
        {
            App.ViewModel.RemoveFromFavorites(_currentContextItem);
            //    CheckEmptyFavoritesList();
        }
        #endregion


        #region "app" panorama item
        private void rateAppButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            try
            {
                if (!NetworkInterface.GetIsNetworkAvailable())
                {
                    MessageBox.Show(AppResources.noNetworkAvailable);
                    return;
                }

                FeedbackHelper.Default.Reviewed();

                MarketplaceReviewTask task = new MarketplaceReviewTask();
                task.Show();


            }
            catch (InvalidOperationException ignored)
            {

            }
            catch (Exception)
            {
                MessageBox.Show(AppResources.error_store);
            }
        }

        private void writeAnEmailButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            try
            {
                if (!NetworkInterface.GetIsNetworkAvailable())
                {
                    MessageBox.Show(AppResources.noNetworkAvailable);
                    return;
                }

                var email = new EmailComposeTask();
                email.To = "centapp@hotmail.com";
                email.Subject = string.Format(AppResources.feedbackText, App.ViewModel.AppName.ToUpper()) + string.Format(" ({0})", GenericHelper.GetAppversion());

                
                email.Show();
            }
            catch (InvalidOperationException ignored)
            {

            }
        }

        private void buyAppButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            try
            {
                var marketplaceDetailTask = new MarketplaceDetailTask();
                marketplaceDetailTask.ContentIdentifier = null;
                marketplaceDetailTask.Show();
            }
            catch (Exception)
            {
                MessageBox.Show(AppResources.error_buy);
            }
        }
        #endregion

        #region license management

        private void GotoBuyPage(AppFunctionToLimit usage)
        {
            Wp7Shared.Helpers.NavigationHelper.SafeNavigateTo(NavigationService,
                                                              Dispatcher,
                                                              string.Format("/BuyAppPage.xaml?" + GenericHelper.UsageKeyName + "={0}", usage.ToString()));
        }
        #endregion

        private void searchEpisodesButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Wp7Shared.Helpers.NavigationHelper.SafeNavigateTo(NavigationService,
                                                              Dispatcher,
                                                              string.Format("/SearchEpisodes.xaml"));
        }


        #region offline backup

        private void backupEpisodesButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ExecBackup();

        }

        private void ExecBackup()
        {
            decimal availGb = 0;
            decimal requiredGb = 0;
            if (!CheckAvailableSpace(out availGb, out requiredGb))
            {
                MessageBox.Show(string.Format(AppResources.notEnoughSpace, requiredGb));
                return;
            }

            MessageBoxResult messageBoxResult = MessageBox.Show(string.Format(AppResources.SpaceRequiredWarn, requiredGb), AppResources.Warning, MessageBoxButton.OKCancel);
            if (messageBoxResult != MessageBoxResult.OK)
            {
                return;
            }

            if (!NetworkInterface.GetIsNetworkAvailable() || NetworkInterface.NetworkInterfaceType == NetworkInterfaceType.None)
            {
                MessageBox.Show(AppResources.noNetworkAvailable);
                return;
            }

#if !SIMULATE_DWN
            if (!CheckConnection())
            {
                MessageBox.Show(AppResources.wifiRequired);
                return;
            }
#endif
            if (((App)Application.Current).IsTrial)
            {
                GotoBuyPage(AppFunctionToLimit.BackupEpisodes);
                return;
            }

            if (GenericHelper.AppIsOfflineSettingValue)
            {
                messageBoxResult = MessageBox.Show(AppResources.MessageAlreadyOffline, AppResources.Warning, MessageBoxButton.OKCancel);
                if (messageBoxResult != MessageBoxResult.OK)
                {
                    return;
                }
                GenericHelper.SetAppIsOffline(false);
                using (IsolatedStorageFile isostore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    var storedFiles = isostore.GetFileNames("ep*.mp4");
                    foreach (var item in storedFiles)
                    {
                        isostore.DeleteFile(item);
                    }
                }

                App.ViewModel.OnLoadCompleted -= new OnLoadCompletedHandler(ViewModel_OnLoadCompleted);
                App.ViewModel.OnLoadCompleted += new OnLoadCompletedHandler(ViewModel_OnLoadCompleted);
                App.ViewModel.LoadData();
            }
            else
            {
                if (App.ViewModel.Items == null || App.ViewModel.Items.Count == 0)
                {
                    //caso raro, si fa ripartire l'utente da zero
                    MessageBox.Show(AppResources.ServerTemporaryUnavailable);
                    throw new ForcedExitException("start backup error : " + AppResources.ServerTemporaryUnavailable);
                }
                GotoDownloaderPage();
            }
        }

        private bool CheckConnection()
        {
            //bool hasNetworkConnection = NetworkInterface.NetworkInterfaceType != NetworkInterfaceType.;

            //None - The phone is not connected to any data network 
            //Wireless80211 - The phone is connected to a WiFi hotspot 
            //MobileBroadbandGsm - The phone is connected to a GSM network 
            //MobileBroadbandCdma - The phone is connected to a CDMA network (US and parts of Asia only) 
            //Ethernet - The phone has an ethernet connection (typically this is when the phone is connected to a PC via USB) 

#if DEBUG
            MessageBox.Show("connessione di rete attuale: " + NetworkInterface.NetworkInterfaceType);
            return true;
#endif

            bool isConnectionOk = (NetworkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || NetworkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet);
            if (isConnectionOk)
            {
                return true;
            }
            return false;
        }

        private bool CheckAvailableSpace(out decimal availableGigaBytes, out decimal requiredGigaBytes)
        {
            availableGigaBytes = 0;
            requiredGigaBytes = 0;

            int episodeDurationInSec = App.ViewModel.EpisodesLength;
            if (episodeDurationInSec == -1)
            {
                return true; //dato non dichiarato nell'app: non è possibile eseguire il calcolo e torna true....
            }

            Int64 curAvail = -1;
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                curAvail = store.AvailableFreeSpace;
            }

            //availableGigaBytes = (double)(curAvail / 1024 / 1024 / 1024);
            availableGigaBytes = Math.Round((decimal)((double)curAvail / 1024d / 1024d / 1024d), 1);

            //int episodeDurationInSec = 300;

            //MP4 medium quality: 2Mb/minuto -> 1 MB = 1048576 Bytes -> 2097152 bytes/minuto -> 34952 bytes/secondo
            int bytesPerSecond = 34952;

            Int64 estimatedRequiredSpace = App.ViewModel.Items.Count * episodeDurationInSec * bytesPerSecond;

            //decimal test1 = Math.Round((decimal)((float)curAvail / 1024 / 1024 / 1024), 1);
            //decimal test2 = Math.Round((decimal)((double)curAvail / 1024d / 1024d / 1024d), 1);

            //requiredGigaBytes = (float)(estimatedRequiredSpace / 1024d / 1024 / 1024);
            requiredGigaBytes = Math.Round((decimal)((double)estimatedRequiredSpace / 1024d / 1024d / 1024d), 1);

            if (estimatedRequiredSpace >= curAvail)
            {
                return false;
            }

            return true;
        }

        void ViewModel_OnLoadCompleted()
        {
            App.ViewModel.OnLoadCompleted -= new OnLoadCompletedHandler(ViewModel_OnLoadCompleted);
            GotoDownloaderPage();
        }

        private void GotoDownloaderPage()
        {
            Wp7Shared.Helpers.NavigationHelper.SafeNavigateTo(NavigationService,
                                                              Dispatcher,
                                                              string.Format("/DownloaderPage.xaml"));
        }


        #endregion

        private void otherAppsButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Wp7Shared.Helpers.NavigationHelper.SafeNavigateTo(NavigationService,
                                                  Dispatcher,
                                                  string.Format("/OtherAppsPage.xaml"));

        }

        private void infoButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {

            var infoPage = App.ViewModel.InfoPageIsPivot ? "/InfoPagePivot.xaml" : "/InfoPage.xaml";
            Wp7Shared.Helpers.NavigationHelper.SafeNavigateTo(NavigationService,
                                                Dispatcher,
                                                string.Format(infoPage));
        }

        private void ImageBanner_OnTap(object sender, GestureEventArgs e)
        {
            //Show an application, using the default ContentType.
            MarketplaceDetailTask marketplaceDetailTask = new MarketplaceDetailTask();
            marketplaceDetailTask.ContentIdentifier = "2c9d48fb-2693-418d-87f9-f850eefb64e1";
            marketplaceDetailTask.ContentType = MarketplaceContentType.Applications;
            marketplaceDetailTask.Show();
        }
    }
}