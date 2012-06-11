using Microsoft.Phone.Controls;
using System.Windows;

namespace Raspberry_Pi.View
{
    /// <summary>
    /// Description for PairWithServerView.
    /// </summary>
    public partial class PairWithServerView : PhoneApplicationPage
    {
        public bool PairedSuccessfully { get; set; }

        private Services.ConnectionService connectionService;

        /// <summary>
        /// Initializes a new instance of the PairWithServerView class.
        /// </summary>
        public PairWithServerView()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            connectionService = Services.Forwarder.GetService<Services.ConnectionService>();
            connectionService.ServerPairSuccessful += connectionService_ServerPairSuccessful;
            connectionService.ServerPairUnsuccessful += connectionService_ServerPairUnsuccessful;
            connectionService.ServerPairCanceled += connectionService_ServerPairCanceled;
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            if (!PairedSuccessfully)
                connectionService.CancelPairingRequest();

            connectionService.ServerPairSuccessful -= connectionService_ServerPairSuccessful;
            connectionService.ServerPairUnsuccessful -= connectionService_ServerPairUnsuccessful;
            connectionService.ServerPairCanceled -= connectionService_ServerPairCanceled;
            connectionService = null;
        }

        void connectionService_ServerPairCanceled(object sender, System.EventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                MessageBox.Show("Pair canceled by server");
                if (this.NavigationService.CanGoBack)
                    this.NavigationService.GoBack();
            });
        }

        void connectionService_ServerPairUnsuccessful(object sender, System.EventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                MessageBox.Show("Incorrect pair code");
                if (this.NavigationService.CanGoBack)
                    this.NavigationService.GoBack();
            });
        }

        void connectionService_ServerPairSuccessful(object sender, System.EventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                PairedSuccessfully = true;
                MessageBox.Show("Device now paired with server.");
                if (this.NavigationService.CanGoBack)
                    this.NavigationService.GoBack();
            });
        }

        private void PairCodeTxt_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            PairCodeTxt.Focus();
        }

        private void ApplicationBarPair_Click(object sender, System.EventArgs e)
        {
            if (PairCodeTxt.Text.Length > 0)
                connectionService.SendPairCode(PairCodeTxt.Text);
        }

        private void ApplicationBarCancel_Click(object sender, System.EventArgs e)
        {
            connectionService.CancelPairingRequest();
            if (this.NavigationService.CanGoBack)
                this.NavigationService.GoBack();
        }
    }
}