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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using GalaSoft.MvvmLight;

namespace Raspberry_Pi.Services
{
    public class ConnectionService : ViewModelBase
    {
        #region Properties

        /// <summary>
        /// Normal connection on seperate port from data.
        /// </summary>
        public Network.IConnection connection { get; set; }

        private ObservableDictionary<string, bool> _AvailableServers;

        /// <summary>
        /// Found servers and paired or not.
        /// </summary>
        public ObservableDictionary<string, bool> AvailableServers
        {
            get { return _AvailableServers; }
            set
            {
                if (_AvailableServers == value)
                    return;
                _AvailableServers = value;
                RaisePropertyChanged(() => AvailableServers);
            }
        }

        private string _ServerAddress;
        /// <summary>
        /// Ip of the server we are using.
        /// </summary>
        public string ServerAddress
        {
            get { return _ServerAddress; }
            set
            {
                if (_ServerAddress == value)
                    return;
                _ServerAddress = value;
                RaisePropertyChanged(() => ServerAddress);
            }
        }

        /// <summary>
        /// Server selected.
        /// </summary>
        public bool GotServer { get; private set; }

        public event EventHandler<ServerSelectedEventArgs> ServerSelected;

        public event EventHandler ServerOkToPair;

        public event EventHandler ServerPairSuccessful;

        public event EventHandler ServerPairUnsuccessful;

        public event EventHandler ServerPairCanceled;

        #endregion

        //Used to find servers
        private Network.UdpMulticastConnection multicastConnection = new Network.UdpMulticastConnection("224.0.0.86", 11300);

        #region Message Sequences

        //TO DEVICE
        private static readonly byte[] IS_ANYONE_THERE = { (byte)'a' };//{ 0xF0, 0xFF, 0x00 };
        private static readonly byte[] ALREADY_PAIRED = { (byte)'b' };//{ 0xF0, 0xFF, 0x01 };
        private static readonly byte[] NOT_PAIRED = { (byte)'c' };//{ 0xF0, 0xFF, 0x02 };
        private static readonly byte[] AUTHORISED = { (byte)'d' };//{ 0xF0, 0xFF, 0x03 };
        private static readonly byte[] INCORRECT_PAIR_CODE = { (byte)'e' };//{ 0xF0, 0xFF, 0x04 };
        //private static readonly byte[] PAIR_CODE_CANCEL =           { (byte)'i' };//{ 0xF0, 0xFF, 0x08 };
        private static readonly byte[] ARE_YOU_A_SERVER_RESPONSE = { (byte)'f' };
        private static readonly byte[] OK_TO_PAIR = { (byte)'m' };

        //FROM DEVICE
        private static readonly byte[] IS_ANYONE_THERE_RESPONSE = { (byte)'g' };//{ 0xF0, 0xFF, 0x05 };
        private static readonly byte[] ASK_TO_PAIR = { (byte)'h' };//{ 0xF0, 0xFF, 0x06 };
        private static readonly byte[] PAIR_CODE_RESPONSE = { (byte)'i' };//{ 0xF0, 0xFF, 0x07 };
        private static readonly byte[] PAIR_CODE_CANCEL = { (byte)'j' };//{ 0xF0, 0xFF, 0x08 };
        private static readonly byte[] ARE_YOU_A_SERVER = { (byte)'k' };
        private static readonly byte[] ARE_WE_PAIRED = { (byte)'l' };

        #endregion

        public ConnectionService()
        {
            AvailableServers = new ObservableDictionary<string, bool>();
            multicastConnection.DataReceived += multicastConnection_DataReceived;
            multicastConnection.Open((success) =>
                {
                    if (success)
                    {
                        multicastConnection.Listen();
                        FindServers();
                    }
                });
        }

        ~ConnectionService()
        {
            multicastConnection.DataReceived -= multicastConnection_DataReceived;
        }

        /// <summary>
        /// Find servers on the network.
        /// </summary>
        public void FindServers()
        {
            multicastConnection.Send(ARE_YOU_A_SERVER);
        }

        /// <summary>
        /// Ask each server if we are paired.
        /// </summary>
        public void CheckServersPairStatus()
        {
            var enumerator = AvailableServers.GetEnumerator();

            EventHandler nextServer = null;
            nextServer = (s, e) =>
            {
                if (enumerator.MoveNext())
                {
                    UseServer(((KeyValuePair<string, bool>)enumerator.Current).Key);
                    CheckIfPaired();
                }
                else
                {
                    ServerOkToPair -= nextServer;
                    ServerPairSuccessful -= nextServer;
                }
            };

            ServerOkToPair += nextServer;
            ServerPairSuccessful += nextServer;

            //Ask first server
            nextServer(null, null);

        }

        /// <summary>
        /// Set ip of server to use.
        /// </summary>
        /// <param name="ip"></param>
        public void UseServer(string ip)
        {
            if (AvailableServers.ContainsKey(ip))
            {
                if (connection != null)
                {
                    connection.DataReceived -= connection_DataReceived;
                    connection.Close();
                    connection = null;
                }
                ServerAddress = ip;
                connection = new Network.UdpConnection(ip, 11301);
                connection.DataReceived += connection_DataReceived;
                connection.Open((success) =>
                    {
                        //TODO handle this
                        connection.DataSentSuccessfully += connection_DataSentSuccessfully;
                        connection.Send(new byte[] { 0x01 });
                        
                    });
                GotServer = true;
                OnServerSelected(ip);
            }
        }

