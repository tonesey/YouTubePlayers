using System.Windows;
using System.Windows.Controls.Primitives;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;


namespace Centapp.CartoonCommon
{

    public delegate void OkPressedEventHandler();
    public delegate void CancelPressedEventHandler(PopupCancelPressedActionToExecute methodToExec);

    public partial class ReminderPopup
    {
        public event OkPressedEventHandler OkPressedEvent;
        public event CancelPressedEventHandler CancelPressedEvent;
        public event PopupClosedEventHandler PopupClosedEvent;

        private readonly Popup _popup;
        private PopupCancelPressedActionToExecute methodToExec;


        public ReminderPopup(Popup popup, PopupCancelPressedActionToExecute methodToExec)
        {
            InitializeComponent();
            LocalizeUI();
            this.methodToExec = methodToExec;
            _popup = popup;
        }

        private void LocalizeUI()
        {
            //txtTrial.Text = LocalizedResources.trialMessageWarning;
            //buyAppButton.Content = LocalizedResources.buyAppButtonText;
            //btnContinueTrial.Content = LocalizedResources.btnContinueTrialText;
        }

        private void btnBuyNow_Click(object sender, RoutedEventArgs e)
        {
            if (OkPressedEvent != null)
            {
                OkPressedEvent();
            }
            ClosePopup();
        }

        private void btnContinueTrial_Click(object sender, RoutedEventArgs e)
        {
            if (CancelPressedEvent != null)
            {
                CancelPressedEvent(methodToExec);
            }
            ClosePopup();
        }

        private void ClosePopup()
        {
            if (_popup != null)
            {
                _popup.IsOpen = false;
            }

            if (PopupClosedEvent != null)
            {
                PopupClosedEvent();
            }
        }
    }
}