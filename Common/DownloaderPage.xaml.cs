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
using System.IO.IsolatedStorage;
using System.Threading;
using Centapp.CartoonCommon;
using MyToolkit.Multimedia;
using System.Diagnostics;
using Microsoft.Phone.Tasks;
using Microsoft.Phone.Shell;
using System.Globalization;
using Telerik.Windows.Controls;
using System.Text;
using Centapp.CartoonCommon.ViewModels;
using Centapp.CartoonCommon.Helpers;
using System.Windows.Media.Imaging;
using Centapp.CartoonCommon;
using Centapp.CartoonCommon.Converters;
using System.ComponentModel;
using Microsoft.Phone.Net.NetworkInformation;
using Centapp.CartoonCommon.Utility;

namespace Centapp.CartoonCommon
{

    public enum BackupStageEn
    {
        Welcome, //inizio operazioni
        CheckingLinks, //controllo correttezza links
        LinksCheckedWithErrors, //link controllati, rilevati errori
        DwnEpisodesInProgress, //dwn iniziato
        DwnCompletedWithSuccess, //dwn completato ok
        DwnCompletedWithError, //dwn completato con errori
        DwnCompletedWithConnectionError, //dwn completato con errori - problemi di connessione
    }

    public enum BackupOperationEn
    {
        UrlChecks,
        Download,
    }

    public partial class DownloaderPage : PhoneApplicationPage
    {
        bool _incrementalBackup = true;

        private static ManualResetEvent _mre = new ManualResetEvent(false);


        ItemViewModel _curEpisode = null;
        //Stopwatch _sw = new Stopwatch();
        WebClient _client = null;
        RadFadeAnimation _fadeAnimation = null;

        IsolatedStorageFile _isoStore = IsolatedStorageFile.GetUserStoreForApplication();

        public const int MaxAttempts = 3;

        int _episodeCount = 0;
        int _totalEpisodes = 0;
        //int _currentEpisodeDownloadRetryNum = 0;

        private Queue<ItemViewModel> _itemsToDownload = new Queue<ItemViewModel>();
        private Queue<ItemViewModel> _itemsToCheck = new Queue<ItemViewModel>();
        private Queue<ItemViewModel> _currentQueue = new Queue<ItemViewModel>();

        private List<ItemViewModel> _wrongEpisodes = new List<ItemViewModel>();
        private List<string> _retriedEpisodes = new List<string>();
        private List<string> _requeuedEpisodes = new List<string>();

        private List<string> _results = new List<string>();
        BackupOperationEn _backupOperation = BackupOperationEn.UrlChecks;
        private bool _cancellationPending = false;


        public DownloaderPage()
        {
            //TODO tombstoning
            //http://developer.nokia.com/Community/Wiki/Tombstoning_helper_for_Windows_Phone_7

            InitializeComponent();
            DataContext = App.ViewModel;
            App.ViewModel.BackupStage = BackupStageEn.Welcome;
            App.ViewModel.CurrentDispatcher = Dispatcher;
            //Init();
            SetCaptions();
            _fadeAnimation = this.LayoutRoot.Resources["radFadeAnimation"] as RadFadeAnimation;
            App.ViewModel.Logger.Reset();
        }

