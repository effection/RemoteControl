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
using System.Threading;
using System.Text;

namespace Raspberry_Pi.Network
{
    public class UdpConnection : IConnection
    {
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

        #region Fields

        private Socket connection;

        #endregion

        public UdpConnection(string address, int port)
        {
            Address = address;
            Port = port;
        }

        public void Open(Action<bool> callback)
        {
            try
            {
                this.connection = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            }
            catch (Exception)
            {
                callback(false);
            }
            IsOpen = true;
            callback(true);
            return;
        }

        public void Close()
        {
            connection.Close();
        }

        public void Send(byte[] data)
        {
            if (this.connection != null)
            {
                try
                {
                    SocketAsyncEventArgs socketAsyncEventArgs = new SocketAsyncEventArgs();
                    socketAsyncEventArgs.RemoteEndPoint = new DnsEndPoint(Address, Port);
                    socketAsyncEventArgs.Completed += (s, e) =>
                    {
                        //TODO Check error
                        if (e.SocketError == SocketError.Success)
                            OnDataSentSuccessfully();
                    };
                    socketAsyncEventArgs.SetBuffer(data, 0, data.Length);
                    this.connection.SendToAsync(socketAsyncEventArgs);
                }
                catch (Exception)
                {
                    
                }
            }
        }

        public void Listen()
        {
            if (!IsOpen || connection == null)
                return;

            const int MAX_BUFFER_SIZE = 256;

            SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();
            socketEventArg.RemoteEndPoint = new IPEndPoint(IPAddress.Parse(Address), Port);

            // Setup the buffer to receive the data
            socketEventArg.SetBuffer(new Byte[MAX_BUFFER_SIZE], 0, MAX_BUFFER_SIZE);

            // Inline event handler for the Completed event.
            // Note: This even handler was implemented inline in order to make this method self-contained.
            socketEventArg.Completed += (s, e) =>
            {
                if (e.SocketError == SocketError.Success)
                {
                    OnDataReceived(e.Buffer, Address);
                    Listen();
                }
                else
                {
                    //response = e.SocketError.ToString();
                }
            };
            connection.ReceiveFromAsync(socketEventArg);
            System.Diagnostics.Debug.WriteLine("Listening...");
        }

        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public event EventHandler DataSentSuccessfully;
        public event EventHandler<ExceptionOccurredEventArgs> ExceptionOccurred;

        private void OnDataReceived(byte[] buf, string source)
        {
            if (DataReceived != null)
                DataReceived(this, new DataReceivedEventArgs { Data = buf, Source = source });
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
