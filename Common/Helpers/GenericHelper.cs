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
using System.IO.IsolatedStorage;
using System.Collections.Generic;
using System.Windows.Navigation;
using System.Xml.Linq;

namespace Centapp.CartoonCommon.Helpers
{

    public enum VersionFormat
    {
        V,
        VR,
        VRB
    }

    public class GenericHelper
    {

        public const string OfflineIndexFileName = "offline.xml";
        public const string DataBackupFileName = "backup.xml";

        public const string FavoriteEpisodesKey = "FavoriteEpisodesIds";
        public const string AppIsOfflineKey = "AppIsOffline";
        public const string OnlineUsagesKey = "OnlineUsages";

        public const string UsageKeyName = "usage";
        public const string MaxNumberKeyName = "number";

        #region settings values
        public static List<int> FavoriteEpisodesIdsSettingValue { set; get; }
        public static bool AppIsOfflineSettingValue { set; get; }
        public static int OnlineUsagesSettingValue { set; get; }
        #endregion

        public static void ReadGlobalVariables()
        {
            object favoriteEpisodes = Readkey(FavoriteEpisodesKey);
            FavoriteEpisodesIdsSettingValue = favoriteEpisodes == null ? new List<int>() : (List<int>)favoriteEpisodes;

            object appIsOffline = Readkey(AppIsOfflineKey);
            AppIsOfflineSettingValue = appIsOffline == null ? false : (bool)appIsOffline;

            object onlineUsagesCount = Readkey(OnlineUsagesKey);
            OnlineUsagesSettingValue = (onlineUsagesCount == null || string.IsNullOrEmpty(onlineUsagesCount.ToString())) ? 0 : int.Parse(onlineUsagesCount.ToString());
        }

        public static void Writekey(string key, object value)
        {
            if (IsolatedStorageSettings.ApplicationSettings.Contains(key))
            {
                IsolatedStorageSettings.ApplicationSettings[key] = value;
            }
            else
            {
                IsolatedStorageSettings.ApplicationSettings.Add(key, value);
            }

            IsolatedStorageSettings.ApplicationSettings.Save();
        }

        internal static void IncrementOnlineUsagesCount()
        {
            OnlineUsagesSettingValue++;
            Writekey(OnlineUsagesKey, OnlineUsagesSettingValue);
        }

        internal static void SetAppIsOffline(bool val)
        {
            AppIsOfflineSettingValue = val;
            Writekey(AppIsOfflineKey, AppIsOfflineSettingValue);
        }

        public static object Readkey(string key)
        {
            return IsolatedStorageSettings.ApplicationSettings.Contains(key) ? IsolatedStorageSettings.ApplicationSettings[key] : null;
        }

        #region misc
        internal static string GetOfflineFileName(string id)
        {
            return string.Format("ep_{0}.mp4", id);
        }

        internal static string GetAppversion(VersionFormat format)
        {
            var appEl = XDocument.Load("WMAppManifest.xml").Root.Element("App");
            var ver = new Version(appEl.Attribute("Version").Value);

            switch (format)
            {
                case VersionFormat.V:
                    return string.Format("{0}", ver.Major);
                case VersionFormat.VR:
                    return string.Format("{0}.{1}", ver.Major, ver.Minor);
            }

            return string.Format("{0}.{1}.{2}", ver.Major, ver.Minor, ver.Build);
        }

        internal static string GetAppversion()
        {
            return GetAppversion(VersionFormat.VRB);
        }

        internal static string GetYoutubeID(string uri)
        {
            ////http://www.youtube.com/watch?v=1CuGUN_rmpE
            //string id = "Uh_tZEkIVS4";
            if (string.IsNullOrEmpty(uri)) throw new ArgumentException("uri not valid");
            return uri.Substring(uri.IndexOf('=') + 1);
        }
        #endregion


        //public static MyResourceManager Resourcemanager { get; set; }
    }
}
