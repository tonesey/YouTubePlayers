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
using Microsoft.Phone.Tasks;
using Centapp.CartoonCommon.Helpers;
using Centapp.CartoonCommon;

namespace Centapp.CartoonCommon
{
    public partial class BuyAppPage : PhoneApplicationPage
    {

        public BuyAppPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            string usageKey = this.NavigationContext.QueryString[GenericHelper.UsageKeyName];
           
            AppFunctionToLimit usage = (AppFunctionToLimit)(Enum.Parse(typeof(AppFunctionToLimit), usageKey, true));

            switch (usage)
            {
                case AppFunctionToLimit.EpisodeCount:
                    InfoTextBlock2.Text = AppResources.trialMessageEpisodes;
                    break;
                case AppFunctionToLimit.BackupEpisodes:
                    InfoTextBlock2.Text = AppResources.trialMessageBackup;
                    break;
            }
               
            buyAppButton.Content = AppResources.buyPage_buyApp;
            noThanksButton.Content = AppResources.buyPage_noThanks;

        }

        private void buyAppButton_Tap(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {

                //@B|1.4|Microsoft.Phone.Tasks.MarketplaceLauncher.Show - InvalidOperationException
                try
                {
                    var marketplaceDetailTask = new MarketplaceDetailTask();
                    marketplaceDetailTask.ContentIdentifier = null;
                    marketplaceDetailTask.Show();
                }
                catch (InvalidOperationException)
                {
                    //MessageBox.Show(AppResources.error_buy);
                }
                catch (Exception)
                {
                    MessageBox.Show(AppResources.error_buy);
                }
            });
        }

        private void noThanksButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}