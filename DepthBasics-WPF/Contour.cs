//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.Drawing;

namespace Microsoft.Samples.Kinect.DepthBasics
{
    using System.ComponentModel;
    using System.Windows;

    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        public class Contour : IContour
        {
            public Contour(System.Drawing.Point[] points, Rectangle boundingBox)
            {
                this.Points = points;
                this.BoundingBox = boundingBox;
            }

            public System.Drawing.Point[] Points { get; set; }

            public Rectangle BoundingBox { get; set; }
        }
    }
}
