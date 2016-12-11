//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------


namespace Microsoft.Samples.Kinect.DepthBasics
{
    using System.ComponentModel;
    using System.Windows;

    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        public class LoiPoint
        {
            public LoiPoint(System.Drawing.Point location, byte depth)
            {
                this.Location = location;
                this.Depth = depth;
            }


            public System.Drawing.Point Location { get; set; }

            public byte Depth { get; set; }
        }
    }
}