        private void Init()
        {
            App.ViewModel.Logger.Log("[Init] -----------------------------------");
            _episodeCount = 0;

            _retriedEpisodes.Clear();
            _wrongEpisodes.Clear();
            _requeuedEpisodes.Clear();

            _cancellationPending = false;
            _backupOperation = BackupOperationEn.UrlChecks;

            App.ViewModel.BackupStage = BackupStageEn.CheckingLinks;
            App.ViewModel.DwnInProgress = true;

#if SIMULATE_DWN
            var episodes = App.ViewModel.Items.Take(1).ToList();
#else
            var episodes = App.ViewModel.Items;
#endif

            App.ViewModel.Logger.Log("[Init] episodes initial count: " + episodes.Count);
            //App.ViewModel.Logger.Log("[Init] _incrementalBackup = " + _incrementalBackup);

            List<int> episodesToSkip = new List<int>();
            var storedFiles = _isoStore.GetFileNames("ep*.mp4");
            if (!_incrementalBackup)
            {
                App.ViewModel.Logger.Log("[Init] removing all existing files . . .");
                foreach (var item in storedFiles)
                {
                    _isoStore.DeleteFile(item);
                }
            }
            else
            {
                foreach (var item in storedFiles)
                {
                    int episodeId = int.Parse(item.Replace(".mp4", string.Empty).Replace("ep_", string.Empty).Trim());
                    episodesToSkip.Add(episodeId);
                }
                App.ViewModel.Logger.Log("[Init] already existing episodes count: " + episodesToSkip.Count);
            }

            _itemsToDownload.Clear();
            _itemsToCheck.Clear();

            foreach (var item in episodes)
            {
                item.DwnRetries = 0;
                if (!episodesToSkip.Contains(item.Id))
                {
                    App.ViewModel.Logger.Log("[Init] ADDED = " + item.Id);
                    _itemsToDownload.Enqueue(item);
                    _itemsToCheck.Enqueue(item);
                }
                else
                {
                    App.ViewModel.Logger.Log("[Init] SKIPPED = " + item.Id);
                }
            }

            _currentQueue = _itemsToCheck;

            App.ViewModel.Logger.Log("[Init] _itemsToDownload.Count = " + _itemsToDownload.Count);
            App.ViewModel.Logger.Log("[Init] _itemsToCheck.Count = " + _itemsToCheck.Count);

            if (_currentQueue.Count() == 0)
            {
                App.ViewModel.Logger.Log("[Init] BAD CASE -> _currentQueue.Count() == 0");
                //anomalia poco chiara riscontrata su alcuni utenti...
                //in questo caso si procede nuovamente con il backup totale
                //per evitare situazioni non pulite
                //#update 17/07/2013 - è possibile che in mod. "online" entrasse senza connessione a internet qua, quindi 
                //gli episodi erano sempre a 0! Corretto su CartoonCommon 1.0.1
                _itemsToDownload.Clear();
                _itemsToCheck.Clear();
                foreach (var item in episodes)
                {
                    _itemsToDownload.Enqueue(item);
                    _itemsToCheck.Enqueue(item);
                }
                App.ViewModel.Logger.Log("[Init] AFTER BAD CASE -> _itemsToDownload.Count = " + _itemsToDownload.Count);
                App.ViewModel.Logger.Log("[Init] AFTER BAD CASE -> _itemsToCheck.Count = " + _itemsToCheck.Count);
            }

            _totalEpisodes = _itemsToDownload.Count();

            App.ViewModel.Logger.Log("[Init] _totalEpisodes = " + _totalEpisodes);
            App.ViewModel.Logger.Log("[Init] _currentQueue.Count = " + _currentQueue.Count);

            App.ViewModel.DwnCurEpisode = _currentQueue.First();
        }

        private void SetCaptions()
        {
            ButtonStartBackupEpisodesText.Text = AppResources.ButtonStartBackupEpisodesText;
            TextCurEpisodeTitleHeader.Text = AppResources.TextCurEpisodeTitleHeader;
            TextOverallProgressHeader.Text = AppResources.TextOverallProgressHeader;
        }

        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (App.ViewModel.DwnInProgress)
            {
                MessageBoxResult messageBoxResult = MessageBox.Show(AppResources.MessageAbortDownload, AppResources.Warning, MessageBoxButton.OKCancel);
                if (messageBoxResult != MessageBoxResult.OK)
                {
                    e.Cancel = true;
                }
            }

            App.ViewModel.DwnInProgress = false;
            _cancellationPending = true;
        }

        void _client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if (App.ViewModel.DwnCurEpisodePercentage != e.ProgressPercentage)
            {
                App.ViewModel.DwnCurEpisodePercentage = e.ProgressPercentage;
            }

            //var speed = (Convert.ToDouble(e.BytesReceived) / 1024 / sw.Elapsed.TotalSeconds).ToString("0.00") + " kb/s";
            //if (App.ViewModel.DwnCurEpisodeSpeed != speed)
            //{
            //    App.ViewModel.DwnCurEpisodeSpeed = speed;
            //}

