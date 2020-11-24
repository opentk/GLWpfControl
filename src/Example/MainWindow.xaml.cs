using System;
using System.Windows;
using OpenTK.Wpf;

namespace Example {
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public sealed partial class MainWindow {
        public MainWindow() {
            InitializeComponent();
            var mainSettings = new GLWpfControlSettings {MajorVersion = 2, MinorVersion = 1};
            OpenTkControl.Start(mainSettings);
            var insetSettings = new GLWpfControlSettings {MajorVersion = 2, MinorVersion = 1, RenderContinuously = false};
            InsetControl.Start(insetSettings);
        }

        private void OpenTkControl_OnRender(TimeSpan delta) {
            ExampleScene.Render();
        }

        private void InsetControl_OnRender(TimeSpan delta) {
            ExampleScene.Render();
        }

        private void RedrawButton_OnClick(object sender, RoutedEventArgs e) {
            // re-draw the inset control when the button is clicked.
            // InsetControl.InvalidateVisual();
        }
    }
}
