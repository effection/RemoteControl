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
    /// <summary>
    /// Sends mouse data.
    /// </summary>
    /// <remarks>
    /// Connection is a public property so individual devices can have seperate connection types if needed.
    /// </remarks>
    public class MouseForwarder
    {
        /// <summary>
        /// Start sequence for mouse bytes.
        /// </summary>
        public static readonly byte[] MOUSE_ID = { 0xF0, 0xF0 };

        private static readonly byte[] MOUSE_DOWN = { 0xF0, 0xF0, 0x00 };
        private static readonly byte[] MOUSE_UP = { 0xF0, 0xF0, 0x01 };
        private static readonly byte[] MOUSE_CLICK = { 0xF0, 0xF0, 0x02 };
        private static readonly byte[] MOUSE_SET_POS = { 0xF0, 0xF0, 0x03 };
        private static readonly byte[] MOUSE_MOVE_REL = { 0xF0, 0xF0, 0x04 };
        private static readonly byte[] MOUSE_SCROLL = { 0xF0, 0xF0, 0x05 };

        public MouseForwarder() { }

        /// <summary>
        /// Send mouse down message.
        /// </summary>
        /// <param name="x">Optional absolute x position of mouse.</param>
        /// <param name="y">Optional absolute y position of mouse.</param>
        public void SendMouseDown(int x = -1, int y = -1)
        {
            if (x != -1 && y != -1)
                SendSetMousePosition(x, y);


            var data = new byte[3];
            MOUSE_DOWN.CopyTo(data, 0);
            Forwarder.GetConnectionForService<MouseForwarder>().Send(data);
        }

        /// <summary>
        /// Send mouse up message.
        /// </summary>
        public void SendMouseUp()
        {
            var data = new byte[3];
            MOUSE_UP.CopyTo(data, 0);
            Forwarder.GetConnectionForService<MouseForwarder>().Send(data);
        }

        /// <summary>
        /// Send mouse click message.
        /// </summary>
        /// <param name="x">Optional absolute x position of mouse.</param>
        /// <param name="y">Optional absolute y position of mouse.</param>
        public void SendMouseClick(int x = -1, int y = -1)
        {
            if (x != -1 && y != -1)
                SendSetMousePosition(x, y);


            var data = new byte[3];
            MOUSE_CLICK.CopyTo(data, 0);
            Forwarder.GetConnectionForService<MouseForwarder>().Send(data);
        }

        /// <summary>
        /// Set the absolute mouse position.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void SendSetMousePosition(int x, int y)
        {
            var data = new byte[3 + 8];

            MOUSE_SET_POS.CopyTo(data, 0);
            BitConverter.GetBytes(x).CopyTo(data, 3);
            BitConverter.GetBytes(y).CopyTo(data, 7);

            Forwarder.GetConnectionForService<MouseForwarder>().Send(data);
        }

        /// <summary>
        /// Set the relative mouse position to current position.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void SendSetMousePositionRelaitve(int x, int y)
        {
            Forwarder.GetConnectionForService<MouseForwarder>().Send(Encoding.UTF8.GetBytes(string.Concat(new object[]
		        {
			        "{m}",
			        x,
			        ",",
			        y
		        })));
        }

        /// <summary>
        /// Send mouse scroll message.
        /// </summary>
        /// <param name="delta"></param>
        public void SendMouseScroll(int delta)
        {
            var data = new byte[3 + 4];

            MOUSE_SCROLL.CopyTo(data, 0);
            BitConverter.GetBytes(delta).CopyTo(data, 3);

            Forwarder.GetConnectionForService<MouseForwarder>().Send(data);
        }
    }
}
