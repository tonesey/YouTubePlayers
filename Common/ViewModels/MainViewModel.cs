using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using Centapp.CartoonCommon.Helpers;
using Centapp.CartoonCommon.Converters;
using System.Xml.Linq;
using System.Net;
using System.IO;
using System.Threading;
using System.Windows.Threading;
using System.IO.IsolatedStorage;
using System.Net.NetworkInformation;
using Wp7Shared.Exceptions;
using System.Reflection;
using Centapp.CartoonCommon.Utility;
using System.Xml;
using System.Globalization;

namespace Centapp.CartoonCommon.ViewModels
{

    public delegate void AsyncMsgHandler(string msg, bool isFatalError);
    public delegate void OnLoadCompletedHandler();

    //public enum EnWorkingMode
    //{
    //    Undefined,
    //    Offline,
    //    Online
    //}

    public class MainViewModel : INotifyPropertyChanged
    {
        #region app main infos
        public string AppName { get; set; }
        public string IndexFile { get; set; }
        public CultureInfo NeutralCulture { get; set; }
        public bool IsMonoLang { get; set; }
        public int EpisodesLength { get; set; }
        public string MtiksId { get; set; }

        public bool IsAdvertisingEnabled { get; set; }
        public string AdUnitId { get; set; }
        public string ApplicationId { get; set; }
        #endregion

        //public Color GradientStop =  Colors.Yellow;
        //private const int TotalEpisodes = 100;

        int _dwnRetryCounter = 0;

        public event AsyncMsgHandler OnError;
        public event AsyncMsgHandler OnUserMessageRequired;

        public event OnLoadCompletedHandler OnLoadCompleted;

        private Logger _logger = new Logger();
        internal Logger Logger
        {
            get { return _logger; }
            set { _logger = value; }
        }

        // public EnWorkingMode WorkingMode { get; set; }

        IdToTitleConverter _cnv = new IdToTitleConverter();

        public Dispatcher CurrentDispatcher { get; set; }

        public MainViewModel()
        {
            //IsNetworkAvailable = true;
            this.Items = new ObservableCollection<ItemViewModel>();
            this.Items_Chunk1 = new ObservableCollection<ItemViewModel>();
            this.Items_Chunk2 = new ObservableCollection<ItemViewModel>();
            this.Items_Chunk3 = new ObservableCollection<ItemViewModel>();
            this.Items_Chunk4 = new ObservableCollection<ItemViewModel>();
        }

        #region DownloaderPage

        private BackupStageEn _BackupStage;
        public BackupStageEn BackupStage
        {
            get
            {
                return _BackupStage;
            }
            set
            {
                _logger.Log("[MainViewModel] _BackupStage = " + _BackupStage);
                if (value != _BackupStage)
                {
                    _BackupStage = value;
                    NotifyPropertyChanged("BackupStage");
                }
            }
        }


        private ItemViewModel _DwnCurEpisode { get; set; }
        public ItemViewModel DwnCurEpisode
        {
            get
            {
                return _DwnCurEpisode;
            }
            set
            {
                if (value != _DwnCurEpisode)
                {
                    _DwnCurEpisode = value;
                    NotifyPropertyChanged("DwnCurEpisode");
                }
            }
        }


        private int _DwnCurEpisodeId;
        public int DwnCurEpisodeId
        {
            get
            {
                return _DwnCurEpisodeId;
            }
            set
            {
                if (value != _DwnCurEpisodeId)
                {
                    _DwnCurEpisodeId = value;
                    NotifyPropertyChanged("DwnCurEpisodeId");
                }
            }
        }



        private string _DwnCurEpisodeTitle = "";
        public string DwnCurEpisodeTitle
        {
            get { return _DwnCurEpisodeTitle; }
            set
            {
                _DwnCurEpisodeTitle = value;
                NotifyPropertyChanged("DwnCurEpisodeTitle");
            }
        }

        private string _DwnCurEpisodeSpeed = "";
        public string DwnCurEpisodeSpeed
        {
            get { return _DwnCurEpisodeSpeed; }
            set
            {
                _DwnCurEpisodeSpeed = value;
                NotifyPropertyChanged("DwnCurEpisodeSpeed");
            }
        }

