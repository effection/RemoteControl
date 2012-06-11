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
using System.Collections.Generic;

namespace Raspberry_Pi.Network
{
    public class TcpConnection : IConnection
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

        /// <summary>
        /// Maximum buffer size.
        /// </summary>
        public int MAX_BUFFER_SIZE { get; set; }

        #endregion

        #region Fields

        private Socket connection;
        private DnsEndPoint endPoint;

        #endregion

        public TcpConnection(string address, int port)
        {
            Address = address;
            Port = port;
        }

        /// <summary>
        /// Open connection.
        /// </summary>
        public void Open(Action<bool> callback)
        {
            IsOpen = false;

            connection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            endPoint = new DnsEndPoint(Address, Port);

            var connectionOperation = new SocketAsyncEventArgs { RemoteEndPoint = endPoint };
            connectionOperation.Completed += (o, e) =>
                {
                    IsOpen = e.SocketError == SocketError.Success;
                    callback(IsOpen);
                };

            connection.ConnectAsync(connectionOperation);
        }

        /// <summary>
        /// Close connection.
        /// </summary>
        public void Close()
        {
            connection.Close();
            IsOpen = false;
        }

        /// <summary>
        /// Send data.
        /// </summary>
        /// <param name="data"></param>
        public void Send(byte[] data)
        {
            if (!IsOpen || connection == null)
                return;

            var sendOp = new SocketAsyncEventArgs { RemoteEndPoint = endPoint };
            sendOp.Completed += (o, e) =>
            {
                //TODO check error
                if (e.SocketError != SocketError.Success)
                    System.Diagnostics.Debug.WriteLine(e.SocketError);
            };

            sendOp.SetBuffer(data, 0, data.Length);
            connection.SendAsync(sendOp);
        }

        private List<byte> previousReceivedBytes = new List<byte>();
        /// <summary>
        /// Listen for incoming data.
        /// </summary>
        public void Listen()
        {
            if (!IsOpen || connection == null)
                return;

            if (MAX_BUFFER_SIZE <= 0)
                MAX_BUFFER_SIZE = 256;

            var receiveOp = new SocketAsyncEventArgs { RemoteEndPoint = endPoint };
            receiveOp.Completed += (o, e) =>
            {
                if (e.BytesTransferred > 0)
                {
                    //Appending data
                    if (previousReceivedBytes.Count > 0)
                    {
                        previousReceivedBytes.AddRange(e.Buffer);
                        if (e.BytesTransferred == MAX_BUFFER_SIZE)
                        {
                            //More to receive
                        }
                        else
                        {
                            //Message complete
                            byte[] prev = previousReceivedBytes.ToArray();
                            previousReceivedBytes.Clear();
                            OnDataReceived(prev, endPoint.Host);
                        }
                    }
                    else
                    {
                        //First packet
                        if (e.BytesTransferred == MAX_BUFFER_SIZE)
                        {
                            //More to receive
                            previousReceivedBytes.AddRange(e.Buffer);
                        }
                        else
                        {
                            //Full message received
                            OnDataReceived(e.Buffer, endPoint.Host);
                        }
                    }
                }
                else
                {
                    //No bytes received - Error

                }
                //Listen again
                Listen();
            };

            var buf = new byte[MAX_BUFFER_SIZE];
            receiveOp.SetBuffer(buf, 0, MAX_BUFFER_SIZE);
            connection.ReceiveAsync(receiveOp);
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
