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

            // You can start and rely on the Settings property that may be set in XAML or elsewhere in the codebase.
            OpenTkControl.Start();

            // Or, you can suppy a settings object directly.
            InsetControl.Start(new GLWpfControlSettings()
            {
                MajorVersion = 2,
                MinorVersion = 1,
                RenderContinuously = false,
            });
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