            //double videoTotalSize = (Convert.ToDouble(e.TotalBytesToReceive) / 1024 / 1024);
            //var downloaded = (Convert.ToDouble(e.BytesReceived) / 1024 / 1024).ToString("0.00") + " Mb" + "  /  " + videoTotalSize.ToString("0.00") + " Mb";
            //if (App.ViewModel.DwnCurEpisodeAmount != downloaded)
            //{
            //    App.ViewModel.DwnCurEpisodeAmount = downloaded;
            //}
        }


        public void ProcessItem()
        {
            if (_cancellationPending)
            {
                App.ViewModel.Logger.Log("[ProcessItem] _cancellationPending!");
                _cancellationPending = false;
                _currentQueue.Clear();
                return;
            }

            //App.ViewModel.Logger.Log("[ProcessItem] _cancellationPending!");

            try
            {
                if (_currentQueue.Any())
                {
                    if (!NetworkInterface.GetIsNetworkAvailable())
                    {
                        App.ViewModel.Logger.Log("[ProcessItem] CONNECTION KO 1");
                        throw new MissingConnectionException("[ProcessItem] CONNECTION KO - GetIsNetworkAvailable false");
                    }

                    if (NetworkInterface.NetworkInterfaceType == NetworkInterfaceType.None)
                    {
                        App.ViewModel.Logger.Log("[ProcessItem] CONNECTION KO 2");
                        throw new MissingConnectionException("[ProcessItem] CONNECTION KO - NetworkInterfaceType == NetworkInterfaceType.None");
                    }

                    switch (_backupOperation)
                    {
                        case BackupOperationEn.Download:
                            _client = new WebClient();
#if !SIMULATE_DWN
                            _client.OpenReadCompleted -= new OpenReadCompletedEventHandler(client_OpenReadCompleted);
                            _client.OpenReadCompleted += new OpenReadCompletedEventHandler(client_OpenReadCompleted);
#endif
                            _client.DownloadProgressChanged -= new DownloadProgressChangedEventHandler(_client_DownloadProgressChanged);
                            _client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(_client_DownloadProgressChanged);
                            break;
                        case BackupOperationEn.UrlChecks:
                            break;
                    }

                    _curEpisode = _currentQueue.Dequeue();
                    ++_episodeCount;

                    App.ViewModel.DwnCurEpisode = _curEpisode;
                    App.ViewModel.DwnCurEpisodeId = _curEpisode.Id;
                    App.ViewModel.DwnCurEpisodeTitle = _curEpisode.Title;
                    App.ViewModel.DwnOverallPercentage = (_episodeCount * 100) / _totalEpisodes;

                    switch (_backupOperation)
                    {
                        case BackupOperationEn.Download:
                            App.ViewModel.DwnCurEpisodePercentage = 0;
                            break;
                        case BackupOperationEn.UrlChecks:
                            App.ViewModel.DwnOverallPercentage = (_episodeCount * 100) / _totalEpisodes;
                            break;
                    }

                    switch (_backupOperation)
                    {
                        case BackupOperationEn.Download:
                            App.ViewModel.Logger.Log("[ProcessItem] ##### starting download of: " + _curEpisode.Id);
                            //completed(null, ex) => http error
                            //completed(null, null) => url not found (eg quality not found)
                            //completed(uri, null) => success
                            YouTube.GetVideoUri(GenericHelper.GetYoutubeID(_curEpisode.Url),
                                                  YouTubeQuality.Quality480P,
                                                  (uri, ex) =>
                                                  {
                                                      if (ex == null && uri != null)
                                                      {
                                                          App.ViewModel.Logger.Log("[ProcessItem] URI before download OK!");
                                                          _curEpisode.ActualMP4Uri = uri;
                                                          _client.OpenReadAsync(_curEpisode.ActualMP4Uri.Uri);
                                                      }
                                                      else
                                                      {
                                                          throw new Exception(string.Format("[ep {0}] [ProcessItem] GetVideoUri error: {1}", new object[] { _curEpisode.Id, ex.Message }), ex);
                                                      }
                                                  });

                            //ProcessItem() in questo caso è chiamata dalla callback in "client_OpenReadCompleted"
                            break;

                        case BackupOperationEn.UrlChecks:
                            #region url checks
                            //App.ViewModel.Logger.Log("[ProcessItem] requesting URI for: " + _curEpisode.Id);
                            YouTube.GetVideoUri(GenericHelper.GetYoutubeID(_curEpisode.Url), YouTubeQuality.Quality480P, (uri, ex) =>
                            {
                                //completed(null, ex) => http error
                                //completed(null, null) => url not found (eg quality not found)
                                //completed(uri, null) => success
                                if (ex != null || uri == null)
                                {
                                    _wrongEpisodes.Add(_curEpisode);
                                }
                                else
                                {
                                    _curEpisode.ActualMP4Uri = uri;
                                    //save thumb offline
                                    var curThumbName = string.Format("thumb_{0}.png", _curEpisode.Id);
                                    Dispatcher.BeginInvoke(() =>
                                    {
                                        WriteableBitmap img = new WriteableBitmap((int)ImagePreview.Width, (int)ImagePreview.Height);
                                        img.Render(ImagePreview, null);
                                        img.Invalidate();
                                        using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                                        {
                                            if (isoStore.FileExists(curThumbName))
                                            {
                                                isoStore.DeleteFile(curThumbName);
                                            }
                                            using (var isoStoreFileStream = isoStore.CreateFile(curThumbName))
                                            {
                                                img.SaveJpeg(isoStoreFileStream, 150, 102, 0, 100);
                                            }
                                        }
                                    });
                                }

                                ProcessItem();
                            });
                            #endregion
                            break;
                    }
                }
                else
                {
                    #region queue is empty
                    App.ViewModel.Logger.Log("[ProcessItem] currentqueue is EMPTY...");
                    switch (_backupOperation)
                    {
                        case BackupOperationEn.Download:
                            App.ViewModel.Logger.Log("[ProcessItem] =============== download COMPLETED! ===");
                            //download completato - fine operazioni!
                            //_sw.Stop();
                            //var elapsed = _sw.Elapsed;
                            StopPreviewAnimation();
                            App.ViewModel.DwnInProgress = false;
                            EnableLockScreen();
                            GenericHelper.SetAppIsOffline(true);
                            App.ViewModel.LoadData();
                            App.ViewModel.BackupStage = BackupStageEn.DwnCompletedWithSuccess;
                            Dispatcher.BeginInvoke(() =>
                            {
                                //MessageBox.Show(elapsed.Seconds.ToString());
                                ButtonEndText.Text = AppResources.ButtonEndText_DwnOk;
                            });
                            break;
                        case BackupOperationEn.UrlChecks:
                            App.ViewModel.Logger.Log("[ProcessItem] url checks COMPLETED!");
                            if (_wrongEpisodes.Any())
                            {
                                //trovati link ko
                                App.ViewModel.Logger.Log("[ProcessItem] found KO episodes");
                                App.ViewModel.DwnInProgress = false;
                                App.ViewModel.BackupStage = BackupStageEn.LinksCheckedWithErrors;
                                EnableLockScreen();
                                Dispatcher.BeginInvoke(() =>
                                {
                                    ButtonEndText.Text = AppResources.ButtonEndText_LinksKO;
                                });
                                return;
                            }
                            else
                            {
                                //tutti i link sono, si può proseguire con il download
                                App.ViewModel.Logger.Log("[ProcessItem] no KO episodes, download starts");
                                _currentQueue = _itemsToDownload;
                                _retriedEpisodes.Clear();
                                _requeuedEpisodes.Clear();
                                _episodeCount = 0;
                                _backupOperation = BackupOperationEn.Download;
                                App.ViewModel.BackupStage = BackupStageEn.DwnEpisodesInProgress;
                                PlayPreviewAnimation();
                                ProcessItem();
                            }
                            break;
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                //_cancellationPending = true;
                ManageException(ex);
                return;
            }
        }

        private void ManageException(Exception ex)
        {
            App.ViewModel.Logger.Log("[ManageException] ex: " + ex.Message);
            App.ViewModel.Logger.Log("[ManageException] ex st: " + ex.StackTrace);

            App.ViewModel.DwnInProgress = false;

            if (ex is MissingConnectionException)
            {
                App.ViewModel.BackupStage = BackupStageEn.DwnCompletedWithConnectionError;
            }
            else
            {
                App.ViewModel.BackupStage = BackupStageEn.DwnCompletedWithError;
            }

            EnableLockScreen();
        }

#if SIMULATE_DWN
        void client_OpenReadCompleted(object sender, MyOpenReadCompletedEventArgs e)
#else
        void client_OpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
#endif
        {
            App.ViewModel.Logger.Log("[client_OpenReadCompleted] ep: " + _curEpisode.Id);

            if (_curEpisode.DwnRetries > 0)
            {
                App.ViewModel.Logger.Log("[client_OpenReadCompleted] WAS REQUEUED #" + _curEpisode.DwnRetries);
            }

            try
            {
                #region youtube stream error management
                if (e.Error != null && !string.IsNullOrEmpty(e.Error.Message))
                {
                    App.ViewModel.Logger.Log("******* ERROR *******");
                    App.ViewModel.Logger.Log("[client_OpenReadCompleted] e.Error.Message: " + e.Error.Message);
                    //App.ViewModel.Logger.Log("[client_OpenReadCompleted] e.Error.Stacktrace:\n" + e.Error.StackTrace);
                    if (e.Error.InnerException != null)
                    {
                        App.ViewModel.Logger.Log("[client_OpenReadCompleted] e.InnerException.Message: " + e.Error.InnerException.Message);
                        //App.ViewModel.Logger.Log("[client_OpenReadCompleted] e.InnerException.Stacktrace:\n" + e.Error.InnerException.StackTrace);
                    }

                    if (_curEpisode.DwnRetries > MaxAttempts)
                    {
                        throw new Exception(string.Format("[ep {0}] MAX RETRIES EXCEEDED -> error: {1}", new object[] { _curEpisode.Id, e.Error.Message }), e.Error.InnerException);
                    }
                    else
                    {
                        _itemsToDownload.Enqueue(_curEpisode);
                        App.ViewModel.Logger.Log("[client_OpenReadCompleted] new queue len : " + _itemsToDownload.Count);
                        _curEpisode.DwnRetries++;
                        _totalEpisodes++;
                        _client = null;
                        ProcessItem();
                        return;
                    }
                }
                #endregion

                string curEpisodeFileName = string.Empty;

                try
                {
                    curEpisodeFileName = GenericHelper.GetOfflineFileName(_curEpisode.Id.ToString());
                    App.ViewModel.Logger.Log("[client_OpenReadCompleted] curEpisodeFileName = " + curEpisodeFileName);
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("[ep {0}] GetOfflineFileName error: {1}", _curEpisode.Id, ex.Message), ex);
                }

                try
                {
                    if (_isoStore.FileExists(curEpisodeFileName))
                    {
                        _isoStore.DeleteFile(curEpisodeFileName);
                        App.ViewModel.Logger.Log("[client_OpenReadCompleted] removed " + curEpisodeFileName);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("[ep {0}] _isoStore.DeleteFile: {1}", _curEpisode.Id, ex.Message), ex);
                }

                try
                {
#if DEBUG
                    //if (_curEpisode.Id == 2)
                    //{
                    //    throw new Exception("test");
                    //}
#endif
                    App.ViewModel.Logger.Log("[client_OpenReadCompleted] starting isostore save process of: " + curEpisodeFileName);
                    using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(curEpisodeFileName, System.IO.FileMode.Create, _isoStore))
                    {
                        byte[] buffer = new byte[1024];
                        //while (e.Result.Read(buffer, 0, buffer.Length) > 0)
                        //{
                        //    stream.Write(buffer, 0, buffer.Length);
                        //}
                        //18-07-2013 - così non viene scritto tutto il buffer ma solo i dati realmente necessari
                        int bytesRead;
                        while ((bytesRead = e.Result.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            stream.Write(buffer, 0, bytesRead);
                        }
                    }
                    App.ViewModel.Logger.Log("[client_OpenReadCompleted] end OK isostore save process of: " + curEpisodeFileName);
                    //_currentEpisodeDownloadRetryNum = 0; //dwn ok, reset contatore tentativi
                    _curEpisode.OfflineFileName = curEpisodeFileName;
                }
                catch (Exception ex)
                {
                    App.ViewModel.Logger.Log("[client_OpenReadCompleted] end KO isostore save process of: " + curEpisodeFileName);
                    App.ViewModel.Logger.Log("[client_OpenReadCompleted] ex.Message " + ex.Message);
                    if (_curEpisode.DwnRetries == 0)
                    {
                        App.ViewModel.Logger.Log("[client_OpenReadCompleted] requeing : " + curEpisodeFileName);
                        //mette in coda l'episodio, riprova in un momento successivo
                        _itemsToDownload.Enqueue(_curEpisode);
                        App.ViewModel.Logger.Log("[client_OpenReadCompleted] new queue len : " + _itemsToDownload.Count);
                        _curEpisode.DwnRetries++;
                        _totalEpisodes++;
                        Thread.Sleep(5000);
                        //_currentEpisodeDownloadRetryNum = 0;
                    }
                    else
                    {
                        //throw new Exception(string.Format("[ep {0}] isostore write error: {1} (retryCount={2})",
                        //                                  new object[] { _curEpisode.Id, ex.Message, _currentEpisodeDownloadRetryNum }),
                        //                                  ex);
                        throw new Exception(string.Format("[ep {0}] isostore write error: {1}",
                                                         new object[] { _curEpisode.Id, ex.Message }),
                                                         ex);

                    }
                    //}
                }

                _client = null;
                ProcessItem();
            }
            catch (Exception ex)
            {
                ManageException(ex);
                return;
            }
        }

        private void ReportWrongEpisodes(List<ItemViewModel> list)
        {
            var email = new EmailComposeTask();
            email.To = "centapp@hotmail.com";

            email.Subject = string.Format("{0} backup errors report", App.ViewModel.AppName.ToUpper());
            email.Body += string.Format("\nApp version = '{0}'", GenericHelper.GetAppversion());
            email.Body += string.Format("\nApp language = '{0}'", App.ViewModel.NeutralCulture);
            email.Body += "\n";
            var titleCnv = new IdToTitleConverter();
            foreach (var item in list)
            {
                string epTitle = titleCnv.Convert(item.Id.ToString(), null, null, App.ViewModel.NeutralCulture).ToString();
                email.Body += string.Format("id={0}; title={1}; url={2}; origid={3}\n", new string[] { item.Id.ToString(), epTitle, item.Url, item.OrigId });
            }
            email.Show();
        }

        private void ButtonStartBackupEpisodes_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            //App.ViewModel.Logger.Log("[ButtonStartBackupEpisodes_Tap] incremental backup = " + _incrementalBackup);

            if (!NetworkInterface.GetIsNetworkAvailable() || NetworkInterface.NetworkInterfaceType == NetworkInterfaceType.None)
            {
                MessageBox.Show(AppResources.noNetworkAvailable);
                return;
            }

            DisableLockScreen();
            Init();
            StopPreviewAnimation();
            App.ViewModel.Logger.Log("[ButtonStartBackupEpisodes_Tap] starting queue analysis . . .");
            ProcessItem();
        }

        private void PlayPreviewAnimation()
        {
            if (LowMemoryHelper.IsLowMemDevice) return;

            Dispatcher.BeginInvoke(() =>
            {
                var ease = new QuadraticEase();
                ease.EasingMode = EasingMode.EaseInOut;
                _fadeAnimation.RepeatBehavior = RepeatBehavior.Forever;
                _fadeAnimation.StartOpacity = 1;
                _fadeAnimation.EndOpacity = 0.2;
                _fadeAnimation.Easing = ease;
                RadAnimationManager.Play(this.ImagePreview, _fadeAnimation);
            });
        }

        private void StopPreviewAnimation()
        {
            if (LowMemoryHelper.IsLowMemDevice) return;

            Dispatcher.BeginInvoke(() =>
            {
                RadAnimationManager.Stop(this.ImagePreview, _fadeAnimation);
            });
        }

        private void DisableLockScreen()
        {
            PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
        }

        private void EnableLockScreen()
        {
            PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Enabled;
        }

        private void ButtonEnd_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.ViewModel.Logger.Log("[ButtonEnd_Tap]");
            switch (App.ViewModel.BackupStage)
            {
                case BackupStageEn.LinksCheckedWithErrors:
                    ReportWrongEpisodes(_wrongEpisodes);
                    break;
                case BackupStageEn.DwnCompletedWithSuccess:
                    NavigationService.GoBack();
                    break;
            }
        }

        private void BorderBackupInfos_DoubleTap_1(object sender, System.Windows.Input.GestureEventArgs e)
        {
#if !DEBUG
            return;
#endif
            StringBuilder sb = new StringBuilder();

            //sb.AppendLine("-------");
            //sb.AppendLine("retried");
            //if (!_retriedEpisodes.Any())
            //{
            //    sb.Append("no retried episodes found");
            //}
            //else
            //{
            //    foreach (var item in _retriedEpisodes)
            //    {
            //        sb.Append(item);
            //        sb.AppendLine(";");
            //    }
            //}
            //sb.AppendLine();
            //sb.AppendLine("--------");
            //sb.AppendLine("requeued");
            //if (!_requeuedEpisodes.Any())
            //{
            //    sb.Append("no requeued episodes found");
            //}
            //else
            //{
            //    foreach (var item in _requeuedEpisodes)
            //    {
            //        sb.Append(item);
            //        sb.AppendLine(";");
            //    }
            //}
            //App.ViewModel.Logger.Log(sb.ToString());

            var email = new EmailComposeTask();
            email.To = "centapp@hotmail.com";
            email.Subject = "internal report";
            email.Body = App.ViewModel.Logger.GetLog();
            email.Show();

        }


    }
}