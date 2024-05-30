using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using OpenTK.Windowing.Common;
using OpenTK.Wpf;

namespace Example {
    /// <summary>
    ///     Interaction logic for TabbedMainWindowTest.xaml
    /// </summary>
    public sealed partial class TabbedMainWindowTest
    {
        ExampleScene scene1 = new ExampleScene();
        ExampleScene scene2 = new ExampleScene();
        ExampleScene scene3 = new ExampleScene();

        public TabbedMainWindowTest() {
            InitializeComponent();
            GLWpfControlSettings mainSettings = new GLWpfControlSettings {MajorVersion = 4, MinorVersion = 1, Profile = ContextProfile.Compatability, ContextFlags = ContextFlags.Debug};
            Control1.Start(mainSettings);
            scene1.Initialize();

            GLWpfControlSettings insetSettings = new GLWpfControlSettings {MajorVersion = 4, MinorVersion = 1, Profile = ContextProfile.Compatability, ContextFlags = ContextFlags.Debug, Samples = 8};
            Control2.Start(insetSettings);
            scene2.Initialize();

            GLWpfControlSettings transparentSettings = new GLWpfControlSettings { MajorVersion = 4, MinorVersion = 1, Profile = ContextProfile.Compatability, ContextFlags = ContextFlags.Debug, TransparentBackground = true};
            Control3.Start(transparentSettings);
            scene3.Initialize();

            Control1.KeyDown += Control1_KeyDown;
        }

        private void Control1_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            Debug.WriteLine(e.Key);
        }

        private void Control2_OnRender(TimeSpan delta) {
            scene1.Render();
        }

        private void Control1_OnRender(TimeSpan delta) {
            scene2.Render();
        }

        private void Control3_OnRender(TimeSpan delta)
        {
            scene3.Render(0.0f);
        }
    }
}
