using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Raspberry_Pi.Network
{
    public interface IConnection
    {
        string Address { get; set; }
        int Port { get; }

        bool IsOpen { get; }

        void Open(Action<bool> callback);
        void Close();

        void Send(byte[] data);
        void Listen();

        event EventHandler<DataReceivedEventArgs> DataReceived;
        event EventHandler DataSentSuccessfully;
        event EventHandler<ExceptionOccurredEventArgs> ExceptionOccurred;
    }

    public class ExceptionOccurredEventArgs : EventArgs
    {
        public Exception Exception { get; set; }
    }
}
