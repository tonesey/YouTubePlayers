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
using System.Globalization;
using System.Threading;
using Centapp.CartoonCommon.JSON;
using Centapp.CartoonCommon.ViewModels;
using System.Reflection;
using System.IO;

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

        public static void ReadAppSettings()
        {
            object favoriteEpisodes = Readkey(FavoriteEpisodesKey);
            FavoriteEpisodesIdsSettingValue = favoriteEpisodes == null ? new List<int>() : (List<int>)favoriteEpisodes;

            object appIsOffline = Readkey(AppIsOfflineKey);
            AppIsOfflineSettingValue = appIsOffline == null ? false : (bool)appIsOffline;

            if (AppIsOfflineSettingValue)
            {
                //offline
                if (AppInfo.Instance.UseJSon)
                {
                    using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (isoStore.FileExists(AppInfo.OfflineIndexFileNameXml))
                        {
                            //è il caso di un passaggio di un update di un app offline con indice xml
                            if (AppInfo.Instance.XmlToJSONRequiresOfflineReset)
                            {
                                //l'app torna ad essere "online" in quando gli episodi sono cambiati (Peppa)
                                RemoveLocalIndexXml(isoStore);
                                AppInfo.Instance.OfflineRevertWarningRequired = true;
                                SetAppIsOffline(false);
                                RemoveOfflineData(isoStore);
                            }
                            else
                            {
                                //l'app può mantenere gli episodi scaricati
                                //è necessario convertire xml locale in json
                                //ATTENZIONE: la conversione è prevista solo per episodi non divisi in stagioni (Pingu)
                                //in futuro sviluppare funzione di conversione anche per altri (Peppa)
                                ConvertOfflineXmlToJSON();
                                RemoveLocalIndexXml(isoStore);
                            }
                        }
                        else
                        {
                            if (!isoStore.FileExists(AppInfo.OfflineIndexFileNameJSON))
                            {
                                //non esistono i file offline ne' in jsono ne' in xml: sono le versioni offline bacate di Pingu (3.1.19).
                                //si copia un file json "fix" da risorsa locale
                                Assembly asm = Assembly.GetExecutingAssembly();
                                Stream stream = asm.GetManifestResourceStream("Centapp.CartoonCommon.Resources.pinguoffline.json");
                                using (IsolatedStorageFileStream outStream = new IsolatedStorageFileStream(AppInfo.OfflineIndexFileNameJSON, FileMode.CreateNew, isoStore))
                                {
                                    byte[] buffer = new byte[1024];
                                    int bytesRead;
                                    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                                    {
                                        outStream.Write(buffer, 0, bytesRead);
                                    }
                                    outStream.Flush();
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (AppInfo.Instance.UseJSon)
                {
                    //per pulizia viene eliminato il vecchio file indice xml anche sulle versioni online
                    using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (isoStore.FileExists(AppInfo.OfflineIndexFileNameXml))
                        {
                            RemoveLocalIndexXml(isoStore);
                        }
                    }
                }
            }

            object onlineUsagesCount = Readkey(OnlineUsagesKey);
            OnlineUsagesSettingValue = (onlineUsagesCount == null || string.IsNullOrEmpty(onlineUsagesCount.ToString())) ? 0 : int.Parse(onlineUsagesCount.ToString());
        }

        private static void RemoveLocalIndexXml(IsolatedStorageFile isoStore)
        {
            isoStore.DeleteFile(AppInfo.OfflineIndexFileNameXml);
        }

        private static void RemoveOfflineData(IsolatedStorageFile isoStore)
        {
            try
            {

                foreach (var item in isoStore.GetFileNames("ep*.mp4"))
                {
                    isoStore.DeleteFile(item);
                }

                foreach (var item in isoStore.GetFileNames("thumb_*.png"))
                {
                    isoStore.DeleteFile(item);
                }
            }
            catch (Exception)
            {
            }
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

        private static void ConvertOfflineXmlToJSON()
        {
            var doc = MainViewModel.LoadIndexFromIsostoreXML();
            int index = 0;

            RootObjectFlatEpisodes r = new RootObjectFlatEpisodes();
            r.episodes = new List<MyEpisode>();

            var status = doc.Element("root").Element("status");
            r.statusmsg = status.Attribute("defMsg").Value;
            r.value = status.Attribute("value").Value;
            r.targetVer = status.Attribute("targetVer").Value;
            r.op = status.Attribute("op").Value;

            var items = doc.Element("root").Descendants("item");
            foreach (var itemXml in items)
            {
                var id = (itemXml.Attribute("id").Value);
                var youtube_id = GetYoutubeID(itemXml.Element("url").Value);
                var descs = itemXml.Element("desc").Descendants("descItem");

                Dictionary<string, string> descsDict = new Dictionary<string, string>();
                foreach (var desc in descs)
                {
                    descsDict.Add(desc.Attribute("lang").Value, desc.Attribute("value").Value);
                }

                var jsonEpisode = new MyEpisode { id = int.Parse(id), youtube_id = youtube_id, names = descsDict };
                r.episodes.Add(jsonEpisode);
            }

            string objTostr = Newtonsoft.Json.JsonConvert.SerializeObject(r);
            MainViewModel.SaveIndexToIsostoreJSON(objTostr);
        }

    }
}
