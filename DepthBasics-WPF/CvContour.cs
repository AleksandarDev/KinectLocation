//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.Drawing;
using Emgu.CV;
using Emgu.CV.Util;

namespace Microsoft.Samples.Kinect.DepthBasics
{
    using System.ComponentModel;
    using System.Windows;

    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        public class CvContour : Contour
        {
            public CvContour(System.Drawing.Point[] points)
                : this(points, CvInvoke.BoundingRectangle(new VectorOfPoint(points)))
            {
            }

            public CvContour(System.Drawing.Point[] points, Rectangle boundingBox) : base(points, boundingBox)
            {
            }
        }
    }
}
