using System;
using System.Windows;
using OpenTK.Wpf;

namespace Example {
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public sealed partial class MainWindow {

        ExampleScene mainScene = new ExampleScene();
        ExampleScene insetScene = new ExampleScene();

        public MainWindow() {
            InitializeComponent();
            var mainSettings = new GLWpfControlSettings {MajorVersion = 2, MinorVersion = 1, Samples = 4};
            OpenTkControl.Start(mainSettings);
            mainScene.Initialize();

            var insetSettings = new GLWpfControlSettings {MajorVersion = 2, MinorVersion = 1, RenderContinuously = false};
            InsetControl.Start(insetSettings);
            insetScene.Initialize();
        }

        private void OpenTkControl_OnRender(TimeSpan delta) {
            mainScene.Render();
        }

        private void InsetControl_OnRender(TimeSpan delta) {
            insetScene.Render();
        }

        private void RedrawButton_OnClick(object sender, RoutedEventArgs e) {
            // re-draw the inset control when the button is clicked.
            InsetControl.InvalidateVisual();
        }
    }
}
