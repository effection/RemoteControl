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

namespace Raspberry_Pi.Network
{
    public class DataReceivedEventArgs : EventArgs
    {
        public byte[] Data { get; set; }

        public string Source { get; set; }
    }
}
