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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Centapp.CartoonCommon.Helpers;
using Microsoft.Phone.Marketplace;
using Centapp.CartoonCommon.Utility;
using System.Reflection;
using System.Threading;
#if NOINTERNET
#else
using Microsoft.Phone.Net.NetworkInformation;
#endif
using Wp7Shared.Exceptions;
using Centapp.CartoonCommon.ViewModels;
using Centapp.CartoonCommon;
using System.IO;
using System.Xml.Linq;
using Centapp.CartoonCommon;
using System.Globalization;
using System.Resources;
using com.mtiks.winmobile;
using Microsoft.Phone.Info;
using Wp7Shared.Helpers;

namespace Centapp.CartoonCommon
{
    public partial class App : Application
    {
        private static MainViewModel viewModel = null;
        private bool _wasApplicationTerminated = true;
        public static MyResourceManager ResManager { get; set; }

        //internal static ManualResetEvent exitEvent = new ManualResetEvent(false); 

        /// <summary>
        /// A static ViewModel used by the views to bind against.
        /// </summary>
        /// <returns>The MainViewModel object.</returns>
        public static MainViewModel ViewModel
        {
            get
            {
                // Delay creation of the view model until necessary
                if (viewModel == null)
                    viewModel = new MainViewModel();

                return viewModel;
            }
        }

        /// <summary>
        /// Provides easy access to the root frame of the Phone Application.
        /// </summary>
        /// <returns>The root frame of the Phone Application.</returns>
        public PhoneApplicationFrame RootFrame { get; private set; }

        /// <summary>
        /// Constructor for the Application object.
        /// </summary>
        public App()
        {
            // Global handler for uncaught exceptions. 
            UnhandledException += Application_UnhandledException;

            // Standard Silverlight initialization
            InitializeComponent();

            // Phone-specific initialization
            InitializePhoneApplication();

            // Show graphics profiling information while debugging.
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // Display the current frame rate counters.
                //Application.Current.Host.Settings.EnableFrameRateCounter = true;

                // Show the areas of the app that are being redrawn in each frame.
                //Application.Current.Host.Settings.EnableRedrawRegions = true;

                // Enable non-production analysis visualization mode, 
                // which shows areas of a page that are handed off to GPU with a colored overlay.
                //Application.Current.Host.Settings.EnableCacheVisualization = true;

                // Disable the application idle detection by setting the UserIdleDetectionMode property of the
                // application's PhoneApplicationService object to Disabled.
                // Caution:- Use this under debug mode only. Application that disables user idle detection will continue to run
                // and consume battery power when the user is not using the phone.
                //PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }

        }


        //private void ReportLittleWatsonException(Exception e, string source)
        //{

        //}

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            FeedbackHelper.Default.Launching();
            InitApp();
            LittleWatson.CheckForPreviousException(AppResources.ExceptionMessage, AppResources.ExceptionMessageTitle);
        }

        private void InitApp()
        {
            ParseAppInfo();

            try
            {
                if (!string.IsNullOrEmpty(AppInfo.Instance.MtiksId))
                {
                    mtiks.Instance.Start(AppInfo.Instance.MtiksId, Assembly.GetExecutingAssembly());
                }
            }
            catch (Exception ex)
            {
            }

            CheckTrialState();
            GenericHelper.ReadAppSettings();
            if (!GenericHelper.AppIsOfflineSettingValue && !NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBox.Show(AppResources.noNetworkAvailable);
                return;
            }
            App.ViewModel.LoadData();
        }

        private static void ParseAppInfo()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            Stream stream = asm.GetManifestResourceStream("Centapp.CartoonCommon.appInfo.xml");
            var doc = XDocument.Load(stream);

            string appName = doc.Root.Attribute("friendlyName").Value.ToLower();
            string indexFile = doc.Root.Attribute("indexFile").Value;
            //default = false
            bool isMonoLang = doc.Root.Attribute("isMonoLang") != null ? bool.Parse(doc.Root.Attribute("isMonoLang").Value) : false;
            bool useResManager = doc.Root.Attribute("useResManager") != null ? bool.Parse(doc.Root.Attribute("useResManager").Value) : true;
            int episodesLength = doc.Root.Attribute("episodesAverageLength") != null ? int.Parse(doc.Root.Attribute("episodesAverageLength").Value) : -1;
            string mtiksId = doc.Root.Attribute("mtiksId") != null ? doc.Root.Attribute("mtiksId").Value : "";

            bool infoPageIsPivot = false;
            if (doc.Root.Element("infoSection") != null && doc.Root.Element("infoSection").Attribute("usePivot") != null)
            {
                infoPageIsPivot = doc.Root.Element("infoSection").Attribute("usePivot").Value == "true";
            }

            bool downloadIsAllowed = true;
            if (doc.Root.Element("offlineManagement") != null && doc.Root.Element("offlineManagement").Attribute("isAllowed") != null)
            {
                downloadIsAllowed = doc.Root.Element("offlineManagement").Attribute("isAllowed").Value == "true";
            }

            bool showOtherApps = true;
            if (doc.Root.Element("misc") != null && doc.Root.Element("misc").Attribute("showOtherApps") != null)
            {
                showOtherApps = doc.Root.Element("misc").Attribute("showOtherApps").Value == "true";
            }

            string customFirstPivotItemName = "";
            if (doc.Root.Element("firstPivotItemName") != null && doc.Root.Element("firstPivotItemName").Value != null)
            {
                customFirstPivotItemName = doc.Root.Element("firstPivotItemName").Value;
            }

