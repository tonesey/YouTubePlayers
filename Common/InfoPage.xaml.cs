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
using Centapp.CartoonCommon;

namespace Centapp.CartoonCommon
{
    public partial class InfoPage : PhoneApplicationPage
    {
        public InfoPage()
        {
            InitializeComponent();
            InfoTextBlock1.Text = AppResources.disclaimerMessage;
            //backButton.Content = AppResources.backButtonText;
            //buyAppButton.Content = AppResources.buyPage_buyApp;
            //noThanksButton.Content = AppResources.buyPage_noThanks;
        }

        //protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        //{
        //}

        //private void backButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        //{
        //    NavigationService.GoBack();
        //}

    }
}