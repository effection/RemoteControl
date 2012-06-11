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
using System.Text;

namespace Raspberry_Pi.Services
{
    public class PingService
    {
        private static readonly byte[] bytes;

        static PingService()
        {
            bytes = Encoding.UTF8.GetBytes("{ping}");
        }

        public void Ping()
        {
            Forwarder.GetConnectionForService<PingService>().Send(bytes);
        }
    }
}