        private string _DwnCurEpisodeAmount = "";
        public string DwnCurEpisodeAmount
        {
            get { return _DwnCurEpisodeAmount; }
            set
            {
                _DwnCurEpisodeAmount = value;
                NotifyPropertyChanged("DwnCurEpisodeAmount");
            }
        }

        private int _DwnCurEpisodePercentage = 0;
        public int DwnCurEpisodePercentage
        {
            get { return _DwnCurEpisodePercentage; }
            set
            {
                _DwnCurEpisodePercentage = value;
                NotifyPropertyChanged("DwnCurEpisodePercentage");
            }
        }

        private int _DwnOverallPercentage = 0;
        public int DwnOverallPercentage
        {
            get { return _DwnOverallPercentage; }
            set
            {
                _DwnOverallPercentage = value;
                NotifyPropertyChanged("DwnOverallPercentage");
            }
        }

        private bool _DwnInProgress = false;
        public bool DwnInProgress
        {
            get { return _DwnInProgress; }
            set
            {
                _DwnInProgress = value;
                NotifyPropertyChanged("DwnInProgress");
            }
        }


        #endregion

        public bool IsDataLoaded
        {
            get;
            private set;
        }

        public bool AlreadyAskedRating
        {
            get;
            set;
        }

        private bool _isDataLoading = true;
        public bool IsDataLoading
        {
            get { return _isDataLoading; }
            set
            {
                _isDataLoading = value;
                NotifyPropertyChanged("IsDataLoading");
                NotifyPropertyChanged("IsNotDataLoading");
            }
        }

        public bool IsNotDataLoading
        {
            get { return !_isDataLoading; }
        }

        private ItemViewModel _selectedEpisode;
        public ItemViewModel SelectedEpisode
        {
            get
            {
                return _selectedEpisode;
            }
            set
            {
                _selectedEpisode = value;
                NotifyPropertyChanged("SelectedEpisode");
            }
        }

        #region items groupment
        private ObservableCollection<ItemViewModel> _items;
        public ObservableCollection<ItemViewModel> Items
        {
            get
            {
                return _items;
            }
            private set
            {
                _items = value;
                NotifyPropertyChanged("Items");
            }
        }

        private ObservableCollection<ItemViewModel> _items_chunk1;
        public ObservableCollection<ItemViewModel> Items_Chunk1
        {
            get
            {
                return _items_chunk1;
            }
            private set
            {
                _items_chunk1 = value;
                NotifyPropertyChanged("Items_Chunk1");
            }
        }

        private ObservableCollection<ItemViewModel> _items_chunk2;
        public ObservableCollection<ItemViewModel> Items_Chunk2
        {
            get
            {
                return _items_chunk2;
            }
            private set
            {
                _items_chunk2 = value;
                NotifyPropertyChanged("Items_Chunk2");
            }
        }

        private ObservableCollection<ItemViewModel> _items_chunk3;
        public ObservableCollection<ItemViewModel> Items_Chunk3
        {
            get
            {
                return _items_chunk3;
            }
            private set
            {
                _items_chunk3 = value;
                NotifyPropertyChanged("Items_Chunk3");
            }
        }


        private ObservableCollection<ItemViewModel> _items_chunk4;
        public ObservableCollection<ItemViewModel> Items_Chunk4
        {
            get
            {
                return _items_chunk4;
            }
            private set
            {
                _items_chunk4 = value;
                NotifyPropertyChanged("Items_Chunk4");
            }
        }

        #endregion

        public List<ItemViewModel> FavoriteEpisodes
        {
            get
            {
                return _items.Where(s => s.IsFavorite).ToList();
            }
        }

        #region online management
        public void DownloadItemsAsynch(string xmlUrl)
        {
            WebClient client = new WebClient();
            client.OpenReadCompleted += new OpenReadCompletedEventHandler(client_OpenReadCompleted);
            client.OpenReadAsync(new Uri(xmlUrl + "?" + Guid.NewGuid()), UriKind.Absolute);
        }


        void client_OpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            string errMsg = string.Empty;
            bool xmlLoadedFromIsoStore = false;

