using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace OpenTK.Wpf
{
    internal static class UnstartedControlHelper
    {

        public static void DrawUnstartedControlHelper(GLWpfControl control, DrawingContext drawingContext)
        {
            if (control.Visibility == Visibility.Visible && control.ActualWidth > 0 && control.ActualHeight > 0)
            {
                var width = control.ActualWidth;
                var height = control.ActualHeight;
                drawingContext.DrawRectangle(Brushes.Gray, null, new Rect(0, 0, width, height));

                if(!Debugger.IsAttached) // Do not show the message if we're not debugging
                {
                    return;
                }

                const string unstartedLabelText = "OpenGL content. Call Start() on the control to begin rendering.";
                const int size = 12;
                var tf = new Typeface("Arial");
#pragma warning disable 618
                var ft = new FormattedText(unstartedLabelText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, size, Brushes.White)
                {
                    TextAlignment = TextAlignment.Left,
                    MaxTextWidth = width
                };
#pragma warning restore 618

                drawingContext.DrawText(ft, new Point(0, 0));
            }
        }


    }
}