        /// <summary>
        /// Hack to start listener as data has to be sent first
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void connection_DataSentSuccessfully(object sender, EventArgs e)
        {
            connection.DataSentSuccessfully -= connection_DataSentSuccessfully;
            connection.Listen();
        }

        /// <summary>
        /// Inform the server we are on the network.
        /// </summary>
        public void TellServerImHere()
        {
            if(GotServer)
                connection.Send(IS_ANYONE_THERE_RESPONSE);
        }

        /// <summary>
        /// Ask the server if we are paired.
        /// </summary>
        public void CheckIfPaired()
        {
            if (GotServer)
                connection.Send(ARE_WE_PAIRED);
        }

        /// <summary>
        /// Ask server if we can pair.
        /// </summary>
        public void AskToPair()
        {
            if (GotServer)
                connection.Send(ASK_TO_PAIR);
        }

        /// <summary>
        /// Cancel pairing request.
        /// </summary>
        public void CancelPairingRequest()
        {
            if (GotServer)
                connection.Send(PAIR_CODE_CANCEL);
        }

        /// <summary>
        /// Send pair code.
        /// </summary>
        /// <param name="code">String version of the code.</param>
        public void SendPairCode(string code)
        {
            if (!GotServer)
                return;

            var bytes = new byte[PAIR_CODE_RESPONSE.Length + code.Length];
            PAIR_CODE_RESPONSE.CopyTo(bytes, 0);

            for (int i = 0; i < code.Length; i++)
            {
                var intValue = Int32.Parse(code[i].ToString());
                bytes[PAIR_CODE_RESPONSE.Length + i] = (byte)intValue;
            }

            connection.Send(bytes);
        }

        /// <summary>
        /// Respond to multicast messages.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void multicastConnection_DataReceived(object sender, Network.DataReceivedEventArgs e)
        {
            if (IsMessage(e.Data, ARE_YOU_A_SERVER_RESPONSE))
            {
                var ip = e.Source;
                if (!AvailableServers.ContainsKey(ip))
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() => { AvailableServers.Add(ip, false); });      
                }
            }
        }

        /// <summary>
        /// Server udp connection messages.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void connection_DataReceived(object sender, Network.DataReceivedEventArgs e)
        {
            var ip = e.Source;
            if (IsMessage(e.Data, ARE_YOU_A_SERVER_RESPONSE))
            {
                if (!AvailableServers.ContainsKey(ip))
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() => { AvailableServers.Add(ip, false); });
                }

                //Check pair byte
                if (e.Data.Length > ARE_YOU_A_SERVER_RESPONSE.Length + 1)
                {
                    var pairByte = e.Data[ARE_YOU_A_SERVER_RESPONSE.Length];
                    Deployment.Current.Dispatcher.BeginInvoke(() => { AvailableServers[ip] = pairByte == 0x01; });
                }
            }
            else if (IsMessage(e.Data, IS_ANYONE_THERE))
            {
                TellServerImHere();
            }
            else if (IsMessage(e.Data, ALREADY_PAIRED))
            {
                OnServerPairSuccessful(ip);
            }
            else if (IsMessage(e.Data, OK_TO_PAIR))
            {
                //Server not yet paired
                OnServerOkToPair(ip);
            }
            else if (IsMessage(e.Data, NOT_PAIRED))
            {
                Deployment.Current.Dispatcher.BeginInvoke(() => { AvailableServers[ip] = false; });
            }
            else if (IsMessage(e.Data, AUTHORISED))
            {
                //Pair succeeded
                OnServerPairSuccessful(ip);
            }
            else if (IsMessage(e.Data, INCORRECT_PAIR_CODE))
            {
                //Pair failed
                OnServerPairUnsuccessful(ip);
            }
            else if (IsMessage(e.Data, PAIR_CODE_CANCEL))
            {
                //Server canceled pair
                OnServerPairCanceled(ip);
            }
        }

        /// <summary>
        /// Is packet a specific message.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private bool IsMessage(byte[] packet, byte[] message)
        {
            if (packet.Length < message.Length)
                return false;

            for (int i = 0; i < message.Length; i++)
                if (packet[i] != message[i])
                    return false;
            return true;
        }

        private void OnServerSelected(string ip)
        {
            if (ServerSelected != null)
                ServerSelected(this, new ServerSelectedEventArgs { Address = ip });
        }

        private void OnServerOkToPair(string ip)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() => { AvailableServers[ip] = false; });
            if (ServerOkToPair != null)
                ServerOkToPair(this, EventArgs.Empty);
        }

        private void OnServerPairSuccessful(string ip)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() => { AvailableServers[ip] = true; });
            if (ServerPairSuccessful != null)
                ServerPairSuccessful(this, EventArgs.Empty);
        }

        private void OnServerPairUnsuccessful(string ip)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() => { AvailableServers[ip] = false; });
            if (ServerPairUnsuccessful != null)
                ServerPairUnsuccessful(this, EventArgs.Empty);
        }

        private void OnServerPairCanceled(string ip)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() => { AvailableServers[ip] = false; });
            if (ServerPairCanceled != null)
                ServerPairCanceled(this, EventArgs.Empty);
        }
    }

    public class ServerSelectedEventArgs : EventArgs
    {
        public string Address { get; set; }
    }
}
