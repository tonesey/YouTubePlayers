using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Centapp.CartoonCommon
{
    class AppInfo
    {

        public const string OfflineIndexFileNameXml = "offline.xml";
        public const string OfflineIndexFileNameJSON = "offline.json";
        public const string DataBackupFileName = "backup.xml";

        private static AppInfo _instance = null;
        public static AppInfo Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AppInfo();
                }
                return _instance;
            }
        }

        public string AppName { get; set; }
        public string IndexFile { get; set; }
        public bool UseJSon
        {
            get
            {
                return IndexFile != null && IndexFile.Contains("_json");
            }
        }

        public bool DownloadIsAllowed { get; set; }
        public bool OfflineRevertWarningRequired { get; set; }
        public bool XmlToJSONRequiresOfflineReset { get; set; }

        public bool InfoPageIsPivot { get; set; }
        public bool ShowOtherApps { get; set; }
        public string CustomFirstPivotItemName { get; set; }

        public CultureInfo NeutralCulture { get; set; }
        public bool IsMonoLang { get; set; }
        public bool UseResManager { get; set; }
        public int EpisodesLength { get; set; }

        #region adv/analytics
        public string MtiksId { get; set; }
        public bool IsAdvertisingEnabled { get; set; }
        public AdvProvider AdvProvider { get; set; }
        public string AdUnitId { get; set; }
        public string ApplicationId { get; set; }
        public string AdSpaceId { get; set; }
        public string AdPublisherId { get; set; }
        #endregion




        public bool EpisodesGroupedBySeasons { get; set; }
    }
}
