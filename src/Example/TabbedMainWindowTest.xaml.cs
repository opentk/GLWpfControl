using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using OpenTK.Windowing.Common;
using OpenTK.Wpf;

namespace Example
{
    /// <summary>
    /// Interaction logic for TabbedMainWindowTest.xaml
    /// </summary>
    public sealed partial class TabbedMainWindowTest
    {
        // FIXME: Make this example make use of context sharing...
        ExampleScene scene1 = new ExampleScene();
        ExampleScene scene2 = new ExampleScene();
        ExampleScene scene3 = new ExampleScene();

        public TabbedMainWindowTest()
        {
            InitializeComponent();
            GLWpfControlSettings mainSettings = new GLWpfControlSettings {MajorVersion = 4, MinorVersion = 1, Profile = ContextProfile.Compatability, ContextFlags = ContextFlags.Debug};
            // Start() makes the controls context current.
            Control1.Start(mainSettings);
            // We call Context.MakeCurrent() to make this explicitly clear.
            Control1.Context.MakeCurrent();
            scene1.Initialize();

            GLWpfControlSettings insetSettings = new GLWpfControlSettings {MajorVersion = 4, MinorVersion = 1, Profile = ContextProfile.Compatability, ContextFlags = ContextFlags.Debug, Samples = 8};
            Control2.Start(insetSettings);
            Control2.Context.MakeCurrent();
            scene2.Initialize();

            GLWpfControlSettings transparentSettings = new GLWpfControlSettings { MajorVersion = 4, MinorVersion = 1, Profile = ContextProfile.Compatability, ContextFlags = ContextFlags.Debug, TransparentBackground = true};
            Control3.Start(transparentSettings);
            Control3.Context.MakeCurrent();
            scene3.Initialize();

            Control1.KeyDown += Control1_KeyDown;

            Keyboard.AddPreviewKeyDownHandler(this, Keyboard_PreviewKeyDown);
        }

        private void Keyboard_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Debug.WriteLine($"Preview key down: {e.Key}");
        }

        private void Control1_KeyDown(object sender, KeyEventArgs e)
        {
            Debug.WriteLine(e.Key);
        }

        private void Control1_OnRender(TimeSpan delta)
        {
            scene1.Render();
        }

        private void Control2_OnRender(TimeSpan delta)
        {
            scene2.Render();
        }

        private void Control3_OnRender(TimeSpan delta)
        {
            scene3.Render(0.0f);
        }
    }
}
