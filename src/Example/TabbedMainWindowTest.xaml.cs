using System;
using OpenTK.Wpf;

namespace Example {
    /// <summary>
    ///     Interaction logic for TabbedMainWindowTest.xaml
    /// </summary>
    public sealed partial class TabbedMainWindowTest
    {
        public TabbedMainWindowTest() {
            InitializeComponent();
            
            var mainSettings = new GLWpfControlSettings {MajorVersion = 2, MinorVersion = 1};
            Control1.Start(mainSettings);
            var insetSettings = new GLWpfControlSettings {MajorVersion = 2, MinorVersion = 1};
            Control2.Start(insetSettings);
            var remoteAppSettings = new GLWpfControlSettings { MajorVersion = 2, MinorVersion = 1 , EnableSoftwareFallback= true };
            Control3.Start(remoteAppSettings);
        }

        private void OpenTkControl_OnRender(TimeSpan delta) {
            ExampleScene.Render();
        }

        private void Control2_OnRender(TimeSpan delta) {
	        ExampleScene.Render();
        }

        private void Control1_OnRender(TimeSpan delta) {
	        ExampleScene.Render();
        }

        private void Control3_OnRender(TimeSpan obj)
        {
            ExampleScene.Render();
        }
    }
}
