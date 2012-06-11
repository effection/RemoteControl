using Microsoft.Phone.Controls;
using System.Threading;
using System;

namespace Raspberry_Pi.View
{
    /// <summary>
    /// Description for MouseView.
    /// </summary>
    public partial class MouseView : PhoneApplicationPage
    {
        private Services.MouseForwarder mouseForwarder;
        private Services.PingService pingService;

        private const int movementThreshold = 1;

        private Timer timer;
        private int lastX;
        private int lastY;

        /// <summary>
        /// Initializes a new instance of the MouseView class.
        /// </summary>
        public MouseView()
        {
            InitializeComponent();
            mouseForwarder = Services.Forwarder.GetService<Services.MouseForwarder>();
            pingService = Services.Forwarder.GetService<Services.PingService>();

            timer = new Timer((o) =>
                {
                    pingService.Ping();
                },
                null, 100, 200);
        }

        private void GestureListener_DragDelta(object sender, DragDeltaGestureEventArgs e)
        {

        }

        private void ContentPanel_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var p = e.GetPosition(null);
            this.lastX = (int)p.X;
            this.lastY = (int)p.Y;
        }

        private void ContentPanel_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

        }

        private void ContentPanel_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var p = e.GetPosition(null);
            var x = (int)p.X;
            var y = (int)p.Y;

            int deltaX = x - this.lastX;
            int deltaY = y - this.lastY;

            if ((deltaX < movementThreshold && deltaX > 0) || (deltaX > -movementThreshold && deltaX < 0))
                deltaX = 0;
            if ((deltaY < movementThreshold && deltaY > 0) || (deltaY > -movementThreshold && deltaY < 0))
                deltaY = 0;

            mouseForwarder.SendSetMousePositionRelaitve(deltaX, deltaY);

            this.lastX = x;
            this.lastY = y;
        }
    }
}