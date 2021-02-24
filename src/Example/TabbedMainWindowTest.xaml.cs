using System;
using System.Runtime.CompilerServices;
using OpenTK.Windowing.Common;
using OpenTK.Wpf;

namespace Example {
    /// <summary>
    ///     Interaction logic for TabbedMainWindowTest.xaml
    /// </summary>
    public sealed partial class TabbedMainWindowTest
    {
        public TabbedMainWindowTest() {
            InitializeComponent();
            var mainSettings = new GLWpfControlSettings {MajorVersion = 4, MinorVersion = 5, GraphicsProfile = ContextProfile.Compatability, GraphicsContextFlags = ContextFlags.Debug};
            Control1.Start(mainSettings);
            var insetSettings = new GLWpfControlSettings {MajorVersion = 4, MinorVersion = 5, GraphicsProfile = ContextProfile.Compatability, GraphicsContextFlags = ContextFlags.Debug};
            Control2.Start(insetSettings);
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
    }
}
