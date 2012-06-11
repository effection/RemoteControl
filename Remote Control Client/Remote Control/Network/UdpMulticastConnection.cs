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
using System.Net.Sockets;

namespace Raspberry_Pi.Network
{
    public class UdpMulticastConnection : IConnection
    {
        private IAsyncResult _openResult;
        private UdpAnySourceMulticastClient channel;

        #region Properties

        private string _Address;
        /// <summary>
        /// Server address.
        /// </summary>
        public string Address
        {
            get { return _Address; }
            set
            {
                if (IsOpen)
                    throw new InvalidOperationException("Cannot change Address while connection is open");
                _Address = value;
            }
        }

        /// <summary>
        /// Port number.
        /// </summary>
        public int Port { get; private set; }

        /// <summary>
        /// Is the connection open.
        /// </summary>
        public bool IsOpen { get; private set; }

        #endregion

        public UdpMulticastConnection(string address, int port)
        {
            Address = address;
            Port = port;
        }

        public void Open(Action<bool> callback)
        {
            try
            {
                IsOpen = false;
                channel = new UdpAnySourceMulticastClient(IPAddress.Parse(Address), Port);

                _openResult = channel.BeginJoinGroup((result) =>
                {
                    channel.EndJoinGroup(result);
                    IsOpen = true;
                    callback(true);
                }, null);


            }
            catch
            {
                callback(false);
            }
        }

        public void Close()
        {
            IsOpen = false;
        }
        byte[] _receiveBuffer = new byte[256];
        public void Listen()
        {
            if (IsOpen)
            {
                Array.Clear(_receiveBuffer, 0, _receiveBuffer.Length);
                channel.BeginReceiveFromGroup(_receiveBuffer, 0, _receiveBuffer.Length,
                    result =>
                    {
                        IPEndPoint source;

                        // Complete the asynchronous operation. The source field will 
                        // contain the IP address of the device that sent the message
                        channel.EndReceiveFromGroup(result, out source);

                        OnDataReceived(_receiveBuffer, source.Address.ToString());

                        // Call receive again to continue to "listen" for the next message from the group
                        Listen();
                    }, null);
            }
        }

        /// <summary>
        /// Send message to everyone in the multicast group.
        /// </summary>
        /// <param name="data"></param>
        public void Send(byte[] data)
        {
            try
            {
                if (IsOpen)
                {
                    channel.BeginSendToGroup(data, 0, data.Length, (r) => channel.EndSendToGroup(r), null);
                }
            }catch
            {

            }
        }

        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public event EventHandler DataSentSuccessfully;
        public event EventHandler<ExceptionOccurredEventArgs> ExceptionOccurred;

        private void OnDataReceived(byte[] buf, string source)
        {
            if (DataReceived != null)
                DataReceived(this, new DataReceivedEventArgs { Data = buf, Source = source});
        }

        private void OnDataSentSuccessfully()
        {
            if (DataSentSuccessfully != null)
                DataSentSuccessfully(this, EventArgs.Empty);
        }

        private void OnExceptionOccurred(Exception e)
        {
            if (ExceptionOccurred != null)
                ExceptionOccurred(this, new ExceptionOccurredEventArgs { Exception = e });
        }
    }
}
