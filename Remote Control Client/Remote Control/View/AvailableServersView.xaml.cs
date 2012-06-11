using Microsoft.Phone.Controls;
using System.Windows;
using System.Collections.Generic;
using System;

namespace Raspberry_Pi.View
{
    /// <summary>
    /// Description for AvailableServersView.
    /// </summary>
    public partial class AvailableServersView : PhoneApplicationPage
    {
        public Services.ConnectionService ConnectionService { get; private set; }

        /// <summary>
        /// Initializes a new instance of the AvailableServersView class.
        /// </summary>
        public AvailableServersView()
        {
            InitializeComponent();

            ConnectionService = Services.Forwarder.GetService<Services.ConnectionService>();

            this.AvailableServersList.DataContext = ConnectionService;

            Loaded += new RoutedEventHandler(AvailableServersView_Loaded);
        }

        void AvailableServersView_Loaded(object sender, RoutedEventArgs e)
        {
            System.Threading.Thread.Sleep(1500);
            ConnectionService.CheckServersPairStatus();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            ConnectionService.ServerOkToPair += ConnectionService_ServerOkToPair;
            ConnectionService.ServerPairCanceled += ConnectionService_ServerPairCanceled;
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            ConnectionService.ServerOkToPair -= ConnectionService_ServerOkToPair;
            ConnectionService.ServerPairCanceled -= ConnectionService_ServerPairCanceled;
        }

        private void ConnectionService_ServerOkToPair(object sender, System.EventArgs e)
        {
            //Goto PairWithServerView
            Dispatcher.BeginInvoke(() =>
            {
                this.NavigationService.Navigate(new System.Uri("/View/PairWithServerView.xaml", UriKind.Relative));
            });
        }

        private void ConnectionService_ServerPairCanceled(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                MessageBox.Show("Server refused to pair");
            });
        }

        private void PairMenuAction_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var item = (KeyValuePair<string, bool>)(sender as MenuItem).DataContext;
            var server = item.Key;
            if (!item.Value)
            {
                ConnectionService.UseServer(server);
                ConnectionService.AskToPair();
            }
        }

        private void UseServerBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
        	//TODO Check selected item is paired then store the server selected or null.
        }
    }
}