            bool usesAdvertising = false;
            //pubcenter
            string adUnitId = string.Empty;
            string applicationId = string.Empty;
            //soma
            string adSpaceId = string.Empty;
            string adPublisherId = string.Empty;
            AdvProvider provider = AdvProvider.Undefined;

            if (doc.Root.Element("adv") != null)
            {
                usesAdvertising = true;

                if (doc.Root.Element("adv").Attribute("provider") == null || (doc.Root.Element("adv").Attribute("provider") != null && doc.Root.Element("adv").Attribute("provider").Value == "pubcenter"))
                {
                    //DEFAULT
                    provider = AdvProvider.PubCenter;
                    adUnitId = doc.Root.Element("adv").Attribute("AdUnitId").Value;
                    applicationId = doc.Root.Element("adv").Attribute("ApplicationId").Value;
                }
                else if (doc.Root.Element("adv").Attribute("provider").Value == "soma")
                {
                    provider = AdvProvider.Sooma;
                    adSpaceId = doc.Root.Element("adv").Attribute("AdSpaceId").Value;
                    adPublisherId = doc.Root.Element("adv").Attribute("PublisherID").Value;
                }
                else if (doc.Root.Element("adv").Attribute("provider").Value == "myapp")
                {
                    provider = AdvProvider.MyAppPromotion;
                    //TODO parametrizzare id app
                    //appId = doc.Root.Element("adv").Attribute("appId").Value;
                }
            }

            var attributes = asm.GetCustomAttributes(typeof(System.Resources.NeutralResourcesLanguageAttribute), false);
            var defLang = (attributes.First() as NeutralResourcesLanguageAttribute).CultureName;

            AppInfo.Instance.CustomFirstPivotItemName = customFirstPivotItemName;
            AppInfo.Instance.ShowOtherApps = showOtherApps;
            AppInfo.Instance.DownloadIsAllowed = downloadIsAllowed;
            AppInfo.Instance.InfoPageIsPivot = infoPageIsPivot;
            AppInfo.Instance.AppName = appName;
            AppInfo.Instance.IndexFile = indexFile;
            AppInfo.Instance.IsMonoLang = isMonoLang;
            AppInfo.Instance.UseResManager = useResManager;
            AppInfo.Instance.NeutralCulture = new CultureInfo(defLang);
            AppInfo.Instance.EpisodesLength = episodesLength;
            AppInfo.Instance.MtiksId = mtiksId;
            AppInfo.Instance.IsAdvertisingEnabled = usesAdvertising;
            AppInfo.Instance.AdvProvider = provider;
            AppInfo.Instance.AdUnitId = adUnitId;
            AppInfo.Instance.ApplicationId = applicationId;
            AppInfo.Instance.AdSpaceId = adSpaceId;
            AppInfo.Instance.AdPublisherId = adPublisherId;
        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            if (_wasApplicationTerminated)
            {
                // real tombstone, new App instance   
                InitApp();
            }
            else
            {
                //must have been a chooser that did not tombstone or a quick back. 
            }
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            _wasApplicationTerminated = false;

            // Ensure that required application state is persisted here.
            try
            {
                mtiks.Instance.Stop();
            }
            catch (Exception)
            {
            }
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
            try
            {
                mtiks.Instance.Stop();
            }
            catch (Exception)
            {
            }
        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }

            if (!(e.ExceptionObject is ForcedExitException))
            {
                string extraInfos = string.Empty;
                extraInfos += "--------------------------------------------\n";
                extraInfos = "- App version: " + GenericHelper.GetAppversion() + "\n" +
                             "- OS ver: " + System.Environment.OSVersion.Version.ToString() + "\n" +
                             "- Trial: " + IsTrial + "\n" +
                             "- Offline: " + GenericHelper.AppIsOfflineSettingValue;
                extraInfos += "\n";
                extraInfos += "Phone infos:\n";

                string phoneNameStr = "-unknown-";
                try
                {
                    var phoneInfo = Wp7Shared.Helpers.PhoneNameResolver.Resolve(DeviceStatus.DeviceManufacturer, DeviceStatus.DeviceName);
                    if (phoneInfo.IsResolved)
                    {
                        phoneNameStr = phoneInfo.FullCanonicalName;
                    }
                    else
                    {
                        phoneNameStr = string.Format("{0} {1}", phoneInfo.ReportedManufacturer, phoneInfo.ReportedModel);
                    }
                }
                catch (Exception)
                {
                }
                extraInfos += phoneNameStr;
                extraInfos += "\n";
                extraInfos += "Internal log:";
                extraInfos += "\n";
                extraInfos += App.ViewModel.Logger.GetLog();

                LittleWatson.StoreExceptionDetails(e.ExceptionObject, extraInfos);
            }
        }

        #region Phone application initialization

        // Avoid double-initialization
        private bool phoneApplicationInitialized = false;

        // Do not add any additional code to this method
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            RootFrame = new PhoneApplicationFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;
        }

        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Set the root visual to allow the application to render
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        #endregion


        #region trial management
        private bool _isTrial = true;
        public bool IsTrial
        {
            get { return _isTrial; }
            private set
            {
                _isTrial = value;

                //if (value != _isTrial)
                //{
                //    if (LicenceInfoChanged != null)
                //    {
                //        LicenceInfoChanged(value);
                //    }
                //}
            }
        }

        private void DetermineIsTrial()
        {
#if TRIAL
            //return true if debugging with trial enabled (DebugTrial configuration is active)
            IsTrial = true;
#else
            var license = new LicenseInformation();
            IsTrial = license.IsTrial();
#endif
        }

        private void CheckTrialState()
        {
            DetermineIsTrial();
        }

        #endregion


    }
}