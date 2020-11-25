using System;
using System.Windows;
using OpenTK.Windowing.Common;
using OpenTK.Wpf;

namespace Example {
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public sealed partial class MainWindow {
        public MainWindow() {
            InitializeComponent();
            var mainSettings = new GLWpfControlSettings {MajorVersion = 4, MinorVersion = 1, GraphicsProfile = ContextProfile.Compatability, GraphicsContextFlags = ContextFlags.Debug};
            OpenTkControl.Start(mainSettings);
            var insetSettings = new GLWpfControlSettings {MajorVersion = 4, MinorVersion = 1, GraphicsProfile = ContextProfile.Compatability, GraphicsContextFlags = ContextFlags.Debug, RenderContinuously = false};
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
            InsetControl.InvalidateVisual();
        }
    }
}