            App.ViewModel.Logger.Log("[client_OpenReadCompleted]");
            XDocument doc = new XDocument();

            #region xml load from web
            Stream webStream = null;
            if (e.Error != null)
            {
                if (_dwnRetryCounter <= 2)
                {
                    _dwnRetryCounter++;
                    App.ViewModel.Logger.Log(string.Format("[client_OpenReadCompleted] error -> retrying #{0}...", _dwnRetryCounter));
                    Thread.Sleep(_dwnRetryCounter * 2000);
                    LoadData();
                    return;
                }

                //try to load local xml
                App.ViewModel.Logger.Log("[client_OpenReadCompleted] error -> trying to load from isostore...");
                using (IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (isoStore.FileExists(GenericHelper.OfflineIndexFileName))
                    {
                        using (IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream(GenericHelper.OfflineIndexFileName, FileMode.Open, isoStore))
                        {
                            doc = LoadXmlFromStream(isoStream);
                            xmlLoadedFromIsoStore = true;
                            App.ViewModel.Logger.Log("[client_OpenReadCompleted] xml loaded from isostore successfully!");
                        }
                    }
                    else
                    {
                        App.ViewModel.Logger.Log("[client_OpenReadCompleted] cannot load xml from isostore...");
                    }
                }

                if (!xmlLoadedFromIsoStore)
                {
                    errMsg = string.Format("error during data download  (0) ({0})", e.Error.Message);
                    App.ViewModel.Logger.Log(string.Format("[client_OpenReadCompleted] e.Error != null - {0}", errMsg));
                    if (e.Error.InnerException != null)
                    {
                        App.ViewModel.Logger.Log(string.Format("[client_OpenReadCompleted] e.Error != null -  InnerEx - {0}", e.Error.InnerException.Message));
                    }

                    if (OnError != null) OnError(AppResources.ServerTemporaryUnavailable, true);
                    return;
                }
            }
            else
            {
                //tutto ok
                try
                {
                    webStream = e.Result;
                    doc = LoadXmlFromStream(webStream);
                }
                catch (XmlException ex)
                {
                    if (_dwnRetryCounter <= 2)
                    {
                        _dwnRetryCounter++;
                        App.ViewModel.Logger.Log(string.Format("[client_OpenReadCompleted] error during xml reading (1)x -> retrying #{0}...", _dwnRetryCounter));
                        Thread.Sleep(_dwnRetryCounter * 2000);
                        LoadData();
                        return;
                    }

                    errMsg = string.Format("error during xml reading (1) ({0})", ex.Message);
                    App.ViewModel.Logger.Log(string.Format("[client_OpenReadCompleted] XmlException - {0}", errMsg));
                    if (ex.InnerException != null)
                    {
                        App.ViewModel.Logger.Log(string.Format("[client_OpenReadCompleted] XmlException InnerEx - {0}", ex.InnerException.Message));
                    }

                    if (OnError != null) OnError(AppResources.ServerTemporaryUnavailable, true);
                    return;
                }
                catch (Exception ex)
                {
                    if (_dwnRetryCounter <= 2)
                    {
                        _dwnRetryCounter++;
                        App.ViewModel.Logger.Log(string.Format("[client_OpenReadCompleted] error during xml reading (2) -> retrying #{0}...", _dwnRetryCounter));
                        Thread.Sleep(_dwnRetryCounter * 2000);
                        LoadData();
                        return;
                    }

                    errMsg = string.Format("error during xml reading (2) ({0})", ex.Message);
                    App.ViewModel.Logger.Log(string.Format("[client_OpenReadCompleted] Exception - {0}", errMsg));
                    if (ex.InnerException != null)
                    {
                        App.ViewModel.Logger.Log(string.Format("[client_OpenReadCompleted] Exception InnerEx - {0}", ex.InnerException.Message));
                    }
                    
                    if (OnError != null) OnError(AppResources.ServerTemporaryUnavailable, true);
                    return;
                }
            }

            try
            {
                if (webStream != null)
                {
                    webStream.Close();
                }
            }
            catch (Exception ex)
            {
                errMsg = string.Format("error during xml reading (3) ({0})", ex.Message);
                App.ViewModel.Logger.Log(string.Format("[client_OpenReadCompleted] webStream.Close error - {0}", errMsg));
                //if (OnError != null) OnError(errMsg, true);
                throw new Exception(errMsg, ex);
            }
            #endregion

