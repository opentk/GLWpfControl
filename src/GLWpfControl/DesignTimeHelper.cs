using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace OpenTK.Wpf {
    internal static class DesignTimeHelper {

        public static void DrawDesignTimeHelper(GLWpfControl control, DrawingContext drawingContext) {
            if (control.Visibility == Visibility.Visible && control.ActualWidth > 0 && control.ActualHeight > 0) {
                const string labelText = "GL WPF CONTROL";
                var width = control.ActualWidth;
                var height = control.ActualHeight;
                var size = 1.5 * Math.Min(width, height) / labelText.Length;
                var tf = new Typeface("Arial");
                #pragma warning disable 618
                var ft = new FormattedText(labelText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, size, Brushes.White) {
                    TextAlignment = TextAlignment.Center
                };
                #pragma warning restore 618
                var redPen = new Pen(Brushes.DarkBlue, 2.0);
                var rect = new Rect(1, 1, width - 1, height - 1);
                drawingContext.DrawRectangle(Brushes.Black, redPen, rect);
                drawingContext.DrawLine(new Pen(Brushes.DarkBlue, 2.0),
                    new Point(0.0, 0.0),
                    new Point(control.ActualWidth, control.ActualHeight));
                drawingContext.DrawLine(new Pen(Brushes.DarkBlue, 2.0),
                    new Point(control.ActualWidth, 0.0),
                    new Point(0.0, control.ActualHeight));
                drawingContext.DrawText(ft, new Point(width / 2, (height - ft.Height) / 2));
            }
        }
        
    }
}