            if (!xmlLoadedFromIsoStore)
            {
                #region status management
                try
                {
                    var elStatus = doc.Element("root").Element("status");
                    if (elStatus != null)
                    {
                        var elTestMode = elStatus.Attribute("mode");
                        if ((elStatus.Attribute("value") != null && (elStatus.Attribute("value").Value != "ok")) ||
                             (elTestMode != null && elTestMode.Value == "testMsg"))
                        {
                            //ko o warning
                            var localizedMsg = elStatus.Attribute("defMsg").Value;

                            try
                            {
                                var localizedMsgNode = elStatus.Descendants("msg").FirstOrDefault(n => n.Attribute("culture").Value == Thread.CurrentThread.CurrentCulture.Name);
                                if (localizedMsgNode != null)
                                {
                                    localizedMsg = localizedMsgNode.Attribute("text").Value;
                                }
                            }
                            catch (Exception)
                            {
                                App.ViewModel.Logger.Log(string.Format("cannot find message for culture '{0}', using default", Thread.CurrentThread.CurrentCulture.Name));
                            }

                            bool mustExit = false;
                            bool msgRequired = true;

                            if (elStatus.Attribute("value").Value == "ko")
                            {
                                mustExit = true;
                            }
                            else if (elStatus.Attribute("value").Value == "warn")
                            {
                                mustExit = false;
                            }

                            if ((elStatus.Attribute("targetVer") != null) && (elStatus.Attribute("op") != null))
                            {
                                var targetVer = elStatus.Attribute("targetVer").Value;
                                var op = elStatus.Attribute("op").Value;

                                if (!string.IsNullOrEmpty(targetVer) && !string.IsNullOrEmpty(op))
                                {
                                    Version serverVer = new Version(targetVer);

                                    VersionFormat formatRequired = VersionFormat.VRB;

                                    if (serverVer.Major != -1 && serverVer.Minor != -1 && serverVer.Build != -1)
                                    {
                                        //VRB
                                        formatRequired = VersionFormat.VRB;
                                    }
                                    else if (serverVer.Major != -1 && serverVer.Minor != -1 && serverVer.Build == -1)
                                    {
                                        //VR
                                        formatRequired = VersionFormat.VR;
                                    }
                                    else if (serverVer.Major != -1 && serverVer.Minor == -1 && serverVer.Build == -1)
                                    {
                                        //V
                                        formatRequired = VersionFormat.V;
                                    }

                                    Version appVer = new Version(GenericHelper.GetAppversion(formatRequired));

                                    switch (op.ToUpper())
                                    {
                                        case "EQ":
                                            msgRequired = appVer == serverVer;
                                            break;
                                        case "LT":
                                            msgRequired = appVer < serverVer;
                                            break;
                                        case "GT":
                                            msgRequired = appVer > serverVer;
                                            break;
                                        default:
                                            break;
                                    }
                                }
                            }

                            if (msgRequired)
                            {
                                OnUserMessageRequired(localizedMsg, mustExit);
                                if (mustExit)
                                {
                                    return;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    errMsg = string.Format("error during status management ({0})", ex.Message);
                    App.ViewModel.Logger.Log(string.Format("[client_OpenReadCompleted] status management error - {0}", errMsg));
                    throw new Exception(errMsg, ex);
                }
                #endregion

                #region xml saving
                try
                {
                    SaveIndexToIsostore(doc);
                }
                catch (Exception ex)
                {
                    errMsg = string.Format("error during xml saving (5) ({0})", ex.Message);
                    App.ViewModel.Logger.Log(string.Format("[client_OpenReadCompleted] SaveIndexToIsostore error - {0}", errMsg));
                    //if (OnError != null) OnError(errMsg, true);
                    throw new Exception(errMsg, ex);
                }
                #endregion
            }

            try
            {
                InitResManager(doc);
            }
            catch (Exception ex)
            {
                errMsg = string.Format("error during dictionary init (6) ({0})", ex.Message);
                App.ViewModel.Logger.Log(string.Format("[client_OpenReadCompleted] InitResManager error - {0}", errMsg));
                //if (OnError != null) OnError(errMsg, true);
                throw new Exception(errMsg, ex);
            }

            try
            {
                BuildItemsFromXml(doc, false);
            }
            catch (Exception ex)
            {
                errMsg = string.Format("error during episodes init (6) ({0})", ex.Message);
                App.ViewModel.Logger.Log(string.Format("[client_OpenReadCompleted] BuildItemsFromXml error - {0}", errMsg));
                //if (OnError != null) OnError(errMsg, true);
                throw new Exception(errMsg, ex);
            }

        }

        private static XDocument LoadXmlFromStream(Stream stream)
        {
            XDocument doc = null;
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.DtdProcessing = DtdProcessing.Ignore;
            using (XmlReader reader = XmlReader.Create(stream, settings))
            {
                doc = XDocument.Load(reader);
            }
            return doc;
        }

        #region load/save index isostore
        private static void SaveIndexToIsostore(XDocument doc)
        {
            //salvataggio su file system doc
            try
            {
                using (IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (isoStore.FileExists(GenericHelper.OfflineIndexFileName))
                    {
                        isoStore.DeleteFile(GenericHelper.OfflineIndexFileName);
                    }
                    using (IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream(GenericHelper.OfflineIndexFileName, FileMode.Create, isoStore))
                    {
                        doc.Save(isoStream);
                    }
                }
            }
            catch (Exception ex)
            {
                App.ViewModel.Logger.Log(string.Format("[SaveIndexToIsostore] error - {0}", ex.Message));
                throw ex;
            }
        }

        private static XDocument LoadIndexFromIsostore()
        {
            XDocument doc = null;
            try
            {
                using (IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (isoStore.FileExists(GenericHelper.OfflineIndexFileName))
                    {
                        using (IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream(GenericHelper.OfflineIndexFileName, FileMode.Open, isoStore))
                        {
                            doc = XDocument.Load(isoStream);
                        }
                    }
                    else
                    {
                        throw new Exception("internal error" + GenericHelper.OfflineIndexFileName + " not found");
                    }
                }
            }
            catch (Exception ex)
            {
                App.ViewModel.Logger.Log(string.Format("[LoadIndexFromIsostore] error - {0}", ex.Message));
                throw ex;
            }
            return doc;
        }
        #endregion

        private void InitResManager(XDocument doc)
        {
            //var doc = LoadIndexFromIsostore();
            //App.ResManager = new MyResourceManager(doc, new CultureInfo("it-IT"));

            if (IsMonoLang)
            {
                //forza la cultura corrente ad essere quella dichiarata dall'AssmblyInfo 
                App.ResManager = new MyResourceManager(doc, App.ViewModel.NeutralCulture, App.ViewModel.NeutralCulture);
            }
            else
            {
                App.ResManager = new MyResourceManager(doc, Thread.CurrentThread.CurrentCulture, App.ViewModel.NeutralCulture);
            }

            //#if DEBUG
            //                        MessageBox.Show("test localizzazione");
            //                        App.ResManager = new MyResourceManager(doc, new System.Globalization.CultureInfo("pt-BR"));
            //#else
            //                        App.ResManager = new MyResourceManager(doc);
            //#endif
        }

        private void BuildItemsFromXml(XDocument doc, bool appIsOffline)
        {
            //<root>
            //  <item id="1">
            //    <url>http://www.youtube.com/watch?v=eHmj8AzRlH4</url>
            //    <origId>43</origId>
            //  </item>

            int index = 0;
            var items = new ObservableCollection<ItemViewModel>((from item in doc.Element("root").Descendants("item")
                                                                 select new ItemViewModel()
                                                                 {
                                                                     //Id = int.Parse(item.Attribute("id").Value),
                                                                     Id = ++index,
                                                                     OrigId = item.Element("origId") != null ? item.Element("origId").Value : string.Empty,
                                                                     Url = item.Element("url").Value,
                                                                     //IsAvailableInTrial = appIsOffline ? true : int.Parse(item.Attribute("id").Value) <= 5
                                                                     IsAvailableInTrial = appIsOffline ? true : index <= 5
                                                                 }).ToList());
            for (int i = 1; i <= items.Count; i++)
            {
                ItemViewModel curEpisode = items.ElementAt(i - 1);
                int episodeId = curEpisode.Id;
                //curEpisode.Progressive = i;
                curEpisode.Title = _cnv.Convert(episodeId);
                if (GenericHelper.FavoriteEpisodesIdsSettingValue.Contains(episodeId))
                {
                    curEpisode.IsFavorite = true;
                }
                if (appIsOffline)
                {
                    curEpisode.OfflineFileName = GenericHelper.GetOfflineFileName(episodeId.ToString());
                }
            }

#if DEBUG
            var duplicates = items
            .GroupBy(i => i.Title)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

            foreach (var item in duplicates)
            {
                MessageBox.Show(item + "  is duplicated!");
            }
#endif

            Items = items;
            Items_Chunk1 = new ObservableCollection<ItemViewModel>(items.Where(n => n.Id >= 1 && n.Id <= 25));
            Items_Chunk2 = new ObservableCollection<ItemViewModel>(items.Where(n => n.Id >= 26 && n.Id <= 50));
            Items_Chunk3 = new ObservableCollection<ItemViewModel>(items.Where(n => n.Id >= 51 && n.Id <= 75));
            Items_Chunk4 = new ObservableCollection<ItemViewModel>(items.Where(n => n.Id >= 76 && n.Id <= 100));

            this.IsDataLoaded = true;
            IsDataLoading = false;

            NotifyPropertyChanged("FavoriteEpisodes");

            if (OnLoadCompleted != null) OnLoadCompleted();
        }
        #endregion


        public void LoadData()
        {
            IsDataLoading = true;
            if (!GenericHelper.AppIsOfflineSettingValue)
            {
                DownloadItemsAsynch(string.Format("http://centapp.altervista.org/{0}", App.ViewModel.IndexFile));
                //DownloadItemsAsynch("https://skydrive.live.com/redir?resid=9CD47B76DA8EF008!5643&authkey=!ALLzIiEIyiZ5m9E");
                //MessageBox.Show("TEST DATA KO");
            }
            else
            {
                var doc = LoadIndexFromIsostore();
                InitResManager(doc);
                BuildItemsFromXml(doc, true);
            }
        }

        #region favorites
        internal void AddToFavorites(object item)
        {
            int id = (item as ItemViewModel).Id;
            (item as ItemViewModel).IsFavorite = true;
            if (!GenericHelper.FavoriteEpisodesIdsSettingValue.Contains(id))
            {
                GenericHelper.FavoriteEpisodesIdsSettingValue.Add(id);
            }
            GenericHelper.Writekey(GenericHelper.FavoriteEpisodesKey, GenericHelper.FavoriteEpisodesIdsSettingValue);
            NotifyPropertyChanged("FavoriteEpisodes");
        }

        internal void RemoveFromFavorites(object item)
        {
            int id = (item as ItemViewModel).Id;
            (item as ItemViewModel).IsFavorite = false;
            if (GenericHelper.FavoriteEpisodesIdsSettingValue.Contains(id))
            {
                GenericHelper.FavoriteEpisodesIdsSettingValue.Remove(id);
            }
            GenericHelper.Writekey(GenericHelper.FavoriteEpisodesKey, GenericHelper.FavoriteEpisodesIdsSettingValue);
            NotifyPropertyChanged("FavoriteEpisodes");
        }
        #endregion

        #region INotify
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null == handler)
            {
                return;
            }
            if (CurrentDispatcher != null)
            {
                CurrentDispatcher.BeginInvoke(() =>
                {
                    handler(this, new PropertyChangedEventArgs(propertyName));
                });
            }
            else
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion






        public bool DownloadIsAllowed { get; set; }

        public bool InfoPageIsPivot { get; set; }

        public bool ShowOtherApps { get; set; }

        public string CustomFirstPivotItemName { get; set; }

        public Uri CurrentYoutubeMP4Uri { get; set; }

        public string CurrentYoutubeMP4FileName { get; set; }
    }
}